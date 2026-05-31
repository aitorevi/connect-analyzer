using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConnectAnalyzer.Application.Ports;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Outbound.Shopify;

// TODO: paginate via the Link header's `rel="next"` cursor. The MVP fetches a single page of
public sealed class ShopifyOrdersRepository(
    HttpClient http,
    ShopifyTokenProvider tokens,
    string apiVersion) : ISalesRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default) =>
        tokens.GetAccessTokenAsync(ct).BindAsync(token => FetchOrdersAsync(token, ct));

    private async Task<Result<IReadOnlyList<Sale>>> FetchOrdersAsync(string token, CancellationToken ct)
    {
        // status=any captures both open and closed orders; financial_status=paid filters out
        // cancelled, pending, refunded and voided orders so the analytics reflect actual revenue
        // and match what the merchant sees in the Shopify admin.
        var resource = $"/admin/api/{apiVersion}/orders.json?status=any&financial_status=paid&limit=250";
        using var request = new HttpRequestMessage(HttpMethod.Get, resource);
        request.Headers.Add("X-Shopify-Access-Token", token);

        try
        {
            using var response = await http.SendAsync(request, ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result<IReadOnlyList<Sale>>.Failure(Error.Unauthorized(
                    "Shopify rejected the Admin API token (it may have been revoked or lack the required scopes)."));

            // TODO: respect Retry-After header on 429 instead of failing immediately.
            if ((int)response.StatusCode == 429)
                return Result<IReadOnlyList<Sale>>.Failure(Error.Unavailable(
                    "Shopify rate limit hit."));

            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<Sale>>.Failure(Error.Unavailable(
                    $"Shopify Admin API returned {(int)response.StatusCode}."));

            var payload = await response.Content.ReadFromJsonAsync<OrdersResponse>(JsonOptions, ct);

            IReadOnlyList<Sale> sales = (payload?.Orders ?? [])
                .SelectMany(LineItemsAsSales)
                .ToList();

            return Result<IReadOnlyList<Sale>>.Success(sales);
        }
        catch (HttpRequestException ex)
        {
            return Result<IReadOnlyList<Sale>>.Failure(Error.Unavailable(
                $"Could not reach the Shopify data source: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            // A 2xx response whose body is not the JSON shape we expect is unexpected (mirrors
            // the SAP adapter), not Validation: the caller's request was perfectly well-formed.
            return Result<IReadOnlyList<Sale>>.Failure(Error.Unexpected(
                $"Malformed payload from the Shopify Admin API: {ex.Message}"));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient throws TaskCanceledException (an OperationCanceledException) when its
            // own Timeout (default 100s) elapses, even though the caller's token wasn't cancelled.
            // Treat that as the source being unavailable, not as a business cancellation.
            return Result<IReadOnlyList<Sale>>.Failure(Error.Unavailable(
                "Shopify request timed out."));
        }
        // OperationCanceledException intentionally propagates when the caller cancelled.
    }

    // Flattens one Shopify order into one Sale per line item, skipping rows missing the
    // fields the domain requires so a single odd record never sinks the whole import.
    private static IEnumerable<Sale> LineItemsAsSales(OrderDto order)
    {
        if (!DateTimeOffset.TryParse(order.CreatedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var createdAt))
            yield break;

        // Use the wall-clock date of the order's offset (store-local) rather than UTC. The
        // Shopify admin and the merchant's daily reports aggregate by store-local date; an
        // order placed at 22:00 PST on Jan 15 is "Jan 15 revenue" to the merchant, not
        // "Jan 16" as `UtcDateTime` would attribute it.
        var date = DateOnly.FromDateTime(createdAt.Date);
        // Guest checkouts have no customer; map them to a stable "guest" id so the analytics
        // still aggregate them instead of dropping the line.
        var customerId = order.Customer?.Id?.ToString(CultureInfo.InvariantCulture) ?? "guest";

        foreach (var line in order.LineItems ?? [])
        {
            if (string.IsNullOrWhiteSpace(line.Title))
                continue;
            if (!decimal.TryParse(line.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                continue;

            // Subtract the line's discount so Sale.Amount stays net (matches NetAmount semantics
            // of the SAP adapter and what the merchant sees in the Shopify admin). total_discount
            // covers both line-level discounts and order-level promos prorated to this line.
            if (!decimal.TryParse(line.TotalDiscount ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture, out var discount))
                discount = 0m;

            yield return new Sale(date, customerId, line.Title, line.Quantity,
                (unitPrice * line.Quantity) - discount);
        }
    }

    // DTOs for the Shopify Admin REST JSON envelope: { "orders": [ { ..., "line_items": [...] } ] }.
    private sealed record OrdersResponse(
        [property: JsonPropertyName("orders")] OrderDto[]? Orders);

    private sealed record OrderDto(
        [property: JsonPropertyName("created_at")] string? CreatedAt,
        [property: JsonPropertyName("customer")] CustomerDto? Customer,
        [property: JsonPropertyName("line_items")] LineItemDto[]? LineItems);

    private sealed record CustomerDto(
        [property: JsonPropertyName("id")] long? Id);

    private sealed record LineItemDto(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("total_discount")] string? TotalDiscount);
}

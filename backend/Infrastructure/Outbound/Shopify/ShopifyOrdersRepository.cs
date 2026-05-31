using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Infrastructure.Outbound.Shopify;

// Outbound adapter that reads sales from a real Shopify store via the Admin REST API.
// One Shopify "order" carries N "line_items"; each line_item becomes one domain Sale.
//
// Authentication is the Client Credentials Grant flow: ShopifyTokenProvider exchanges
// client_id + client_secret for the X-Shopify-Access-Token used here. The adapter is the
// only class that knows the source speaks Shopify, that orders have line_items, and how the
// JSON fields map to the domain.
//
// TODO: paginate via the Link header's `rel="next"` cursor. The MVP fetches a single page of
// up to 250 orders, which is enough for the test dataset. See DEUDA-TECNICA.md.
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
        var resource = $"/admin/api/{apiVersion}/orders.json?status=any&limit=250";
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
            return Result<IReadOnlyList<Sale>>.Failure(Error.Validation(
                $"Malformed payload from the Shopify Admin API: {ex.Message}"));
        }
        // OperationCanceledException intentionally propagates.
    }

    // Flattens one Shopify order into one Sale per line item, skipping rows missing the
    // fields the domain requires so a single odd record never sinks the whole import.
    private static IEnumerable<Sale> LineItemsAsSales(OrderDto order)
    {
        if (!DateTimeOffset.TryParse(order.CreatedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var createdAt))
            yield break;

        var date = DateOnly.FromDateTime(createdAt.UtcDateTime);
        // Guest checkouts have no customer; map them to a stable "guest" id so the analytics
        // still aggregate them instead of dropping the line.
        var customerId = order.Customer?.Id?.ToString(CultureInfo.InvariantCulture) ?? "guest";

        foreach (var line in order.LineItems ?? [])
        {
            if (string.IsNullOrWhiteSpace(line.Title))
                continue;
            if (!decimal.TryParse(line.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                continue;

            yield return new Sale(date, customerId, line.Title, line.Quantity, unitPrice * line.Quantity);
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
        [property: JsonPropertyName("price")] string? Price);
}

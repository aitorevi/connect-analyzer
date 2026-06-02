using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConnectAnalyzer.Application.Ports;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Outbound.Sap;

public sealed class SapODataSalesRepository(HttpClient http) : ISalesRepository
{
    private const string SalesItemsResource =
        "A_SalesOrderItem?$expand=to_SalesOrder&$top=200&$format=json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            using var response = await http.GetAsync(SalesItemsResource, ct);

            // A bad/missing API key is an auth problem (401), not the source being down (502).
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Result<IReadOnlyList<Sale>>.Failure(
                    Error.Unauthorized("SAP rejected the API key (check Sap:ApiKey)."));

            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<Sale>>.Failure(
                    Error.Unavailable($"The SAP data source returned {(int)response.StatusCode}."));

            var json = await response.Content.ReadAsStringAsync(ct);
            var payload = JsonSerializer.Deserialize<ODataResponse>(json, JsonOptions);

            IReadOnlyList<Sale> sales = (payload?.D?.Results ?? [])
                .Select(TryParseSale)
                .OfType<Sale>()
                .ToList();

            return Result<IReadOnlyList<Sale>>.Success(sales);
        }
        catch (HttpRequestException ex)
        {
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unavailable($"Could not reach the SAP data source: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unexpected($"Malformed OData payload from the SAP data source: {ex.Message}"));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unavailable("SAP request timed out."));
        }
    }
    
    private static Sale? TryParseSale(SalesOrderItemDto item)
    {
        var order = item.SalesOrder;
        if (order is null)
            return null;
        if (string.IsNullOrWhiteSpace(item.Material) || string.IsNullOrWhiteSpace(order.SoldToParty))
            return null;
        if (!TryParseODataDate(order.CreationDate, out var date))
            return null;
        if (!decimal.TryParse(item.RequestedQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
            return null;
        if (!decimal.TryParse(item.NetAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return null;

        return new Sale(date, order.SoldToParty, item.Material, (int)quantity, amount);
    }
    
    private static bool TryParseODataDate(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrEmpty(raw))
            return false;

        var open = raw.IndexOf('(');
        var close = raw.IndexOf(')');
        if (open < 0 || close <= open)
            return false;

        var inner = raw[(open + 1)..close];
        var offsetSign = inner.IndexOfAny(['+', '-'], 1);
        var millisPart = offsetSign > 0 ? inner[..offsetSign] : inner;
        if (!long.TryParse(millisPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millis))
            return false;

        date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime);
        return true;
    }

    private sealed record ODataResponse([property: JsonPropertyName("d")] ODataPayload? D);

    private sealed record ODataPayload([property: JsonPropertyName("results")] SalesOrderItemDto[]? Results);

    private sealed record SalesOrderItemDto(
        [property: JsonPropertyName("Material")] string? Material,
        [property: JsonPropertyName("RequestedQuantity")] string? RequestedQuantity,
        [property: JsonPropertyName("NetAmount")] string? NetAmount,
        [property: JsonPropertyName("to_SalesOrder")] SalesOrderDto? SalesOrder);

    private sealed record SalesOrderDto(
        [property: JsonPropertyName("SoldToParty")] string? SoldToParty,
        [property: JsonPropertyName("CreationDate")] string? CreationDate);
}

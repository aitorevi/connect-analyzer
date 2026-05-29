using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Infrastructure.Outbound.Sap;

// Outbound adapter that reads sales from a real SAP S/4HANA OData service (the free
// SAP Business Accelerator Hub sandbox, API_SALES_ORDER_SRV). This is the only class that
// knows the source speaks OData v2, that authentication is an `APIKey` header, and how the
// S/4HANA fields (Material, RequestedQuantity, NetAmount, SoldToParty, CreationDate) map to
// the domain `Sale`. The APIKey header and base address are configured in the composition
// root, so the secret never lives here.
//
// It implements the same ISalesRepository port as MockTxtSalesRepository: swapping the data
// source from the mock to real SAP is a configuration switch (SalesSource), nothing above
// this adapter changes.
public sealed class SapODataSalesRepository(HttpClient http) : ISalesRepository
{
    // Query sales-order items expanding their header so each row carries the customer and
    // order date alongside the product, quantity and amount. $format=json yields OData v2 JSON.
    private const string SalesItemsResource =
        "A_SalesOrderItem?$expand=to_SalesOrder&$top=200&$format=json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default)
    {
        try
        {
            // GetStringAsync throws HttpRequestException on a non-2xx status, which we treat
            // as the source being unavailable (same contract as the other adapters).
            var json = await http.GetStringAsync(SalesItemsResource, ct);
            var payload = JsonSerializer.Deserialize<ODataResponse>(json, JsonOptions);

            IReadOnlyList<Sale> sales = (payload?.D?.Results ?? [])
                .Select(TryParseSale)
                .OfType<Sale>()
                .ToList();

            return Result<IReadOnlyList<Sale>>.Success(sales);
        }
        catch (HttpRequestException ex)
        {
            // Source unreachable or non-2xx: caught at the edge and turned into a value so
            // nothing above the adapter has to know about HttpClient.
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unavailable($"Could not reach the SAP data source: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            // A 2xx response whose body is not the OData shape we expect is unexpected, not a
            // mere availability blip.
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unexpected($"Malformed OData payload from the SAP data source: {ex.Message}"));
        }
        // OperationCanceledException is intentionally not caught: cancellation is a caller's
        // decision, not a business failure, so it propagates as the exception.
    }

    // Maps one OData sales-order item to a Sale, returning null for rows missing the fields we
    // need so a single odd record never sinks the whole import (mirrors the mock's row skipping).
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

    // OData v2 serialises Edm.DateTime as "/Date(1465776000000)/" (Unix epoch in ms, optionally
    // followed by a timezone offset like "+0000").
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

    // DTOs for the OData v2 JSON envelope: { "d": { "results": [ { ... "to_SalesOrder": {...} } ] } }.
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

using System.Globalization;
using System.Text;
using ConnectAnalyzer.Application.Ports;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Outbound.MockTxt;

public sealed class MockTxtSalesRepository(HttpClient http) : ISalesRepository
{
    private const string SalesResourcePath = "/sales.txt";
    private const string Latin1Encoding = "ISO-8859-1";
    private const string SapDateFormat = "yyyyMMdd";
    private const char FieldDelimiter = '|';

    // Column layout of the DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT wire format.
    private const int DateColumn = 0;
    private const int CustomerIdColumn = 1;
    private const int ProductNameColumn = 2;
    private const int QuantityColumn = 3;
    private const int AmountColumn = 4;
    private const int ColumnCount = 5;

    public async Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default)
    {
        try
        {
            var bytes = await http.GetByteArrayAsync(SalesResourcePath, ct);
            var latin1 = Encoding.GetEncoding(Latin1Encoding);
            var text = latin1.GetString(bytes);

            IReadOnlyList<Sale> sales = text.Split('\n')
                .Skip(1)
                .Select(l => l.TrimEnd('\r'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(FieldDelimiter))
                .Where(p => p.Length == ColumnCount)
                .Select(TryParseSale)
                .OfType<Sale>()
                .ToList();

            return Result<IReadOnlyList<Sale>>.Success(sales);
        }
        catch (HttpRequestException ex)
        {
            // The source being unreachable or returning a non-2xx status is the textbook
            // "truly exceptional" case: caught here at the edge and translated to a value
            // so nothing above the adapter has to know about HttpClient.
            return Result<IReadOnlyList<Sale>>.Failure(
                Error.Unavailable($"Could not reach the SAP data source: {ex.Message}"));
        }
        // OperationCanceledException is intentionally not caught: a cancelled request is a
        // caller's decision, not a business failure, so it must propagate as the exception.
    }

    private static Sale? TryParseSale(string[] parts)
    {
        if (!DateOnly.TryParseExact(parts[DateColumn], SapDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;
        if (!int.TryParse(parts[QuantityColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            return null;
        if (!decimal.TryParse(parts[AmountColumn], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return null;

        return new Sale(date, parts[CustomerIdColumn], parts[ProductNameColumn], quantity, amount);
    }
}

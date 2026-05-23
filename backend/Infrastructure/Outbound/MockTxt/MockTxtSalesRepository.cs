using System.Globalization;
using System.Text;
using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Infrastructure.Outbound.MockTxt;

// Outbound adapter that reads sales from a pipe-delimited Latin-1 .txt served over HTTP,
// imitating a typical SAP export. This is the only class that knows the file lives at
// /sales.txt, that it is ISO-8859-1 encoded, and that the columns are
// DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT. The mapping from these wire
// column names to the domain fields of Sale is exactly what an adapter is for.
public class MockTxtSalesRepository(HttpClient http) : ISalesRepository
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

    public async Task<IReadOnlyList<Sale>> SearchAsync(CancellationToken ct = default)
    {
        var bytes = await http.GetByteArrayAsync(SalesResourcePath, ct);
        var latin1 = Encoding.GetEncoding(Latin1Encoding);
        var text = latin1.GetString(bytes);

        return text.Split('\n')
            .Skip(1)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(FieldDelimiter))
            .Where(p => p.Length == ColumnCount)
            .Select(TryParseSale)
            .OfType<Sale>()
            .ToList();
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

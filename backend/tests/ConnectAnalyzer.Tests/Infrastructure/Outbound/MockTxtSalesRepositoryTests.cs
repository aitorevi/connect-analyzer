using System.Net;
using System.Text;
using ConnectAnalyzer.Domain;
using ConnectAnalyzer.Infrastructure.Outbound.MockTxt;
using Xunit;

namespace ConnectAnalyzer.Tests.Infrastructure.Outbound;

public class MockTxtSalesRepositoryTests
{
    [Fact]
    public async Task ParsesValidLatin1InputWithAccents()
    {
        const string text =
            "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n" +
            "20260102|C001|Café Molido|10|125.50\n" +
            "20260103|C002|Té Verde|5|42.75\n";
        var sut = CreateSut(Latin1Bytes(text));

        var sales = await SearchSucceeding(sut);

        Assert.Equal(2, sales.Count);
        Assert.Equal(new DateOnly(2026, 1, 2), sales[0].Date);
        Assert.Equal("C001", sales[0].CustomerId);
        Assert.Equal("Café Molido", sales[0].ProductName);
        Assert.Equal(10, sales[0].Quantity);
        Assert.Equal(125.50m, sales[0].Amount);
        Assert.Equal("Té Verde", sales[1].ProductName);
    }

    [Fact]
    public async Task SkipsHeaderEmptyAndMalformedLines()
    {
        const string text =
            "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n" +
            "20260102|C001|Product A|10|100.00\n" +
            "\n" +
            "this is not a valid row\n" +
            "20260103|C002|Product B|5|50.00\n" +
            "  \n";
        var sut = CreateSut(Latin1Bytes(text));

        var sales = await SearchSucceeding(sut);

        Assert.Equal(2, sales.Count);
        Assert.Equal("Product A", sales[0].ProductName);
        Assert.Equal("Product B", sales[1].ProductName);
    }

    [Fact]
    public async Task ReturnsEmptyWhenOnlyHeader()
    {
        const string text = "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n";
        var sut = CreateSut(Latin1Bytes(text));

        var sales = await SearchSucceeding(sut);

        Assert.Empty(sales);
    }

    [Fact]
    public async Task SkipsRowsWithCorruptDateQuantityOrAmount()
    {
        const string text =
            "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n" +
            "20260102|C001|Valid|10|100.00\n" +
            "notadate|C002|Bad date|5|50.00\n" +
            "20260103|C003|Bad quantity|abc|50.00\n" +
            "20260104|C004|Bad amount|5|xyz\n" +
            "20260105|C005|Also valid|2|20.00\n";
        var sut = CreateSut(Latin1Bytes(text));

        var sales = await SearchSucceeding(sut);

        Assert.Equal(2, sales.Count);
        Assert.Equal("Valid", sales[0].ProductName);
        Assert.Equal("Also valid", sales[1].ProductName);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReturnsUnavailableFailureWhenSourceReturnsHttpError(HttpStatusCode status)
    {
        var sut = CreateSut(Latin1Bytes("ignored"), status);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnavailableFailureWhenSourceIsUnreachable()
    {
        // Simulates a network-level failure (DNS, connection refused, reset): the handler
        // itself throws HttpRequestException before any response exists.
        var http = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://test-host/")
        };
        var sut = new MockTxtSalesRepository(http);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task PropagatesCancellationInsteadOfWrappingItAsFailure()
    {
        var sut = CreateSut(Latin1Bytes("ignored"));
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.SearchAsync(cancelled));
    }

    [Fact]
    public async Task ParsesAmountWithInvariantCulture()
    {
        const string text =
            "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n" +
            "20260101|C001|X|1|1234.56\n";
        var sut = CreateSut(Latin1Bytes(text));

        var sales = await SearchSucceeding(sut);

        Assert.Single(sales);
        Assert.Equal(1234.56m, sales[0].Amount);
    }

    private static async Task<IReadOnlyList<Sale>> SearchSucceeding(MockTxtSalesRepository sut)
    {
        var result = await sut.SearchAsync();
        return result.Match(
            sales => sales,
            error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));
    }

    private static Error FailureError(Result<IReadOnlyList<Sale>> result) =>
        result.Match(
            sales => throw new Xunit.Sdk.XunitException($"expected Failure but was Success with {sales.Count} rows"),
            error => error);

    private static byte[] Latin1Bytes(string s) =>
        Encoding.GetEncoding("ISO-8859-1").GetBytes(s);

    private static MockTxtSalesRepository CreateSut(
        byte[] responseBytes,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var http = new HttpClient(new StubHandler(responseBytes, status))
        {
            BaseAddress = new Uri("http://test-host/")
        };
        return new MockTxtSalesRepository(http);
    }

    private sealed class StubHandler(byte[] bytes, HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new ByteArrayContent(bytes),
            });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("simulated network failure");
    }
}

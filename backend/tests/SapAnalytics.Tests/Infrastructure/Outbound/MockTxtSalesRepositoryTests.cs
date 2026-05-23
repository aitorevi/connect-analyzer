using System.Net;
using System.Text;
using SapAnalytics.Infrastructure.Outbound.MockTxt;
using Xunit;

namespace SapAnalytics.Tests.Infrastructure.Outbound;

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

        var result = await sut.SearchAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 2), result[0].Date);
        Assert.Equal("C001", result[0].CustomerId);
        Assert.Equal("Café Molido", result[0].ProductName);
        Assert.Equal(10, result[0].Quantity);
        Assert.Equal(125.50m, result[0].Amount);
        Assert.Equal("Té Verde", result[1].ProductName);
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

        var result = await sut.SearchAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Product A", result[0].ProductName);
        Assert.Equal("Product B", result[1].ProductName);
    }

    [Fact]
    public async Task ReturnsEmptyWhenOnlyHeader()
    {
        const string text = "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n";
        var sut = CreateSut(Latin1Bytes(text));

        var result = await sut.SearchAsync();

        Assert.Empty(result);
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

        var result = await sut.SearchAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Valid", result[0].ProductName);
        Assert.Equal("Also valid", result[1].ProductName);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ThrowsWhenMockReturnsHttpError(HttpStatusCode status)
    {
        var sut = CreateSut(Latin1Bytes("ignored"), status);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.SearchAsync());
    }

    [Fact]
    public async Task ParsesAmountWithInvariantCulture()
    {
        const string text =
            "DATE|CUSTOMER_ID|PRODUCT_NAME|QUANTITY|AMOUNT\n" +
            "20260101|C001|X|1|1234.56\n";
        var sut = CreateSut(Latin1Bytes(text));

        var result = await sut.SearchAsync();

        Assert.Single(result);
        Assert.Equal(1234.56m, result[0].Amount);
    }

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
}

using System.Net;
using System.Text;
using SapAnalytics.Domain;
using SapAnalytics.Infrastructure.Outbound.Sap;
using Xunit;

namespace SapAnalytics.Tests.Infrastructure.Outbound;

public class SapODataSalesRepositoryTests
{
    // OData v2 envelope: { "d": { "results": [ items with an expanded to_SalesOrder ] } }.
    // CreationDate is the v2 "/Date(epoch-ms)/" form; decimals come as strings.
    // 1465776000000 ms = 2016-06-13, 1465862400000 ms = 2016-06-14 (UTC).
    private const string TwoItems = """
    {
      "d": {
        "results": [
          {
            "Material": "Coffee",
            "RequestedQuantity": "10.000",
            "NetAmount": "1250.50",
            "to_SalesOrder": { "SoldToParty": "C001", "CreationDate": "/Date(1465776000000)/" }
          },
          {
            "Material": "Tea",
            "RequestedQuantity": "5",
            "NetAmount": "42.75",
            "to_SalesOrder": { "SoldToParty": "C002", "CreationDate": "/Date(1465862400000)/" }
          }
        ]
      }
    }
    """;

    [Fact]
    public async Task MapsODataItemsToSales()
    {
        var sut = CreateSut(TwoItems);

        var sales = await SearchSucceeding(sut);

        Assert.Equal(2, sales.Count);
        Assert.Equal(new DateOnly(2016, 6, 13), sales[0].Date);
        Assert.Equal("C001", sales[0].CustomerId);
        Assert.Equal("Coffee", sales[0].ProductName);
        Assert.Equal(10, sales[0].Quantity);
        Assert.Equal(1250.50m, sales[0].Amount);
        Assert.Equal("Tea", sales[1].ProductName);
        Assert.Equal(new DateOnly(2016, 6, 14), sales[1].Date);
    }

    [Fact]
    public async Task SkipsItemsMissingRequiredFields()
    {
        const string json = """
        {
          "d": {
            "results": [
              { "Material": "Valid", "RequestedQuantity": "1", "NetAmount": "10.00",
                "to_SalesOrder": { "SoldToParty": "C001", "CreationDate": "/Date(1465776000000)/" } },
              { "Material": "", "RequestedQuantity": "1", "NetAmount": "10.00",
                "to_SalesOrder": { "SoldToParty": "C002", "CreationDate": "/Date(1465776000000)/" } },
              { "Material": "NoOrder", "RequestedQuantity": "1", "NetAmount": "10.00",
                "to_SalesOrder": null },
              { "Material": "BadQty", "RequestedQuantity": "abc", "NetAmount": "10.00",
                "to_SalesOrder": { "SoldToParty": "C003", "CreationDate": "/Date(1465776000000)/" } },
              { "Material": "BadDate", "RequestedQuantity": "1", "NetAmount": "10.00",
                "to_SalesOrder": { "SoldToParty": "C004", "CreationDate": "not-a-date" } },
              { "Material": "AlsoValid", "RequestedQuantity": "2", "NetAmount": "20.00",
                "to_SalesOrder": { "SoldToParty": "C005", "CreationDate": "/Date(1465776000000)/" } }
            ]
          }
        }
        """;
        var sut = CreateSut(json);

        var sales = await SearchSucceeding(sut);

        Assert.Equal(2, sales.Count);
        Assert.Equal("Valid", sales[0].ProductName);
        Assert.Equal("AlsoValid", sales[1].ProductName);
    }

    [Fact]
    public async Task ReturnsEmptyWhenNoResults()
    {
        var sut = CreateSut("""{ "d": { "results": [] } }""");

        var sales = await SearchSucceeding(sut);

        Assert.Empty(sales);
    }

    [Fact]
    public async Task ParsesAmountWithInvariantCulture()
    {
        const string json = """
        {
          "d": { "results": [
            { "Material": "X", "RequestedQuantity": "1", "NetAmount": "1234.56",
              "to_SalesOrder": { "SoldToParty": "C001", "CreationDate": "/Date(1465776000000)/" } }
          ] }
        }
        """;
        var sut = CreateSut(json);

        var sales = await SearchSucceeding(sut);

        Assert.Single(sales);
        Assert.Equal(1234.56m, sales[0].Amount);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReturnsUnavailableFailureWhenSourceReturnsHttpError(HttpStatusCode status)
    {
        var sut = CreateSut("ignored", status);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnavailableFailureWhenSourceIsUnreachable()
    {
        var http = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("https://test-host/")
        };
        var sut = new SapODataSalesRepository(http);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnexpectedFailureWhenPayloadIsMalformed()
    {
        var sut = CreateSut("this is not json {{{");

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unexpected, FailureError(result).Type);
    }

    [Fact]
    public async Task PropagatesCancellationInsteadOfWrappingItAsFailure()
    {
        var sut = CreateSut(TwoItems);
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.SearchAsync(cancelled));
    }

    private static async Task<IReadOnlyList<Sale>> SearchSucceeding(SapODataSalesRepository sut)
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

    private static SapODataSalesRepository CreateSut(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var http = new HttpClient(new StubHandler(responseJson, status))
        {
            BaseAddress = new Uri("https://test-host/")
        };
        return new SapODataSalesRepository(http);
    }

    private sealed class StubHandler(string json, HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("simulated network failure");
    }
}

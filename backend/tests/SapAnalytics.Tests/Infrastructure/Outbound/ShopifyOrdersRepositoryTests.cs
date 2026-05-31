using System.Net;
using System.Text;
using SapAnalytics.Domain;
using SapAnalytics.Infrastructure.Outbound.Shopify;
using Xunit;

namespace SapAnalytics.Tests.Infrastructure.Outbound;

public class ShopifyOrdersRepositoryTests
{
    // OAuth response from POST /admin/oauth/access_token. Apps from the Dev Dashboard return a
    // "shpat_" token (offline, no expires_in) that goes in the X-Shopify-Access-Token header.
    private const string TokenJson = """
    { "access_token": "shpat_test_token", "scope": "read_orders,read_products,read_customers" }
    """;

    // Two orders × two line items each → adapter must flatten into 4 Sales, one per line.
    private const string TwoOrdersTwoLines = """
    {
      "orders": [
        {
          "created_at": "2026-01-15T10:00:00Z",
          "customer": { "id": 1001 },
          "line_items": [
            { "title": "Widget A", "quantity": 2, "price": "10.00" },
            { "title": "Widget B", "quantity": 1, "price": "25.50" }
          ]
        },
        {
          "created_at": "2026-02-20T15:30:00Z",
          "customer": { "id": 2002 },
          "line_items": [
            { "title": "Gadget C", "quantity": 3, "price": "5.00" },
            { "title": "Gadget D", "quantity": 1, "price": "100.00" }
          ]
        }
      ]
    }
    """;

    [Fact]
    public async Task FlattensLineItemsIntoOneSalePerItem()
    {
        var sut = CreateSut(TwoOrdersTwoLines);

        var sales = await SearchSucceeding(sut);

        Assert.Equal(4, sales.Count);
        Assert.Equal(new DateOnly(2026, 1, 15), sales[0].Date);
        Assert.Equal("1001", sales[0].CustomerId);
        Assert.Equal("Widget A", sales[0].ProductName);
        Assert.Equal(2, sales[0].Quantity);
        Assert.Equal(20.00m, sales[0].Amount);
        Assert.Equal("Widget B", sales[1].ProductName);
        Assert.Equal(25.50m, sales[1].Amount);
        Assert.Equal(new DateOnly(2026, 2, 20), sales[2].Date);
        Assert.Equal("2002", sales[2].CustomerId);
        Assert.Equal(15.00m, sales[2].Amount);
        Assert.Equal(100.00m, sales[3].Amount);
    }

    [Fact]
    public async Task MapsMissingCustomerToGuest()
    {
        const string json = """
        {
          "orders": [
            {
              "created_at": "2026-03-10T09:00:00Z",
              "customer": null,
              "line_items": [ { "title": "Anonymous Buy", "quantity": 1, "price": "7.00" } ]
            }
          ]
        }
        """;
        var sut = CreateSut(json);

        var sales = await SearchSucceeding(sut);

        Assert.Single(sales);
        Assert.Equal("guest", sales[0].CustomerId);
    }

    [Fact]
    public async Task ParsesLineItemPriceWithInvariantCulture()
    {
        // "19.99" must parse as 19.99m on any host culture (es-ES would otherwise read it as 1999).
        const string json = """
        {
          "orders": [
            {
              "created_at": "2026-04-01T12:00:00Z",
              "customer": { "id": 42 },
              "line_items": [ { "title": "Decimal Test", "quantity": 2, "price": "19.99" } ]
            }
          ]
        }
        """;
        var sut = CreateSut(json);

        var sales = await SearchSucceeding(sut);

        Assert.Single(sales);
        Assert.Equal(39.98m, sales[0].Amount);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenOrdersEndpointReturns401()
    {
        var sut = CreateSut("ignored", ordersStatus: HttpStatusCode.Unauthorized);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unauthorized, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenTokenEndpointReturns401()
    {
        var handler = new RoutingHandler(
            tokenJson: """{ "error": "invalid_client" }""",
            tokenStatus: HttpStatusCode.Unauthorized,
            ordersJson: "ignored",
            ordersStatus: HttpStatusCode.OK);
        var sut = CreateSutCustom(handler);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unauthorized, FailureError(result).Type);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task ReturnsUnavailableWhenOrdersEndpointReturns5xx(HttpStatusCode status)
    {
        var sut = CreateSut("ignored", ordersStatus: status);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnavailableWhenOrdersEndpointReturns429()
    {
        var sut = CreateSut("ignored", ordersStatus: (HttpStatusCode)429);

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsUnavailableWhenSourceIsUnreachable()
    {
        var http = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("https://test-shop.myshopify.com/"),
        };
        var tokens = new ShopifyTokenProvider(http, "client-id", "client-secret");
        var sut = new ShopifyOrdersRepository(http, tokens, "2025-01");

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    [Fact]
    public async Task ReturnsValidationWhenOrdersPayloadIsMalformed()
    {
        var sut = CreateSut("this is not json {{{");

        var result = await sut.SearchAsync();

        Assert.Equal(ErrorType.Validation, FailureError(result).Type);
    }

    [Fact]
    public async Task PropagatesCancellationInsteadOfWrappingItAsFailure()
    {
        var sut = CreateSut(TwoOrdersTwoLines);
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.SearchAsync(cancelled));
    }

    [Fact]
    public async Task SendsAccessTokenHeaderOnOrdersRequest()
    {
        var handler = new RoutingHandler(TokenJson, HttpStatusCode.OK, """{ "orders": [] }""", HttpStatusCode.OK);
        var sut = CreateSutCustom(handler);

        _ = await sut.SearchAsync();

        Assert.NotNull(handler.LastOrdersRequest);
        Assert.True(handler.LastOrdersRequest!.Headers.Contains("X-Shopify-Access-Token"));
        Assert.Equal("shpat_test_token",
            handler.LastOrdersRequest!.Headers.GetValues("X-Shopify-Access-Token").Single());
    }

    [Fact]
    public async Task ReusesCachedTokenAcrossSearches()
    {
        // The token endpoint is hit at most once per process lifetime: subsequent SearchAsync
        // calls reuse the cached access token. Guards against accidental token-storms.
        var handler = new RoutingHandler(TokenJson, HttpStatusCode.OK, """{ "orders": [] }""", HttpStatusCode.OK);
        var sut = CreateSutCustom(handler);

        _ = await sut.SearchAsync();
        _ = await sut.SearchAsync();
        _ = await sut.SearchAsync();

        Assert.Equal(1, handler.TokenCallCount);
        Assert.Equal(3, handler.OrdersCallCount);
    }

    private static async Task<IReadOnlyList<Sale>> SearchSucceeding(ShopifyOrdersRepository sut)
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

    private static ShopifyOrdersRepository CreateSut(
        string ordersJson,
        HttpStatusCode ordersStatus = HttpStatusCode.OK)
    {
        var handler = new RoutingHandler(TokenJson, HttpStatusCode.OK, ordersJson, ordersStatus);
        return CreateSutCustom(handler);
    }

    private static ShopifyOrdersRepository CreateSutCustom(RoutingHandler handler)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test-shop.myshopify.com/"),
        };
        var tokens = new ShopifyTokenProvider(http, "client-id", "client-secret");
        return new ShopifyOrdersRepository(http, tokens, "2025-01");
    }

    // Routes the two endpoints the adapter hits: the OAuth token exchange (POST .../oauth/access_token)
    // and the orders fetch (GET .../orders.json). Each response is independently configurable so
    // tests can simulate success on one and failure on the other.
    private sealed class RoutingHandler(
        string tokenJson,
        HttpStatusCode tokenStatus,
        string ordersJson,
        HttpStatusCode ordersStatus) : HttpMessageHandler
    {
        public HttpRequestMessage? LastOrdersRequest { get; private set; }
        public int TokenCallCount { get; private set; }
        public int OrdersCallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("/oauth/access_token"))
            {
                TokenCallCount++;
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = tokenStatus,
                    Content = new StringContent(tokenJson, Encoding.UTF8, "application/json"),
                });
            }

            OrdersCallCount++;
            LastOrdersRequest = request;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = ordersStatus,
                Content = new StringContent(ordersJson, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("simulated network failure");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ConnectAnalytics.Application.Ports;
using ConnectAnalytics.Domain;
using ConnectAnalytics.Tests.TestDoubles;
using Xunit;

namespace ConnectAnalytics.Tests.Api;
public class SalesEndpointsTests(SalesEndpointsTests.Factory factory)
    : IClassFixture<SalesEndpointsTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsAllSales()
    {
        var response = await _client.GetAsync("/api/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sales = await response.Content.ReadFromJsonAsync<List<SaleDto>>(JsonOpts);

        Assert.NotNull(sales);
        Assert.Equal(3, sales!.Count);
        Assert.Equal("C001", sales[0].CustomerId);
        Assert.Equal("Café Molido", sales[0].ProductName);
        Assert.Equal(100m, sales[0].Amount);
    }

    [Fact]
    public async Task ByProduct_ReturnsAggregatedAndOrderedDesc()
    {
        var response = await _client.GetAsync("/api/sales/by-product");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var aggregate = await response.Content.ReadFromJsonAsync<List<ProductTotal>>(JsonOpts);

        Assert.NotNull(aggregate);
        Assert.Equal(2, aggregate!.Count);
        Assert.Equal("Café Molido", aggregate[0].Product);
        Assert.Equal(130m, aggregate[0].TotalAmount);
        Assert.Equal("Té Verde", aggregate[1].Product);
        Assert.Equal(50m, aggregate[1].TotalAmount);
    }

    [Fact]
    public async Task ByCustomer_ReturnsAggregatedAndOrderedDesc()
    {
        var response = await _client.GetAsync("/api/sales/by-customer");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var aggregate = await response.Content.ReadFromJsonAsync<List<CustomerTotal>>(JsonOpts);

        Assert.NotNull(aggregate);
        Assert.Equal(2, aggregate!.Count);
        Assert.Equal("C001", aggregate[0].CustomerId);
        Assert.Equal(130m, aggregate[0].TotalAmount);
        Assert.Equal("C002", aggregate[1].CustomerId);
        Assert.Equal(50m, aggregate[1].TotalAmount);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ISalesStore));
                services.AddSingleton<ISalesStore>(StubSalesStore.Containing(
                    new Sale(new DateOnly(2026, 1, 1), "C001", "Café Molido", 10, 100m),
                    new Sale(new DateOnly(2026, 1, 2), "C002", "Té Verde", 5, 50m),
                    new Sale(new DateOnly(2026, 1, 3), "C001", "Café Molido", 2, 30m)));
            });
        }
    }

    private record SaleDto(DateOnly Date, string CustomerId, string ProductName, int Quantity, decimal Amount);
    private record ProductTotal(string Product, decimal TotalAmount);
    private record CustomerTotal(string CustomerId, decimal TotalAmount);
}

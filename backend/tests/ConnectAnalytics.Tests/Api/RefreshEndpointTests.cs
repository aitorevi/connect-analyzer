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

public class RefreshEndpointTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Refresh_IngestsFromSourceIntoStoreAndReturnsCount()
    {
        var store = StubSalesStore.Containing(); // starts empty
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ISalesRepository));
                services.AddSingleton<ISalesRepository>(new FakeSalesRepository());
                services.RemoveAll(typeof(ISalesStore));
                services.AddSingleton<ISalesStore>(store);
            });
        });
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/sales/refresh", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RefreshResponse>(JsonOpts);
        Assert.Equal(3, body!.Ingested);
        Assert.NotNull(store.LastSaved);
        Assert.Equal(3, store.LastSaved!.Count);
    }

    private record RefreshResponse(int Ingested);

    private sealed class FakeSalesRepository : ISalesRepository
    {
        public Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default)
        {
            IReadOnlyList<Sale> sales =
            [
                new(new DateOnly(2026, 1, 1), "C001", "Café Molido", 10, 100m),
                new(new DateOnly(2026, 1, 2), "C002", "Té Verde", 5, 50m),
                new(new DateOnly(2026, 1, 3), "C001", "Café Molido", 2, 30m),
            ];
            return Task.FromResult(Result<IReadOnlyList<Sale>>.Success(sales));
        }
    }
}

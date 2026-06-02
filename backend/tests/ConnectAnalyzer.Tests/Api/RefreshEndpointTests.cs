using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ConnectAnalyzer.Application.Ports;
using ConnectAnalyzer.Domain;
using ConnectAnalyzer.Tests.TestDoubles;
using Xunit;

namespace ConnectAnalyzer.Tests.Api;

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
        using var factory = CreateFactory(store, refreshToken: null);
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/sales/refresh", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RefreshResponse>(JsonOpts);
        Assert.Equal(3, body!.Ingested);
        Assert.NotNull(store.LastSaved);
        Assert.Equal(3, store.LastSaved!.Count);
    }

    [Fact]
    public async Task Refresh_WithTokenConfigured_RejectsRequestWithoutMatchingHeader()
    {
        using var factory = CreateFactory(StubSalesStore.Containing(), refreshToken: "s3cret");
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/sales/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithTokenConfigured_AcceptsMatchingHeader()
    {
        using var factory = CreateFactory(StubSalesStore.Containing(), refreshToken: "s3cret");
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/refresh");
        request.Headers.Add("X-Refresh-Token", "s3cret");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        StubSalesStore store, string? refreshToken) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            if (refreshToken is not null)
                builder.UseSetting("Refresh:Token", refreshToken);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ISalesRepository));
                services.AddSingleton<ISalesRepository>(new FakeSalesRepository());
                services.RemoveAll(typeof(ISalesStore));
                services.AddSingleton<ISalesStore>(store);
            });
        });

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

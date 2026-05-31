using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ConnectAnalytics.Application.Ports;
using ConnectAnalytics.Domain;
using ConnectAnalytics.Tests.TestDoubles;
using Xunit;

namespace ConnectAnalytics.Tests.Api;

// End-to-end through the real controller + service: only the store is faked, so a Failure
// travels the whole pipeline and we assert the HTTP status it produces.
public class ErrorMappingEndpointsTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorType.Validation, HttpStatusCode.BadRequest)]
    [InlineData(ErrorType.Unavailable, HttpStatusCode.BadGateway)]
    [InlineData(ErrorType.Unexpected, HttpStatusCode.InternalServerError)]
    public async Task Failure_IsTranslatedToItsHttpStatus(ErrorType type, HttpStatusCode expected)
    {
        var error = new Error(type, "boom");
        using var factory = FactoryReturning(Result<IReadOnlyList<Sale>>.Failure(error));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sales");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Success_Returns200()
    {
        var sale = new Sale(new DateOnly(2026, 1, 1), "C001", "Café", 1, 10m);
        using var factory = FactoryReturning(Result<IReadOnlyList<Sale>>.Success([sale]));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> FactoryReturning(Result<IReadOnlyList<Sale>> result) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ISalesStore));
                services.AddSingleton<ISalesStore>(new StubSalesStore(result));
            });
        });
}

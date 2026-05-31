using ConnectAnalytics.Application;
using ConnectAnalytics.Domain;
using ConnectAnalytics.Tests.TestDoubles;
using Xunit;

namespace ConnectAnalytics.Tests.Application;

public class SalesAnalyticsTests
{
    [Fact]
    public async Task TotalsByProduct_OnStoreFailure_PropagatesFailureWithoutAggregating()
    {
        var error = Error.Unavailable("store down");
        var service = new SalesAnalytics(StubSalesStore.FailingRead(error));

        var result = await service.TotalsByProductAsync();

        // The Map never runs the aggregation on a Failure: the very same error comes back.
        Assert.Same(error, FailureError(result));
    }

    [Fact]
    public async Task TotalsByCustomer_OnStoreFailure_PropagatesFailureWithoutAggregating()
    {
        var error = Error.Unavailable("store down");
        var service = new SalesAnalytics(StubSalesStore.FailingRead(error));

        var result = await service.TotalsByCustomerAsync();

        Assert.Same(error, FailureError(result));
    }

    [Fact]
    public async Task TotalsByProduct_OnSuccess_AggregatesGroupedAndOrderedDescending()
    {
        var service = new SalesAnalytics(StubSalesStore.Containing(
            new Sale(new DateOnly(2026, 1, 1), "C001", "Café", 10, 100m),
            new Sale(new DateOnly(2026, 1, 2), "C002", "Té", 5, 50m),
            new Sale(new DateOnly(2026, 1, 3), "C001", "Café", 2, 30m)));

        var totals = Unwrap(await service.TotalsByProductAsync());

        Assert.Equal(2, totals.Count);
        Assert.Equal("Café", totals[0].Product);
        Assert.Equal(130m, totals[0].TotalAmount);
        Assert.Equal("Té", totals[1].Product);
        Assert.Equal(50m, totals[1].TotalAmount);
    }

    [Fact]
    public async Task TotalsByCustomer_OnSuccess_AggregatesGroupedAndOrderedDescending()
    {
        var service = new SalesAnalytics(StubSalesStore.Containing(
            new Sale(new DateOnly(2026, 1, 1), "C001", "Café", 10, 100m),
            new Sale(new DateOnly(2026, 1, 2), "C002", "Té", 5, 50m),
            new Sale(new DateOnly(2026, 1, 3), "C001", "Café", 2, 30m)));

        var totals = Unwrap(await service.TotalsByCustomerAsync());

        Assert.Equal(2, totals.Count);
        Assert.Equal("C001", totals[0].CustomerId);
        Assert.Equal(130m, totals[0].TotalAmount);
        Assert.Equal("C002", totals[1].CustomerId);
        Assert.Equal(50m, totals[1].TotalAmount);
    }

    private static T Unwrap<T>(Result<T> result) =>
        result.Match(value => value, error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static Error FailureError<T>(Result<T> result) =>
        result.Match(value => throw new Xunit.Sdk.XunitException("expected Failure but was Success"), error => error);
}

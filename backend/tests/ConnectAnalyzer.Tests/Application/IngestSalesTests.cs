using ConnectAnalyzer.Application;
using ConnectAnalyzer.Domain;
using ConnectAnalyzer.Tests.TestDoubles;
using Xunit;

namespace ConnectAnalyzer.Tests.Application;

public class IngestSalesTests
{
    [Fact]
    public async Task OnSuccess_SavesSourceSalesAndReturnsCount()
    {
        var sale = new Sale(new DateOnly(2026, 1, 1), "C001", "Café", 3, 30m);
        var store = StubSalesStore.Containing();
        var ingest = new IngestSales(StubSalesRepository.Returning(sale), store);

        var count = Unwrap(await ingest.ExecuteAsync());

        Assert.Equal(1, count);
        Assert.NotNull(store.LastSaved);
        Assert.Same(sale, store.LastSaved![0]);
    }

    [Fact]
    public async Task OnSourceFailure_ShortCircuitsAndDoesNotSave()
    {
        var error = Error.Unavailable("source down");
        var store = StubSalesStore.Containing();
        var ingest = new IngestSales(StubSalesRepository.Failing(error), store);

        var result = await ingest.ExecuteAsync();

        Assert.Same(error, FailureError(result));
        Assert.Null(store.LastSaved); // save never ran
    }

    [Fact]
    public async Task OnStoreFailure_PropagatesTheStoreError()
    {
        var error = Error.Unavailable("store down");
        var ingest = new IngestSales(
            StubSalesRepository.Returning(new Sale(new DateOnly(2026, 1, 1), "C001", "Café", 1, 1m)),
            StubSalesStore.FailingSave(error));

        var result = await ingest.ExecuteAsync();

        Assert.Same(error, FailureError(result));
    }

    private static T Unwrap<T>(Result<T> result) =>
        result.Match(value => value, error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static Error FailureError<T>(Result<T> result) =>
        result.Match(_ => throw new Xunit.Sdk.XunitException("expected Failure but was Success"), error => error);
}

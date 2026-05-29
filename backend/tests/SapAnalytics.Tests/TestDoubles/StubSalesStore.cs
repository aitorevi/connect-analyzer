using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Tests.TestDoubles;

// Configurable test double for the store port. ReadAllAsync returns a preset Result; SaveAsync
// records what it was given and returns success unless a save error was configured. Touches no
// database, so it isolates the application/HTTP layers from SQLite in tests.
public sealed class StubSalesStore(Result<IReadOnlyList<Sale>> readResult, Error? saveError = null) : ISalesStore
{
    public IReadOnlyList<Sale>? LastSaved { get; private set; }

    public static StubSalesStore Containing(params Sale[] sales) =>
        new(Result<IReadOnlyList<Sale>>.Success(sales));

    public static StubSalesStore FailingRead(Error error) =>
        new(Result<IReadOnlyList<Sale>>.Failure(error));

    public static StubSalesStore FailingSave(Error error) =>
        new(Result<IReadOnlyList<Sale>>.Success([]), error);

    public Task<Result<int>> SaveAsync(IReadOnlyList<Sale> sales, CancellationToken ct = default)
    {
        LastSaved = sales;
        return Task.FromResult(saveError is null
            ? Result<int>.Success(sales.Count)
            : Result<int>.Failure(saveError));
    }

    public Task<Result<IReadOnlyList<Sale>>> ReadAllAsync(CancellationToken ct = default) =>
        Task.FromResult(readResult);
}

using ConnectAnalytics.Application.Ports;
using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Tests.TestDoubles;

// Configurable test double: returns whatever Result it is given, touching no network or
// files. Because the service and controller depend on ISalesRepository (the interface),
// this swaps the data source out in tests. Constructor injection at the service level,
// or registered in place of the real adapter for controller integration tests.
public sealed class StubSalesRepository(Result<IReadOnlyList<Sale>> result) : ISalesRepository
{
    public static StubSalesRepository Returning(params Sale[] sales) =>
        new(Result<IReadOnlyList<Sale>>.Success(sales));

    public static StubSalesRepository Failing(Error error) =>
        new(Result<IReadOnlyList<Sale>>.Failure(error));

    public Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default) =>
        Task.FromResult(result);
}

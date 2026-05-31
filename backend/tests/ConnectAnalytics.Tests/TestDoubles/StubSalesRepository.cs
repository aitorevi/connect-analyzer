using ConnectAnalytics.Application.Ports;
using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Tests.TestDoubles;

public sealed class StubSalesRepository(Result<IReadOnlyList<Sale>> result) : ISalesRepository
{
    public static StubSalesRepository Returning(params Sale[] sales) =>
        new(Result<IReadOnlyList<Sale>>.Success(sales));

    public static StubSalesRepository Failing(Error error) =>
        new(Result<IReadOnlyList<Sale>>.Failure(error));

    public Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default) =>
        Task.FromResult(result);
}

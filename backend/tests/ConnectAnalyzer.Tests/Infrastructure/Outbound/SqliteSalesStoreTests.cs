using ConnectAnalyzer.Domain;
using ConnectAnalyzer.Infrastructure.Outbound.Sqlite;
using Xunit;

namespace ConnectAnalyzer.Tests.Infrastructure.Outbound;

public sealed class SqliteSalesStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"connectanalyzer-{Guid.NewGuid():N}.db");

    private SqliteSalesStore CreateSut() => new($"Data Source={_dbPath}");

    [Fact]
    public async Task SaveThenReadAll_RoundTripsEveryFieldExactly()
    {
        var sut = CreateSut();
        IReadOnlyList<Sale> sales =
        [
            new(new DateOnly(2026, 1, 2), "C001", "Café Molido", 10, 125.50m),
            new(new DateOnly(2026, 3, 14), "C002", "Té Verde", 5, 42.75m),
        ];

        var saved = await SucceedingInt(sut.SaveAsync(sales));
        var readBack = await Succeeding(sut.ReadAllAsync());

        Assert.Equal(2, saved);
        Assert.Equal(2, readBack.Count);
        Assert.Equal(new DateOnly(2026, 1, 2), readBack[0].Date);
        Assert.Equal("C001", readBack[0].CustomerId);
        Assert.Equal("Café Molido", readBack[0].ProductName);
        Assert.Equal(10, readBack[0].Quantity);
        Assert.Equal(125.50m, readBack[0].Amount);
        Assert.Equal("Té Verde", readBack[1].ProductName);
    }

    [Fact]
    public async Task Save_ReplacesPreviousContent()
    {
        var sut = CreateSut();
        await SucceedingInt(sut.SaveAsync([new Sale(new DateOnly(2026, 1, 1), "OLD", "Old", 1, 1m)]));

        await SucceedingInt(sut.SaveAsync([new Sale(new DateOnly(2026, 2, 2), "NEW", "New", 2, 2m)]));
        var readBack = await Succeeding(sut.ReadAllAsync());

        Assert.Single(readBack);
        Assert.Equal("NEW", readBack[0].CustomerId);
    }

    [Fact]
    public async Task ReadAll_OnFreshStore_ReturnsEmpty()
    {
        var sut = CreateSut();

        var readBack = await Succeeding(sut.ReadAllAsync());

        Assert.Empty(readBack);
    }

    [Fact]
    public async Task Save_OnUnwritablePath_ReturnsUnavailableFailure()
    {
        var sut = new SqliteSalesStore("Data Source=/no/such/directory/exists/sales.db");

        var result = await sut.SaveAsync([new Sale(new DateOnly(2026, 1, 1), "C001", "X", 1, 1m)]);

        Assert.Equal(ErrorType.Unavailable, FailureError(result).Type);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private static async Task<IReadOnlyList<Sale>> Succeeding(Task<Result<IReadOnlyList<Sale>>> task) =>
        (await task).Match(
            sales => sales,
            error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static async Task<int> SucceedingInt(Task<Result<int>> task) =>
        (await task).Match(
            count => count,
            error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static Error FailureError<T>(Result<T> result) =>
        result.Match(
            _ => throw new Xunit.Sdk.XunitException("expected Failure but was Success"),
            error => error);
}

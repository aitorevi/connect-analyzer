using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Application.Ports;

public interface ISalesStore
{ 
    Task<Result<int>> SaveAsync(IReadOnlyList<Sale> sales, CancellationToken ct = default);

    Task<Result<IReadOnlyList<Sale>>> ReadAllAsync(CancellationToken ct = default);
}

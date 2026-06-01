using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Application.Ports;

public interface ISalesRepository
{
    Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default);
}

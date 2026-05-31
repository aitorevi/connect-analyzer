using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Application.Ports;

public interface ISalesRepository
{
    Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default);
}

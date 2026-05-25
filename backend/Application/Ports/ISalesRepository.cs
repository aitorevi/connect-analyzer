using SapAnalytics.Domain;

namespace SapAnalytics.Application.Ports;

// Outbound port. The application depends on this contract; concrete data sources
// (mock, HANA, OData, ECC/RFC) are adapters that implement it.
// Filtering will be added in step 2 as a SalesFilter parameter.
//
// Returns Result instead of throwing: expected failures (source unavailable, malformed
// payload) are values the adapter translates at its edge, so nothing above the adapter
// ever sees an infrastructure exception.
public interface ISalesRepository
{
    Task<Result<IReadOnlyList<Sale>>> SearchAsync(CancellationToken ct = default);
}

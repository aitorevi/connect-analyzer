using SapAnalytics.Domain;

namespace SapAnalytics.Application.Ports;

// Outbound port. The application depends on this contract; concrete data sources
// (mock, HANA, OData, ECC/RFC) are adapters that implement it.
// Filtering will be added in step 2 as a SalesFilter parameter.
public interface ISalesRepository
{
    Task<IReadOnlyList<Sale>> SearchAsync(CancellationToken ct = default);
}

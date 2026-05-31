using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Application.Ports;

// Outbound port for the application's own store of sales. It is separate from ISalesRepository
// (the upstream source): ingestion writes here, analytics read from here. The concrete store
// (today SQLite) is an outbound adapter; nothing above this port knows the storage technology.
//
// Returns Result instead of throwing: storage failures (locked db, unreachable file) are values
// the adapter translates at its edge.
public interface ISalesStore
{
    // Replaces the stored sales with the given set; returns the number of rows persisted.
    Task<Result<int>> SaveAsync(IReadOnlyList<Sale> sales, CancellationToken ct = default);

    Task<Result<IReadOnlyList<Sale>>> ReadAllAsync(CancellationToken ct = default);
}

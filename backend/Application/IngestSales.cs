using ConnectAnalytics.Application.Ports;
using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Application;

// ETL use case: pulls the current sales from the configured source (mock or real SAP) and
// persists them in the local store, replacing what was there. Returns the number of rows ingested.
//
// Chains the two steps with BindAsync without unwrapping: if reading the source fails, the save
// never runs and that very failure comes back; same if the save fails. Neither step's error is
// inspected here.
public sealed class IngestSales(ISalesRepository source, ISalesStore store)
{
    public Task<Result<int>> ExecuteAsync(CancellationToken ct = default) =>
        source.SearchAsync(ct).BindAsync(sales => store.SaveAsync(sales, ct));
}

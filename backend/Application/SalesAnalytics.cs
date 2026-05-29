using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Application;

// Reads sales from the local store (not the upstream source): analytics are served from what
// IngestSales has persisted, decoupling queries from the source's availability.
public sealed class SalesAnalytics(ISalesStore store)
{
    public Task<Result<IReadOnlyList<Sale>>> GetAllAsync(CancellationToken ct = default) =>
        store.ReadAllAsync(ct);

    public async Task<Result<IReadOnlyList<ProductTotal>>> TotalsByProductAsync(CancellationToken ct = default)
    {
        var sales = await store.ReadAllAsync(ct);
        return sales.Map(AggregateByProduct);
    }

    public async Task<Result<IReadOnlyList<CustomerTotal>>> TotalsByCustomerAsync(CancellationToken ct = default)
    {
        var sales = await store.ReadAllAsync(ct);
        return sales.Map(AggregateByCustomer);
    }

    private static IReadOnlyList<ProductTotal> AggregateByProduct(IReadOnlyList<Sale> sales) =>
        sales
            .GroupBy(sale => sale.ProductName)
            .Select(group => new ProductTotal(group.Key, group.Sum(sale => sale.Amount)))
            .OrderByDescending(row => row.TotalAmount)
            .ToList();

    private static IReadOnlyList<CustomerTotal> AggregateByCustomer(IReadOnlyList<Sale> sales) =>
        sales
            .GroupBy(sale => sale.CustomerId)
            .Select(group => new CustomerTotal(group.Key, group.Sum(sale => sale.Amount)))
            .OrderByDescending(row => row.TotalAmount)
            .ToList();
}

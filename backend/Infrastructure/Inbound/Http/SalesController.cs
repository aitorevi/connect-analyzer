using Microsoft.AspNetCore.Mvc;
using SapAnalytics.Application.Ports;
using SapAnalytics.Domain;

namespace SapAnalytics.Infrastructure.Inbound.Http;

// Inbound adapter: translates HTTP requests into calls against the application port.
// Aggregation in memory here is temporary; in step 3 it moves to the port so each
// outbound adapter can push it down to its source (SQL GROUP BY, OData $apply, etc.).
[ApiController]
[Route("api/sales")]
public class SalesController(ISalesRepository sales) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Sale>>> GetAll(CancellationToken ct)
    {
        var result = await sales.SearchAsync(ct);
        return Ok(result);
    }

    [HttpGet("by-product")]
    public async Task<ActionResult> ByProduct(CancellationToken ct)
    {
        var result = await sales.SearchAsync(ct);
        var aggregate = result
            .GroupBy(sale => sale.ProductName)
            .Select(productSales => new { Product = productSales.Key, TotalAmount = productSales.Sum(sale => sale.Amount) })
            .OrderByDescending(row => row.TotalAmount)
            .ToList();
        return Ok(aggregate);
    }

    [HttpGet("by-customer")]
    public async Task<ActionResult> ByCustomer(CancellationToken ct)
    {
        var result = await sales.SearchAsync(ct);
        var aggregate = result
            .GroupBy(sale => sale.CustomerId)
            .Select(customerSales => new { CustomerId = customerSales.Key, TotalAmount = customerSales.Sum(sale => sale.Amount) })
            .OrderByDescending(row => row.TotalAmount)
            .ToList();
        return Ok(aggregate);
    }
}

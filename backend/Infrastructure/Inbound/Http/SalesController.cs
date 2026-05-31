using Microsoft.AspNetCore.Mvc;
using ConnectAnalytics.Application;

namespace ConnectAnalytics.Infrastructure.Inbound.Http;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(SalesAnalytics analytics, IngestSales ingest) : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var result = await ingest.ExecuteAsync(ct);
        return result.Match<IActionResult>(
            ingested => Ok(new { ingested }),
            ErrorHttpResults.ToActionResult);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await analytics.GetAllAsync(ct);
        return result.Match<IActionResult>(Ok, ErrorHttpResults.ToActionResult);
    }

    [HttpGet("by-product")]
    public async Task<IActionResult> ByProduct(CancellationToken ct)
    {
        var result = await analytics.TotalsByProductAsync(ct);
        return result.Match<IActionResult>(Ok, ErrorHttpResults.ToActionResult);
    }

    [HttpGet("by-customer")]
    public async Task<IActionResult> ByCustomer(CancellationToken ct)
    {
        var result = await analytics.TotalsByCustomerAsync(ct);
        return result.Match<IActionResult>(Ok, ErrorHttpResults.ToActionResult);
    }
}

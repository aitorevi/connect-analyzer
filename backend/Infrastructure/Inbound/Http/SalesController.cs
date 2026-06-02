using Microsoft.AspNetCore.Mvc;
using ConnectAnalyzer.Application;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Inbound.Http;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(
    SalesAnalytics analytics,
    IngestSales ingest,
    ILogger<SalesController> logger) : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var result = await ingest.ExecuteAsync(ct);
        return result.Match<IActionResult>(ingested => Ok(new { ingested }), Fail);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await analytics.GetAllAsync(ct);
        return result.Match<IActionResult>(Ok, Fail);
    }

    [HttpGet("by-product")]
    public async Task<IActionResult> ByProduct(CancellationToken ct)
    {
        var result = await analytics.TotalsByProductAsync(ct);
        return result.Match<IActionResult>(Ok, Fail);
    }

    [HttpGet("by-customer")]
    public async Task<IActionResult> ByCustomer(CancellationToken ct)
    {
        var result = await analytics.TotalsByCustomerAsync(ct);
        return result.Match<IActionResult>(Ok, Fail);
    }

    // Logs the real error detail server-side (it's stripped from the client response by
    // ErrorHttpResults for server-side failures), then maps to the HTTP status.
    private IActionResult Fail(Error error)
    {
        logger.LogWarning("Sales request failed: {Type} {Message}", error.Type, error.Message);
        return ErrorHttpResults.ToActionResult(error);
    }
}

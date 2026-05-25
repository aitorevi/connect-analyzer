using Microsoft.AspNetCore.Mvc;
using SapAnalytics.Application;

namespace SapAnalytics.Infrastructure.Inbound.Http;

// Inbound adapter: translates HTTP requests into calls against the application service.
// This is the only place where a Result is "opened" (via Match): Success -> 200 with the
// data, Failure -> the error is translated to its HTTP status in one single point.
[ApiController]
[Route("api/sales")]
public sealed class SalesController(SalesAnalytics analytics) : ControllerBase
{
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

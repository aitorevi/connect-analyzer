using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ConnectAnalyzer.Application;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Inbound.Http;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(
    SalesAnalytics analytics,
    IngestSales ingest,
    ILogger<SalesController> logger,
    IConfiguration configuration) : ControllerBase
{
    private const string RefreshTokenHeader = "X-Refresh-Token";

    // Re-ingestion is a write/admin action. If Refresh:Token is configured, require it in the
    // X-Refresh-Token header (constant-time compare); if it isn't, the endpoint stays open
    // (demo default). Protects against anyone triggering ingestion against the real source.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var requiredToken = configuration["Refresh:Token"];
        if (!string.IsNullOrEmpty(requiredToken) && !RefreshTokenMatches(requiredToken))
            return Unauthorized();

        var result = await ingest.ExecuteAsync(ct);
        return result.Match<IActionResult>(ingested => Ok(new { ingested }), Fail);
    }

    private bool RefreshTokenMatches(string requiredToken)
    {
        var provided = Request.Headers[RefreshTokenHeader].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(requiredToken));
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

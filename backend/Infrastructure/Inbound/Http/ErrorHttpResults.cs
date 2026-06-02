using Microsoft.AspNetCore.Mvc;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Inbound.Http;

public static class ErrorHttpResults
{
    public static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Unavailable => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static IActionResult ToActionResult(Error error)
    {
        var status = StatusFor(error.Type);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type.ToString(),
            Detail = ClientDetail(error),
        };
        return new ObjectResult(problem) { StatusCode = status };
    }

    // Server-side failures carry upstream/infra exception text (SAP/Shopify/SQLite messages); keep
    // it out of the client response (the controller logs the real detail). Client-facing errors
    // (NotFound/Validation/Unauthorized) carry intentional, safe domain messages.
    private static string ClientDetail(Error error) => error.Type switch
    {
        ErrorType.Unavailable => "The data source is currently unavailable.",
        ErrorType.Unexpected => "An unexpected error occurred.",
        _ => error.Message,
    };
}

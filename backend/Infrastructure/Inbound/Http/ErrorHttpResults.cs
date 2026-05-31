using Microsoft.AspNetCore.Mvc;
using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Infrastructure.Inbound.Http;

// The single point that translates a domain Error into an HTTP response. Keeping HTTP
// knowledge here (the inbound adapter), not in the domain, is what lets Error stay a pure
// domain concept. Add a case here when a new ErrorType appears.
public static class ErrorHttpResults
{
    public static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Unavailable => StatusCodes.Status502BadGateway,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static IActionResult ToActionResult(Error error)
    {
        var status = StatusFor(error.Type);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type.ToString(),
            Detail = error.Message,
        };
        return new ObjectResult(problem) { StatusCode = status };
    }
}

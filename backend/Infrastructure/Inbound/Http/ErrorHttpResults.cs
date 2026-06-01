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
            Detail = error.Message,
        };
        return new ObjectResult(problem) { StatusCode = status };
    }
}

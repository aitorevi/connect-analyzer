using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ConnectAnalytics.Domain;
using ConnectAnalytics.Infrastructure.Inbound.Http;
using Xunit;

namespace ConnectAnalytics.Tests.Infrastructure.Inbound.Http;

public class ErrorHttpResultsTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    public void StatusFor_MapsEachErrorTypeToItsHttpStatus(ErrorType type, int expectedStatus)
    {
        Assert.Equal(expectedStatus, ErrorHttpResults.StatusFor(type));
    }

    [Fact]
    public void ToActionResult_CarriesStatusAndMessageInProblemDetails()
    {
        var result = ErrorHttpResults.ToActionResult(Error.NotFound("customer C999 not found"));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("customer C999 not found", problem.Detail);
    }
}

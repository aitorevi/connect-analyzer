using ConnectAnalytics.Domain;
using Xunit;

namespace ConnectAnalytics.Tests.Domain;

public class ResultExtensionsTests
{
    [Fact]
    public async Task BindAsync_OnSuccess_RunsTheNextStep()
    {
        var start = Task.FromResult(Result<int>.Success(2));

        var result = await start.BindAsync(n => Task.FromResult(Result<string>.Success($"n={n}")));

        Assert.Equal("n=2", Unwrap(result));
    }

    [Fact]
    public async Task BindAsync_OnFailure_DoesNotRunTheNextStepAndPropagatesError()
    {
        var error = Error.Unavailable("boom");
        var start = Task.FromResult(Result<int>.Failure(error));
        var ran = false;

        var result = await start.BindAsync(n =>
        {
            ran = true;
            return Task.FromResult(Result<string>.Success("unreachable"));
        });

        Assert.False(ran);
        Assert.Same(error, FailureError(result));
    }

    private static T Unwrap<T>(Result<T> result) =>
        result.Match(value => value, error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static Error FailureError<T>(Result<T> result) =>
        result.Match(_ => throw new Xunit.Sdk.XunitException("expected Failure but was Success"), error => error);
}

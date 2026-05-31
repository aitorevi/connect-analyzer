using ConnectAnalytics.Domain;
using Xunit;

namespace ConnectAnalytics.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Match_OnSuccess_RunsSuccessBranchWithValue()
    {
        var result = Result<int>.Success(42);

        var branch = result.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: error => $"err:{error.Message}");

        Assert.Equal("ok:42", branch);
    }

    [Fact]
    public void Match_OnFailure_RunsFailureBranchWithError()
    {
        var result = Result<int>.Failure(Error.NotFound("missing"));

        var branch = result.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: error => $"err:{error.Type}:{error.Message}");

        Assert.Equal("err:NotFound:missing", branch);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValueAndRewrapsInSuccess()
    {
        var result = Result<int>.Success(21);

        var mapped = result.Map(value => value * 2);

        Assert.Equal(42, Unwrap(mapped));
    }

    [Fact]
    public void Map_OnFailure_DoesNotRunTransformAndPropagatesErrorUnchanged()
    {
        var error = Error.Unavailable("source down");
        var result = Result<int>.Failure(error);

        // The transform would throw if executed; on a Failure it must never run.
        var mapped = result.Map<int>(_ => throw new InvalidOperationException("must not run"));

        Assert.Same(error, UnwrapError(mapped));
    }

    [Fact]
    public void Bind_OnSuccess_FlattensWhenInnerSucceeds()
    {
        var result = Result<int>.Success(21);

        // Func<int, Result<string>> -> Bind keeps it flat (Result<string>, not Result<Result<string>>).
        var bound = result.Bind(value => Result<string>.Success($"v{value}"));

        Assert.Equal("v21", Unwrap(bound));
    }

    [Fact]
    public void Bind_OnSuccess_FlattensWhenInnerFails()
    {
        var error = Error.Validation("bad");
        var result = Result<int>.Success(21);

        var bound = result.Bind(_ => Result<string>.Failure(error));

        Assert.Same(error, UnwrapError(bound));
    }

    [Fact]
    public void Bind_OnFailure_DoesNotRunFunctionAndPropagatesErrorUnchanged()
    {
        var error = Error.Unavailable("source down");
        var result = Result<int>.Failure(error);

        var bound = result.Bind<string>(_ => throw new InvalidOperationException("must not run"));

        Assert.Same(error, UnwrapError(bound));
    }

    // Test-only helpers: unwrap via Match (the type intentionally has no public Value).
    private static T Unwrap<T>(Result<T> result) =>
        result.Match(value => value, error => throw new Xunit.Sdk.XunitException($"expected Success but was Failure: {error.Message}"));

    private static Error UnwrapError<T>(Result<T> result) =>
        result.Match(value => throw new Xunit.Sdk.XunitException($"expected Failure but was Success: {value}"), error => error);
}

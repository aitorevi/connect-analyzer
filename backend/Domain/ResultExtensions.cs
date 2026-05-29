namespace SapAnalytics.Domain;

public static class ResultExtensions
{
    // Async sibling of Result.Bind: chains an asynchronous step that itself returns a Result,
    // short-circuiting on the first Failure. Lets use cases compose await-able steps on the
    // railway without unwrapping (e.g. read-from-source .BindAsync save-to-store).
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask, Func<T, Task<Result<TOut>>> bind)
    {
        var result = await resultTask;
        return await result.Match(
            onSuccess: bind,
            onFailure: error => Task.FromResult(Result<TOut>.Failure(error)));
    }
}

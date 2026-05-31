namespace ConnectAnalytics.Domain;

public static class ResultExtensions
{
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask, Func<T, Task<Result<TOut>>> bind)
    {
        var result = await resultTask;
        return await result.Match(
            onSuccess: bind,
            onFailure: error => Task.FromResult(Result<TOut>.Failure(error)));
    }
}

namespace ConnectAnalytics.Domain;

public sealed class Result<T>
{
    private readonly T _value;
    private readonly Error _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = null!;
        _isSuccess = true;
    }

    private Result(Error error)
    {
        _value = default!;
        _error = error;
        _isSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        _isSuccess ? onSuccess(_value) : onFailure(_error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        _isSuccess ? Result<TOut>.Success(map(_value)) : Result<TOut>.Failure(_error);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        _isSuccess ? bind(_value) : Result<TOut>.Failure(_error);
}

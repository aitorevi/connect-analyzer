namespace ConnectAnalyzer.Domain;

public enum ErrorType
{
    NotFound,
    Validation,
    Unauthorized,
    Unavailable,
    Unexpected,
}

public sealed record Error(ErrorType Type, string Message)
{
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);
    public static Error Unavailable(string message) => new(ErrorType.Unavailable, message);
    public static Error Unexpected(string message) => new(ErrorType.Unexpected, message);
}

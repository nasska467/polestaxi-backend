namespace PolesTaxi.Shared.Errors;

public static class ErrorCodes
{
    public const string Validation = "VALIDATION";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Internal = "INTERNAL";
}

public sealed class AppException : Exception
{
    public string Code { get; }
    public int HttpStatus { get; }

    public AppException(string code, string message, int httpStatus) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
    }

    public static AppException Validation(string message) => new(ErrorCodes.Validation, message, 400);
    public static AppException Unauthorized(string message) => new(ErrorCodes.Unauthorized, message, 401);
    public static AppException Forbidden(string message) => new(ErrorCodes.Forbidden, message, 403);
    public static AppException NotFound(string message) => new(ErrorCodes.NotFound, message, 404);
    public static AppException Conflict(string message) => new(ErrorCodes.Conflict, message, 409);
    public static AppException Internal(string message) => new(ErrorCodes.Internal, message, 500);
}

public sealed record ErrorBody(string Code, string Message);
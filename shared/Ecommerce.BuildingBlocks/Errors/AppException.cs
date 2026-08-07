using Microsoft.AspNetCore.Http;

namespace Ecommerce.BuildingBlocks.Errors;

/**
 * Exception nghiệp vụ mang sẵn mã lỗi + HTTP status để ExceptionHandlingMiddleware map thẳng ra ApiResponse lỗi.
 * Ném exception này ở tầng service/domain; không ai phải tự build response lỗi.
 */
public class AppException(string errorCode, int statusCode, string message, object? details = null) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
    public int StatusCode { get; } = statusCode;
    public object? Details { get; } = details;

    public static AppException NotFound(string errorCode, string message, object? details = null) =>
        new(errorCode, StatusCodes.Status404NotFound, message, details);

    public static AppException Conflict(string errorCode, string message, object? details = null) =>
        new(errorCode, StatusCodes.Status409Conflict, message, details);

    public static AppException BadRequest(string errorCode, string message, object? details = null) =>
        new(errorCode, StatusCodes.Status400BadRequest, message, details);

    public static AppException Validation(string message, object? details = null) =>
        new(ErrorCodes.ValidationError, StatusCodes.Status400BadRequest, message, details);

    public static AppException Unauthorized(string errorCode, string message, object? details = null) =>
       new(errorCode, StatusCodes.Status401Unauthorized, message, details);
}

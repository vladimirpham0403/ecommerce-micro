using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.BuildingBlocks.Http;

/// <summary>
/// Metadata gắn vào mọi response để trace được request.
/// </summary>
/// <param name="RequestId">ID riêng của một request HTTP này.</param>
/// <param name="CorrelationId">ID xuyên suốt một luồng nghiệp vụ (có thể đi qua nhiều service).</param>
public sealed record ApiMeta(string RequestId, string CorrelationId)
{
    // Dựng meta từ correlation-id và request-id đã được CorrelationIdMiddleware gắn vào HttpContext.
    public static ApiMeta From(HttpContext context) => new(context.GetRequestId(), context.GetCorrelationId());
}

/// <summary>
/// Envelope chuẩn cho mọi API response.
/// Success: có <see cref="Data"/>, không có <see cref="Error"/>.
/// Error: có <see cref="Error"/>, không có <see cref="Data"/>.
/// </summary>
public sealed record ApiResponse<T>
{
    public required bool Success { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; init; }

    public required ApiMeta Meta { get; init; }
}

/// <summary>
/// Factory tạo <see cref="ApiResponse{T}"/> — gọi ApiResponse.Ok(...) / ApiResponse.Fail(...).
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, ApiMeta meta) =>
        new()
        {
            Success = true,
            Data = data,
            Meta = meta
        };

    public static ApiResponse<object> Fail(ApiError error, ApiMeta meta) =>
        new()
        {
            Success = false,
            Error = error,
            Meta = meta
        };
}
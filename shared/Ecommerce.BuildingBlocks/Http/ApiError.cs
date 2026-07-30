using System.Text.Json.Serialization;

namespace Ecommerce.BuildingBlocks.Http;

/// <summary>
/// Phần "error" trong response lỗi.
/// </summary>
/// <param name="Code">Mã lỗi UPPER_SNAKE_CASE, có tiền tố domain (vd PRODUCT_NOT_FOUND).</param>
/// <param name="Message">Thông điệp tiếng Anh, ngắn, an toàn để hiển thị cho client.</param>
/// <param name="Details">Tùy chọn: chi tiết lỗi từng field (validation...). Bỏ qua khi null.</param>
public sealed record ApiError(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? Details = null);

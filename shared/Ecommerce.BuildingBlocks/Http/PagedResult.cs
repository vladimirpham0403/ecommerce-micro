namespace Ecommerce.BuildingBlocks.Http;

/**
 * Kết quả phân trang chuẩn dùng chung cho mọi service.
 */
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}

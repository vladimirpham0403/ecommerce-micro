namespace Ecommerce.Product.Common;

/**
 * Policy phân quyền của Product.
 *
 * Chuỗi role và scope là HỢP ĐỒNG với Auth service - chúng đi qua JWT chứ không qua
 * project reference, nên đổi ở Auth mà quên đổi ở đây thì build vẫn xanh còn runtime trả 403.
 * Giá trị phải khớp Ecommerce.Auth.Common.ScopeNames và Ecommerce.Auth.Domain.Contracts.RoleNames.
 */
public static class ProductPolicies
{
    /// <summary>Tạo/sửa/xóa catalog: phải là Admin VÀ token phải mang scope product.write.</summary>
    public const string Write = "product:write";

    public const string AdminRole = "Admin";
    public const string WriteScope = "product.write";

    /// <summary>Audience của service này - Auth phải phát token có "aud" đúng chuỗi này.</summary>
    public const string Audience = "product-api";
}

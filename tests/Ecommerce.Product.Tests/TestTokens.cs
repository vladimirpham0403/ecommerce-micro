using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Product.Tests;

/**
 * Tự ký JWT bằng một khóa đối xứng chỉ tồn tại trong test.
 *
 * Vì sao không gọi Auth service thật: test của Product phải hermetic - không cần container Auth,
 * không cần mạng, chạy song song được. Cái đang kiểm ở đây là *phân quyền* của Product
 * (role + scope + audience), không phải cơ chế cấp token của Auth. Luồng OIDC thật được
 * kiểm riêng trong Ecommerce.Auth.Tests.
 */
public static class TestTokens
{
    public const string Issuer = "https://auth.test";
    public const string Audience = "product-api";

    private const string AdminRole = "Admin";
    private const string CustomerRole = "Customer";
    private const string ReadScope = "product.read";
    private const string WriteScope = "product.write";

    // 32 byte: đủ dài cho HMAC-SHA256.
    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("product-tests-signing-key-0123456789"));

    /// <summary>Admin đủ quyền ghi - dùng làm token mặc định cho các test CRUD sẵn có.</summary>
    public static string Admin() => Create([AdminRole], [ReadScope, WriteScope]);

    /// <summary>Đúng scope nhưng sai role.</summary>
    public static string Customer() => Create([CustomerRole], [ReadScope, WriteScope]);

    /// <summary>Đúng role nhưng token không xin scope ghi.</summary>
    public static string AdminWithoutWriteScope() => Create([AdminRole], [ReadScope]);

    public static string Expired() => Create([AdminRole], [ReadScope, WriteScope], TimeSpan.FromMinutes(-5));

    public static string Create(string[] roles, string[] scopes, TimeSpan? lifetime = null)
    {
        // Cửa sổ hiệu lực phải mạch lạc: nbf < exp. Nếu chỉ lùi exp về quá khứ mà giữ nbf ở hiện tại
        // thì token có nbf SAU exp, và handler báo lifetime không hợp lệ chứ không báo hết hạn.
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15));
        var issuedAt = expires - TimeSpan.FromMinutes(15);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expires,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = Guid.CreateVersion7().ToString(),
                ["name"] = "Test User",
                ["jti"] = Guid.CreateVersion7().ToString(),
                ["role"] = roles,
                // OIDC gom mọi scope vào một claim, ngăn cách bằng dấu cách (RFC 9068).
                ["scope"] = string.Join(' ', scopes)
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

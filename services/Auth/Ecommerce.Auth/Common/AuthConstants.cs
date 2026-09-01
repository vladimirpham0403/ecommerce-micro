namespace Ecommerce.Auth.Common;

public static class ScopeNames
{
    public const string ProductRead = "product.read";
    public const string ProductWrite = "product.write";
    public const string UsersManage = "users.manage";
    public static readonly IReadOnlyList<string> All = [ProductRead, ProductWrite, UsersManage];
}

public static class Audiences
{
    public const string ProductApi = "product-api";
    public const string AuthApi = "auth-api";
}

public static class ClientIds
{
    public const string WebClient = "web-client";
    public const string ServiceWorker = "service-worker";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}

public static class AuthPolicies
{
    public const string ManageUsers = "users:manage";
}

namespace Ecommerce.Auth.Domain.Contracts;

public sealed class RoleNames
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string Manager = "Manager";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Customer,
        Admin,
        Manager
    };
}

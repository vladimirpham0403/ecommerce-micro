namespace Ecommerce.Auth.Dtos;

public record UserResponse(
    Guid Id,
    string? Email,
    string? DisplayName,
    IEnumerable<string> Roles
);

public record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName
);

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UserListQuery(string? Search = null, int Page = 1, int PageSize = 20);

public record SetUserRolesRequest(IReadOnlyList<string> Roles);

public record SessionResponse(
    string Id,
    string? ClientId,
    IReadOnlyList<string> Scopes,
    DateTime? CreatedAt,
    bool IsCurrent
);

using Ecommerce.Auth.Domain;

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

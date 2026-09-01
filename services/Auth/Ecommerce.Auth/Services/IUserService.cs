using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Http;

namespace Ecommerce.Auth.Services;

public interface IUserService
{
    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    public Task<AppUser> ValidateCredentialsAsync(string email, string password, CancellationToken ct);
    public Task<AppUser?> FindActiveByIdAsync(Guid id, CancellationToken ct);
    public Task<PagedResult<UserSummaryResponse>> ListAsync(UserListQuery query, CancellationToken ct);
    public Task<UserSummaryResponse> SetRolesAsync(Guid userId, IReadOnlyList<string> roles, Guid actingUserId, CancellationToken ct);
}

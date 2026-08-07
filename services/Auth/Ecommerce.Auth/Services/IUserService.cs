using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Dtos;
using RegisterRequest = Ecommerce.Auth.Dtos.RegisterRequest;

namespace Ecommerce.Auth.Services;

public interface IUserService
{
    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    public Task<AppUser> ValidateCredentialsAsync(string email, string password, CancellationToken ct);
    public Task<AppUser?> FindActiveByIdAsync(Guid id, CancellationToken ct);
}

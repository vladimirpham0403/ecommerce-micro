using Asp.Versioning;
using Ecommerce.Auth.Common;
using Ecommerce.Auth.Dtos;
using Ecommerce.Auth.Services;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ecommerce.Auth.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
public class AccountController(IUserService users) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await users.RegisterAsync(request, ct);
        return Created($"/v1/auth/users/{result.Id}", ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var id = Guid.TryParse(User.GetClaim(Claims.Subject), out var parsed) ? parsed : Guid.Empty;

        var response = new UserResponse(
            id,
            User.GetClaim(Claims.Email),
            User.GetClaim(Claims.Name),
            User.GetClaims(Claims.Role));

        return Ok(ApiResponse.Ok(response, ApiMeta.From(HttpContext)));
    }
}

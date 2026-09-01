using Asp.Versioning;
using Ecommerce.Auth.Common;
using Ecommerce.Auth.Dtos;
using Ecommerce.Auth.Services;
using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ecommerce.Auth.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/users")]
[Authorize(Policy = AuthPolicies.ManageUsers)]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserListQuery query, CancellationToken ct)
    {
        var result = await users.ListAsync(query, ct);
        return Ok(ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, SetUserRolesRequest request, CancellationToken ct)
    {
        var result = await users.SetRolesAsync(id, request.Roles, CurrentUserId(), ct);
        return Ok(ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    private Guid CurrentUserId()
    {
        if (!Guid.TryParse(User.GetClaim(Claims.Subject), out var id))
        {
            throw new AppException(
                ErrorCodes.SystemInternalError,
                StatusCodes.Status500InternalServerError,
                "Token does not carry a usable subject claim.");
        }

        return id;
    }
}

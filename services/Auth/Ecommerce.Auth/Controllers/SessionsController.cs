using Asp.Versioning;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ecommerce.Auth.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/sessions")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SessionsController(IOpenIddictAuthorizationManager authorizations, IOpenIddictApplicationManager applications, IOpenIddictTokenManager tokens) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var subject = CurrentSubject();
        var currentAuthorizationId = User.GetAuthorizationId();
        var sessions = new List<SessionResponse>();

        await foreach (var authorization in authorizations.FindBySubjectAsync(subject, ct))
        {
            if (!await authorizations.HasStatusAsync(authorization, Statuses.Valid, ct) || !await authorizations.HasTypeAsync(authorization, AuthorizationTypes.Permanent, ct))
            {
                continue;
            }

            var id = await authorizations.GetIdAsync(authorization, ct);
            if (id is null)
            {
                continue;
            }

            sessions.Add(new SessionResponse(
                id,
                await ResolveClientIdAsync(authorization, ct),
                [.. await authorizations.GetScopesAsync(authorization, ct)],
                (await authorizations.GetCreationDateAsync(authorization, ct))?.UtcDateTime,
                string.Equals(id, currentAuthorizationId, StringComparison.Ordinal)));
        }

        return Ok(ApiResponse.Ok<IReadOnlyList<SessionResponse>>(sessions, ApiMeta.From(HttpContext)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(string id, CancellationToken ct)
    {
        var subject = CurrentSubject();
        var authorization = await authorizations.FindByIdAsync(id, ct);

        if (authorization is null || !string.Equals(await authorizations.GetSubjectAsync(authorization, ct), subject, StringComparison.Ordinal))
        {
            throw AppException.NotFound(ErrorCodes.AuthSessionNotFound, "Session not found.");
        }

        await tokens.RevokeByAuthorizationIdAsync(id, ct);
        await authorizations.TryRevokeAsync(authorization, ct);

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAll(CancellationToken ct)
    {
        var subject = CurrentSubject();

        await tokens.RevokeBySubjectAsync(subject, ct);
        await authorizations.RevokeBySubjectAsync(subject, ct);

        return NoContent();
    }

    private async Task<string?> ResolveClientIdAsync(object authorization, CancellationToken ct)
    {
        if (await authorizations.GetApplicationIdAsync(authorization, ct) is not { } applicationId)
        {
            return null;
        }

        return await applications.FindByIdAsync(applicationId, ct) is { } application
            ? await applications.GetClientIdAsync(application, ct)
            : null;
    }

    private string CurrentSubject()
    {
        return User.GetClaim(Claims.Subject)
               ?? throw new AppException(ErrorCodes.SystemInternalError, StatusCodes.Status500InternalServerError, "Token does not carry a subject claim.");
    }
}

using System.Collections.Immutable;
using System.Security.Claims;
using Ecommerce.Auth.Common;
using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Services;
using Ecommerce.BuildingBlocks.Errors;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ecommerce.Auth.Controllers;

public class AuthorizationController(
    IUserService users,
    IOpenIddictScopeManager scopes,
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations) : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Không đọc được OpenID Connect request.");
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.HasFormContentType ? Request.Form : Request.Query)
                });
        }

        var subject = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out var userId) || await users.FindActiveByIdAsync(userId, ct) is not { } user)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Challenge(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var identity = await BuildIdentityAsync(user, request.GetScopes(), request.ClientId, null, ct);
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Exchange(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Không đọc được OpenID Connect request.");

        if (request.IsPasswordGrantType())
        {
            AppUser user;

            try
            {
                user = await users.ValidateCredentialsAsync(request.Username!, request.Password!, ct);
            }
            catch (AppException ex)
            {
                return InvalidGrant(ex.Message);
            }

            var identity = await BuildIdentityAsync(user, request.GetScopes(), request.ClientId, null, ct);
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var subject = result.Principal?.GetClaim(Claims.Subject);

            if (!Guid.TryParse(subject, out var userId) ||
                await users.FindActiveByIdAsync(userId, ct) is not { } user)
            {
                return InvalidGrant("The account no longer exists or has been disabled.");
            }

            var authorizationId = result.Principal!.GetAuthorizationId();

            var identity = await BuildIdentityAsync(
                user, result.Principal!.GetScopes(), request.ClientId, authorizationId, ct);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, request.ClientId)
                .SetClaim(Claims.Name, request.ClientId);

            identity.SetScopes(request.GetScopes());
            identity.SetResources(await scopes.ListResourcesAsync(identity.GetScopes()).ToListAsync(ct));
            identity.SetDestinations(ClaimDestinations.GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotSupportedException($"Grant type '{request.GrantType}' không được hỗ trợ.");
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EndSession()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    public IActionResult UserInfo()
    {
        var claims = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [Claims.Subject] = User.GetClaim(Claims.Subject),
            [Claims.Email] = User.GetClaim(Claims.Email),
            [Claims.Name] = User.GetClaim(Claims.Name),
            [Claims.Role] = User.GetClaims(Claims.Role)
        };

        return Ok(claims);
    }

    private async Task<ClaimsIdentity> BuildIdentityAsync(
        AppUser user,
        ImmutableArray<string> requestedScopes,
        string? clientId,
        string? authorizationId,
        CancellationToken ct)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Email, user.Email)
            .SetClaim(Claims.Name, user.DisplayName ?? user.Email)
            .SetClaims(Claims.Role, [.. user.UserRoles.Select(ur => ur.Role.Name)]);

        identity.SetScopes(requestedScopes);

        identity.SetResources(await scopes.ListResourcesAsync(identity.GetScopes(), ct).ToListAsync(ct));

        authorizationId ??= await ResolveAuthorizationIdAsync(user, clientId, identity.GetScopes(), ct);
        if (!string.IsNullOrEmpty(authorizationId))
        {
            identity.SetAuthorizationId(authorizationId);
        }

        identity.SetDestinations(ClaimDestinations.GetDestinations);

        return identity;
    }

    private async Task<string?> ResolveAuthorizationIdAsync(AppUser user, string? clientId, ImmutableArray<string> grantedScopes, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        if (await applications.FindByClientIdAsync(clientId, ct) is not { } application ||
            await applications.GetIdAsync(application, ct) is not { } applicationId)
        {
            return null;
        }

        var subject = user.Id.ToString();

        var existing = await authorizations.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: grantedScopes,
            cancellationToken: ct).FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return await authorizations.GetIdAsync(existing, ct);
        }

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            Subject = subject,
            Type = AuthorizationTypes.Permanent,
            Status = Statuses.Valid,

            CreationDate = DateTimeOffset.UtcNow
        };

        descriptor.Scopes.UnionWith(grantedScopes);

        var created = await authorizations.CreateAsync(descriptor, ct);
        return await authorizations.GetIdAsync(created, ct);
    }

    private ForbidResult InvalidGrant(string description) => Forbid(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }));
}

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ecommerce.Auth.Common;
using Ecommerce.Auth.Services;
using Ecommerce.BuildingBlocks.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Auth.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Login)]
public class LoginModel(IUserService users) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Domain.AppUser user;

        try
        {
            user = await users.ValidateCredentialsAsync(Input.Email, Input.Password, ct);
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(ReturnUrl ?? "/");
    }
}

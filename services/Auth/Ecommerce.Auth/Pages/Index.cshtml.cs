using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.Auth.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    public string? DisplayName { get; private set; }

    public void OnGet() => DisplayName = User.Identity?.IsAuthenticated == true
        ? User.Identity.Name
        : null;
}

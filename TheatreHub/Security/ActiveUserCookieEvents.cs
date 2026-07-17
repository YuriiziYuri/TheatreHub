using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using TheatreHub.Models;

namespace TheatreHub.Security;

public class ActiveUserCookieEvents : CookieAuthenticationEvents
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ActiveUserCookieEvents(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        if (context.Principal == null)
        {
            context.RejectPrincipal();
            return;
        }

        var user = await _userManager.GetUserAsync(context.Principal);

        if (user == null || !user.IsActive)
        {
            context.RejectPrincipal();

            await _signInManager.SignOutAsync();
        }
    }
}
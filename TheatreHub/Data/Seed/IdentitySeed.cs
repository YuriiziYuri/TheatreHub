using Microsoft.AspNetCore.Identity;
using TheatreHub.Models;
using TheatreHub.Constants;

namespace TheatreHub.Data.Seed;

public static class IdentitySeed
{
    public static readonly string[] Roles =
[
    UserRoles.SystemAdmin,
    UserRoles.GeneralDirector,
    UserRoles.StageDirector,
    UserRoles.AssistantDirector,
    UserRoles.Playwright,
    UserRoles.Actor,
    UserRoles.ProductionStaff,
    UserRoles.FinanceManager
];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@theatrehub.local";
        const string adminPassword = "Admin123";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Системний адміністратор",
                JobTitle = "Адміністратор системи",
                IsActive = true
            };

            var result = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    admin,
                    UserRoles.SystemAdmin);
            }
        }
    }
}
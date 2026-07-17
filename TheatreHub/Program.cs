using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Data.Seed;
using Microsoft.AspNetCore.Identity;
using TheatreHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using TheatreHub.Constants;
using TheatreHub.Security;
using TheatreHub.Services.Notifications;
using TheatreHub.Services.ActionLogs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ActiveUserCookieEvents>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.EventsType = typeof(ActiveUserCookieEvents);
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.CanManageUsers, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin));

    options.AddPolicy(AppPolicies.CanManagePerformances, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.StageDirector));

    options.AddPolicy(AppPolicies.CanManageRehearsals, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.StageDirector,
            UserRoles.AssistantDirector));

    options.AddPolicy(AppPolicies.CanManageTasks, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.StageDirector,
            UserRoles.AssistantDirector,
            UserRoles.ProductionStaff,
            UserRoles.Playwright));

    options.AddPolicy(AppPolicies.CanManageProduction, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.StageDirector,
            UserRoles.ProductionStaff));

    options.AddPolicy(AppPolicies.CanManageVenues, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.StageDirector));

    options.AddPolicy(AppPolicies.CanViewFinance, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.FinanceManager));

    options.AddPolicy(AppPolicies.CanManageFinance, policy =>
        policy.RequireRole(
            UserRoles.SystemAdmin,
            UserRoles.GeneralDirector,
            UserRoles.FinanceManager));

    options.AddPolicy(AppPolicies.CanViewDashboard, policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy(AppPolicies.CanViewCalendar, policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<IUserActionLogService, UserActionLogService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeed.SeedAsync(scope.ServiceProvider);
}

app.Run();

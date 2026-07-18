using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Constants;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.ViewModels.AdminUsers;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize(Policy = AppPolicies.CanManageUsers)]
public class AdminUsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _actionLogService = actionLogService;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string? role,
        bool? isActive)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .Include(user => user.Actor)
            .OrderBy(user => user.FullName)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch =
                searchTerm.Trim().ToLower();

            users = users
                .Where(user =>
                    user.FullName.ToLower().Contains(normalizedSearch) ||
                    (user.Email != null &&
                     user.Email.ToLower().Contains(normalizedSearch)) ||
                    (user.JobTitle != null &&
                     user.JobTitle.ToLower().Contains(normalizedSearch)))
                .ToList();
        }

        if (isActive.HasValue)
        {
            users = users
                .Where(user =>
                    user.IsActive == isActive.Value)
                .ToList();
        }

        var model = new AdminUserIndexViewModel
        {
            SearchTerm = searchTerm,
            Role = role,
            IsActive = isActive,
            AvailableRoles = await GetRoleNamesAsync()
        };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(role) &&
                !roles.Contains(role))
            {
                continue;
            }

            model.Users.Add(
                new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    JobTitle = user.JobTitle,
                    IsActive = user.IsActive,
                    ActorId = user.ActorId,
                    ActorName = user.Actor?.FullName,
                    Roles = roles.ToList()
                });
        }

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = new AdminUserCreateViewModel();

        await PopulateCreateFormAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminUserCreateViewModel model)
    {
        NormalizeCreateModel(model);

        await ValidateSelectedRolesAsync(model.SelectedRoles);

        await ValidateActorAsync(model.ActorId);

        if (!ModelState.IsValid)
        {
            await PopulateCreateFormAsync(model);

            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            JobTitle = model.JobTitle,
            IsActive = model.IsActive,
            ActorId = model.ActorId
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                model.Password);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);

            await PopulateCreateFormAsync(model);

            return View(model);
        }

        if (model.SelectedRoles.Count > 0)
        {
            var roleResult =
                await _userManager.AddToRolesAsync(
                    user,
                    model.SelectedRoles);

            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);

                await PopulateCreateFormAsync(model);

                return View(model);
            }
        }

        await _actionLogService.LogAsync(
            User,
            "Create",
            "User",
            null,
            user.FullName,
            $"Створено користувача «{user.FullName}» з email {user.Email}.");

        TempData["SuccessMessage"] =
            $"Користувача «{user.FullName}» створено.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.Users
            .Include(item => item.Actor)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var model = new AdminUserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            JobTitle = user.JobTitle,
            IsActive = user.IsActive,
            ActorId = user.ActorId,
            SelectedRoles = roles.ToList()
        };

        await PopulateEditFormAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        AdminUserEditViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        NormalizeEditModel(model);

        await ValidateSelectedRolesAsync(model.SelectedRoles);

        await ValidateActorAsync(model.ActorId);

        if (!ModelState.IsValid)
        {
            await PopulateEditFormAsync(model);

            return View(model);
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var oldFullName = user.FullName;
        var oldEmail = user.Email;
        var oldIsActive = user.IsActive;
        var oldActorId = user.ActorId;
        var oldRoles = await _userManager.GetRolesAsync(user);

        user.FullName = model.FullName;
        user.JobTitle = model.JobTitle;
        user.IsActive = model.IsActive;
        user.ActorId = model.ActorId;

        if (user.Email != model.Email)
        {
            var setEmailResult =
                await _userManager.SetEmailAsync(
                    user,
                    model.Email);

            if (!setEmailResult.Succeeded)
            {
                AddIdentityErrors(setEmailResult);

                await PopulateEditFormAsync(model);

                return View(model);
            }

            var setUserNameResult =
                await _userManager.SetUserNameAsync(
                    user,
                    model.Email);

            if (!setUserNameResult.Succeeded)
            {
                AddIdentityErrors(setUserNameResult);

                await PopulateEditFormAsync(model);

                return View(model);
            }

            user.EmailConfirmed = true;
        }

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);

            await PopulateEditFormAsync(model);

            return View(model);
        }

        var currentRoles =
            oldRoles;

        var currentUserId =
            _userManager.GetUserId(User);

        if (user.Id == currentUserId &&
            currentRoles.Contains(UserRoles.SystemAdmin) &&
            !model.SelectedRoles.Contains(UserRoles.SystemAdmin))
        {
            ModelState.AddModelError(
                nameof(model.SelectedRoles),
                "Не можна забрати роль SystemAdmin у власного облікового запису.");

            await PopulateEditFormAsync(model);

            return View(model);
        }

        var rolesToRemove =
            currentRoles.Except(model.SelectedRoles).ToList();

        var rolesToAdd =
            model.SelectedRoles.Except(currentRoles).ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);

                await PopulateEditFormAsync(model);

                return View(model);
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult =
                await _userManager.AddToRolesAsync(
                    user,
                    rolesToAdd);

            if (!addResult.Succeeded)
            {
                AddIdentityErrors(addResult);

                await PopulateEditFormAsync(model);

                return View(model);
            }
        }

        var changes = new List<string>();

        if (oldFullName != user.FullName)
        {
            changes.Add($"ПІБ: «{oldFullName}» → «{user.FullName}»");
        }

        if (oldEmail != user.Email)
        {
            changes.Add($"email: «{oldEmail}» → «{user.Email}»");
        }

        if (oldIsActive != user.IsActive)
        {
            changes.Add(user.IsActive ? "користувача активовано" : "користувача деактивовано");
        }

        if (oldActorId != user.ActorId)
        {
            changes.Add("змінено прив’язку до актора");
        }

        if (rolesToAdd.Count > 0)
        {
            changes.Add("додано ролі: " + string.Join(", ", rolesToAdd));
        }

        if (rolesToRemove.Count > 0)
        {
            changes.Add("видалено ролі: " + string.Join(", ", rolesToRemove));
        }

        var description = changes.Count > 0
            ? $"Оновлено користувача «{user.FullName}»: {string.Join("; ", changes)}."
            : $"Оновлено користувача «{user.FullName}» без зміни основних полів.";

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "User",
            null,
            user.FullName,
            description);

        TempData["SuccessMessage"] =
            $"Користувача «{user.FullName}» оновлено.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChangePassword(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user =
            await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var model = new AdminUserChangePasswordViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        string id,
        AdminUserChangePasswordViewModel model)
    {
        if (id != model.UserId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var token =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var result =
            await _userManager.ResetPasswordAsync(
                user,
                token,
                model.NewPassword);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);

            return View(model);
        }

        await _actionLogService.LogAsync(
            User,
            "ChangePassword",
            "User",
            null,
            user.FullName,
            $"Змінено пароль користувача «{user.FullName}».");

        TempData["SuccessMessage"] =
            $"Пароль користувача «{user.FullName}» змінено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user =
            await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUserId =
            _userManager.GetUserId(User);

        if (user.Id == currentUserId && user.IsActive)
        {
            TempData["ErrorMessage"] =
                "Не можна деактивувати власний обліковий запис.";

            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);

            TempData["ErrorMessage"] =
                "Не вдалося змінити статус користувача.";

            return RedirectToAction(nameof(Index));
        }

        await _actionLogService.LogAsync(
            User,
            "ChangeStatus",
            "User",
            null,
            user.FullName,
            user.IsActive
                ? $"Користувача «{user.FullName}» активовано."
                : $"Користувача «{user.FullName}» деактивовано.");

        TempData["SuccessMessage"] =
            user.IsActive
                ? $"Користувача «{user.FullName}» активовано."
                : $"Користувача «{user.FullName}» деактивовано.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] =
                "Не можна видалити власний обліковий запис.";

            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(UserRoles.SystemAdmin))
        {
            var systemAdmins =
                await _userManager.GetUsersInRoleAsync(
                    UserRoles.SystemAdmin);

            if (systemAdmins.Count <= 1)
            {
                TempData["ErrorMessage"] =
                    "Не можна видалити останнього системного адміністратора.";

                return RedirectToAction(nameof(Index));
            }
        }

        var userFullName = user.FullName;
        var userEmail = user.Email;

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            TempData["ErrorMessage"] =
                $"Не вдалося видалити користувача. {errors}";

            return RedirectToAction(nameof(Index));
        }

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "User",
            null,
            userFullName,
            $"Видалено користувача «{userFullName}» з email {userEmail}.");

        TempData["SuccessMessage"] =
            $"Користувача «{userFullName}» остаточно видалено.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateFormAsync(
        AdminUserCreateViewModel model)
    {
        model.AvailableRoles =
            await GetRoleNamesAsync();

        model.Actors =
            await GetActorsAsync();
    }

    private async Task PopulateEditFormAsync(
        AdminUserEditViewModel model)
    {
        model.AvailableRoles =
            await GetRoleNamesAsync();

        model.Actors =
            await GetActorsAsync();
    }

    private async Task<List<string>> GetRoleNamesAsync()
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();
    }

    private async Task<List<Actor>> GetActorsAsync()
    {
        return await _context.Actors
            .AsNoTracking()
            .OrderBy(actor => actor.LastName)
            .ThenBy(actor => actor.FirstName)
            .ToListAsync();
    }

    private async Task ValidateSelectedRolesAsync(
        List<string> selectedRoles)
    {
        selectedRoles =
            selectedRoles
                .Where(role =>
                    !string.IsNullOrWhiteSpace(role))
                .Distinct()
                .ToList();

        foreach (var role in selectedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError(
                    nameof(AdminUserCreateViewModel.SelectedRoles),
                    $"Роль «{role}» не існує.");
            }
        }
    }

    private async Task ValidateActorAsync(int? actorId)
    {
        if (!actorId.HasValue)
        {
            return;
        }

        var actorExists =
            await _context.Actors
                .AsNoTracking()
                .AnyAsync(actor =>
                    actor.Id == actorId.Value);

        if (!actorExists)
        {
            ModelState.AddModelError(
                nameof(AdminUserCreateViewModel.ActorId),
                "Обраний актор не існує.");
        }
    }

    private static void NormalizeCreateModel(
        AdminUserCreateViewModel model)
    {
        model.FullName =
            model.FullName?.Trim() ?? string.Empty;

        model.Email =
            model.Email?.Trim() ?? string.Empty;

        model.JobTitle =
            NormalizeOptionalText(model.JobTitle);

        model.SelectedRoles =
            model.SelectedRoles
                .Where(role =>
                    !string.IsNullOrWhiteSpace(role))
                .Distinct()
                .ToList();
    }

    private static void NormalizeEditModel(
        AdminUserEditViewModel model)
    {
        model.FullName =
            model.FullName?.Trim() ?? string.Empty;

        model.Email =
            model.Email?.Trim() ?? string.Empty;

        model.JobTitle =
            NormalizeOptionalText(model.JobTitle);

        model.SelectedRoles =
            model.SelectedRoles
                .Where(role =>
                    !string.IsNullOrWhiteSpace(role))
                .Distinct()
                .ToList();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class ScenesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public ScenesController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    public async Task<IActionResult> Index(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act == null)
        {
            return NotFound();
        }

        var scenes = await _context.Scenes
            .AsNoTracking()
            .Where(scene => scene.ActId == actId)
            .OrderBy(scene => scene.Position)
            .ThenBy(scene => scene.Number)
            .ToListAsync();

        ViewBag.ActId = act.Id;
        ViewBag.ActName = act.DisplayName;
        ViewBag.PerformanceId = act.PerformanceId;
        ViewBag.PerformanceTitle = act.Performance.Title;

        return View(scenes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .Include(item => item.SceneRoles)
                .ThenInclude(sceneRole =>
                    sceneRole.CharacterRole)
                    .ThenInclude(role =>
                        role.Assignments)
                        .ThenInclude(assignment =>
                            assignment.Actor)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Hall)
                    .ThenInclude(hall => hall.Venue)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Participants)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        scene.SceneRoles = scene.SceneRoles
            .OrderBy(sceneRole =>
                sceneRole.CharacterRole.IsMainRole ? 0 : 1)
            .ThenBy(sceneRole =>
                sceneRole.CharacterRole.Name)
            .ToList();

        scene.Rehearsals = scene.Rehearsals
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToList();

        ViewBag.Tasks = await _context.TheatreTasks
    .AsNoTracking()
    .Include(task => task.Performance)
    .Include(task => task.Act)
    .Include(task => task.Scene)
    .Where(task =>
        task.SceneId == scene.Id)
    .OrderBy(task =>
        task.Status == TheatreTaskStatus.Done ||
        task.Status == TheatreTaskStatus.Cancelled)
    .ThenBy(task =>
        task.Deadline ?? DateTime.MaxValue)
    .ThenByDescending(task =>
        task.Priority)
    .ToListAsync();

        ViewBag.ProductionItems = await _context.ProductionItems
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Act)
            .Include(item => item.Scene)
            .Where(item =>
                item.SceneId == scene.Id)
            .OrderBy(item =>
                item.Status == ProductionItemStatus.Ready ||
                item.Status == ProductionItemStatus.Cancelled)
            .ThenBy(item =>
                item.NeededBy ?? DateTime.MaxValue)
            .ThenBy(item =>
                item.Type)
            .ThenBy(item =>
                item.Name)
            .ToListAsync();

        return View(scene);
    }

    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act == null)
        {
            return NotFound();
        }

        var nextNumber = await _context.Scenes
            .Where(scene => scene.ActId == actId)
            .Select(scene => (int?)scene.Number)
            .MaxAsync() ?? 0;

        var nextPosition = await _context.Scenes
            .Where(scene => scene.ActId == actId)
            .Select(scene => (int?)scene.Position)
            .MaxAsync() ?? 0;

        var model = new Scene
        {
            ActId = actId,
            Number = nextNumber + 1,
            Position = nextPosition + 1
        };

        SetActHeader(act);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create(
        [Bind(
            "ActId,Number,Title,Synopsis," +
            "DurationMinutes,Position")]
        Scene scene)
    {
        NormalizeScene(scene);

        await ValidateSceneAsync(scene);

        if (!ModelState.IsValid)
        {
            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        _context.Scenes.Add(scene);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");

            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        var sceneLogTitle =
            await GetSceneLogTitleAsync(scene.Id);

        await _actionLogService.LogAsync(
            User,
            "Create",
            "Scene",
            scene.Id,
            sceneLogTitle,
            $"Створено сцену «{scene.Title}».");

        TempData["SuccessMessage"] =
            $"Сцену «{scene.Title}» успішно створено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = scene.ActId });
    }

    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        SetActHeader(scene.Act);

        return View(scene);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,ActId,Number,Title,Synopsis," +
            "DurationMinutes,Position")]
        Scene scene)
    {
        if (id != scene.Id)
        {
            return NotFound();
        }

        NormalizeScene(scene);

        await ValidateSceneAsync(
            scene,
            excludedSceneId: id);

        if (!ModelState.IsValid)
        {
            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        var existingScene = await _context.Scenes
            .FirstOrDefaultAsync(item => item.Id == id);

        if (existingScene == null)
        {
            return NotFound();
        }

        if (existingScene.ActId != scene.ActId)
        {
            return BadRequest();
        }

        existingScene.Number = scene.Number;
        existingScene.Title = scene.Title;
        existingScene.Synopsis = scene.Synopsis;
        existingScene.DurationMinutes = scene.DurationMinutes;
        existingScene.Position = scene.Position;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");

            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        var sceneLogTitle =
            await GetSceneLogTitleAsync(existingScene.Id);

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "Scene",
            existingScene.Id,
            sceneLogTitle,
            $"Відредаговано сцену «{existingScene.Title}».");

        TempData["SuccessMessage"] =
            $"Сцену «{existingScene.Title}» успішно оновлено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = existingScene.ActId });
    }

    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        return View(scene);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var scene = await _context.Scenes
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (scene == null)
        {
            return NotFound();
        }

        if (scene.Rehearsals.Any())
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити сцену, бо до неї вже прив’язані репетиції.";

            return RedirectToAction(
                nameof(Details),
                new { id = scene.Id });
        }

        var hasTasks =
    await _context.TheatreTasks.AnyAsync(task =>
        task.SceneId == scene.Id);

        var hasProductionItems =
            await _context.ProductionItems.AnyAsync(item =>
                item.SceneId == scene.Id);

        if (hasTasks || hasProductionItems)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити сцену, бо до неї прив’язані завдання або постановочні елементи.";

            return RedirectToAction(
                nameof(Details),
                new { id = scene.Id });
        }

        var actId = scene.ActId;
        var sceneTitle = scene.Title;
        var sceneLogTitle =
            await GetSceneLogTitleAsync(scene.Id);

        _context.Scenes.Remove(scene);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "Scene",
            id,
            sceneLogTitle,
            $"Видалено сцену «{sceneTitle}».");

        TempData["SuccessMessage"] =
            $"Сцену «{sceneTitle}» видалено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = actId });
    }

    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Roles(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .Include(item => item.SceneRoles)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        var roles = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Assignments)
                .ThenInclude(assignment => assignment.Actor)
            .Where(role =>
                role.PerformanceId == scene.Act.PerformanceId)
            .OrderBy(role => role.IsMainRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .ToListAsync();

        var existingSceneRoles = scene.SceneRoles
            .ToDictionary(
                sceneRole => sceneRole.CharacterRoleId);

        var model = new SceneRolesViewModel
        {
            SceneId = scene.Id,

            SceneTitle =
                $"Сцена {scene.Number}. {scene.Title}",

            ActName = scene.Act.DisplayName,

            PerformanceId = scene.Act.PerformanceId,

            PerformanceTitle =
                scene.Act.Performance.Title,

            Roles = roles.Select(role =>
            {
                existingSceneRoles.TryGetValue(
                    role.Id,
                    out var existingSceneRole);

                return new SceneRoleSelectionViewModel
                {
                    CharacterRoleId = role.Id,

                    RoleName = role.Name,

                    IsMainRole = role.IsMainRole,

                    AssignedActorsText =
                        GetAssignedActorsText(role),

                    IsSelected =
                        existingSceneRole != null,

                    IsRequired =
                        existingSceneRole?.IsRequired ?? true,

                    Notes =
                        existingSceneRole?.Notes
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Roles(
        int id,
        SceneRolesViewModel model)
    {
        if (id != model.SceneId)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .Include(item => item.SceneRoles)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (scene == null)
        {
            return NotFound();
        }

        var validRoleIds = await _context.CharacterRoles
            .Where(role =>
                role.PerformanceId == scene.Act.PerformanceId)
            .Select(role => role.Id)
            .ToListAsync();

        var validRoleIdSet = validRoleIds.ToHashSet();

        var submittedRoles = model.Roles
            .Where(role =>
                validRoleIdSet.Contains(role.CharacterRoleId))
            .ToList();

        var selectedRoles = submittedRoles
            .Where(role => role.IsSelected)
            .ToList();

        var selectedRoleIds = selectedRoles
            .Select(role => role.CharacterRoleId)
            .ToHashSet();

        var rolesToRemove = scene.SceneRoles
            .Where(sceneRole =>
                !selectedRoleIds.Contains(
                    sceneRole.CharacterRoleId))
            .ToList();

        _context.SceneRoles.RemoveRange(rolesToRemove);

        foreach (var submittedRole in selectedRoles)
        {
            var existingSceneRole = scene.SceneRoles
                .FirstOrDefault(sceneRole =>
                    sceneRole.CharacterRoleId ==
                    submittedRole.CharacterRoleId);

            if (existingSceneRole == null)
            {
                scene.SceneRoles.Add(
                    new SceneRole
                    {
                        SceneId = scene.Id,
                        CharacterRoleId =
                            submittedRole.CharacterRoleId,
                        IsRequired =
                            submittedRole.IsRequired,
                        Notes =
                            NormalizeOptionalText(
                                submittedRole.Notes)
                    });
            }
            else
            {
                existingSceneRole.IsRequired =
                    submittedRole.IsRequired;

                existingSceneRole.Notes =
                    NormalizeOptionalText(
                        submittedRole.Notes);
            }
        }

        await _context.SaveChangesAsync();

        var sceneLogTitle =
            await GetSceneLogTitleAsync(scene.Id);

        await _actionLogService.LogAsync(
            User,
            "UpdateSceneRoles",
            "Scene",
            scene.Id,
            sceneLogTitle,
            $"Оновлено ролі сцени «{scene.Title}».");

        TempData["SuccessMessage"] =
            "Ролі сцени успішно оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id = scene.Id });
    }

    private async Task<string> GetSceneLogTitleAsync(int sceneId)
    {
        var sceneInfo = await _context.Scenes
            .AsNoTracking()
            .Include(scene => scene.Act)
                .ThenInclude(act => act.Performance)
            .Where(scene => scene.Id == sceneId)
            .Select(scene =>
                scene.Act.Performance.Title +
                " — " +
                scene.Act.DisplayName +
                " — Сцена " +
                scene.Number +
                ". " +
                scene.Title)
            .FirstOrDefaultAsync();

        return sceneInfo ?? "Сцена";
    }

    private async Task ValidateSceneAsync(
        Scene scene,
        int? excludedSceneId = null)
    {
        var actExists = await _context.Acts
            .AnyAsync(act => act.Id == scene.ActId);

        if (!actExists)
        {
            ModelState.AddModelError(
                nameof(scene.ActId),
                "Обрана дія не існує.");

            return;
        }

        if (scene.Number <= 0)
        {
            return;
        }

        var duplicateExists = await _context.Scenes
            .AnyAsync(existing =>
                existing.ActId == scene.ActId &&
                existing.Number == scene.Number &&
                (!excludedSceneId.HasValue ||
                 existing.Id != excludedSceneId.Value));

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");
        }
    }

    private async Task LoadActHeaderAsync(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act != null)
        {
            SetActHeader(act);
        }
    }

    private void SetActHeader(Act act)
    {
        ViewBag.ActName = act.DisplayName;
        ViewBag.PerformanceId = act.PerformanceId;
        ViewBag.PerformanceTitle = act.Performance.Title;
    }

    private static void NormalizeScene(Scene scene)
    {
        scene.Title = scene.Title?.Trim() ?? string.Empty;
        scene.Synopsis = NormalizeOptionalText(scene.Synopsis);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetAssignedActorsText(
        CharacterRole role)
    {
        var assignments = role.Assignments
            .Where(assignment =>
                assignment.IsCurrent &&
                assignment.Status ==
                    RoleAssignmentStatus.Approved)
            .OrderBy(assignment =>
                assignment.CastType)
            .ThenBy(assignment =>
                assignment.Actor.LastName)
            .ThenBy(assignment =>
                assignment.Actor.FirstName)
            .Select(assignment =>
            {
                var castTypeText =
                    assignment.CastType == CastType.Main
                        ? "основний склад"
                        : "запасний склад";

                return
                    $"{assignment.Actor.FullName} — {castTypeText}";
            })
            .ToList();

        return assignments.Any()
            ? string.Join("; ", assignments)
            : "Актор ще не призначений";
    }
}
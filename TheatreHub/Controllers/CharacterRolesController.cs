using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class CharacterRolesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public CharacterRolesController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: CharacterRoles
    // Список усіх персонажів разом із виставами
    // та новими призначеннями акторів.
    public async Task<IActionResult> Index()
    {
        var characterRoles = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Performance)
            .Include(role => role.Assignments)
                .ThenInclude(assignment => assignment.Actor)
            .OrderBy(role => role.Performance.Title)
            .ThenBy(role => role.Name)
            .ToListAsync();

        return View(characterRoles);
    }

    // GET: CharacterRoles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Performance)
            .Include(role => role.Assignments)
                .ThenInclude(assignment => assignment.Actor)
            .FirstOrDefaultAsync(role => role.Id == id.Value);

        if (characterRole == null)
        {
            return NotFound();
        }

        return View(characterRole);
    }

    // GET: CharacterRoles/Create
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create()
    {
        await LoadPerformancesAsync();

        return View(new CharacterRole());
    }

    // POST: CharacterRoles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,IsMainRole,PerformanceId")]
        CharacterRole characterRole)
    {
        await ValidatePerformanceAsync(characterRole.PerformanceId);

        if (!ModelState.IsValid)
        {
            await LoadPerformancesAsync(characterRole.PerformanceId);

            return View(characterRole);
        }

        characterRole.Name = characterRole.Name.Trim();

        characterRole.Description =
            string.IsNullOrWhiteSpace(characterRole.Description)
                ? null
                : characterRole.Description.Trim();

        _context.CharacterRoles.Add(characterRole);
        await _context.SaveChangesAsync();

        var performanceTitle = await _context.Performances
            .Where(performance =>
                performance.Id == characterRole.PerformanceId)
            .Select(performance =>
                performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "CharacterRole",
            characterRole.Id,
            characterRole.Name,
            $"Створено персонажа «{characterRole.Name}» для вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Персонажа «{characterRole.Name}» успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: CharacterRoles/Edit/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole = await _context.CharacterRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Id == id.Value);

        if (characterRole == null)
        {
            return NotFound();
        }

        await LoadPerformancesAsync(characterRole.PerformanceId);

        return View(characterRole);
    }

    // POST: CharacterRoles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,IsMainRole,PerformanceId")]
        CharacterRole characterRole)
    {
        if (id != characterRole.Id)
        {
            return NotFound();
        }

        await ValidatePerformanceAsync(characterRole.PerformanceId);

        if (!ModelState.IsValid)
        {
            await LoadPerformancesAsync(characterRole.PerformanceId);

            return View(characterRole);
        }

        var existingRole = await _context.CharacterRoles
            .FirstOrDefaultAsync(role => role.Id == id);

        if (existingRole == null)
        {
            return NotFound();
        }

        existingRole.Name = characterRole.Name.Trim();

        existingRole.Description =
            string.IsNullOrWhiteSpace(characterRole.Description)
                ? null
                : characterRole.Description.Trim();

        existingRole.IsMainRole = characterRole.IsMainRole;
        existingRole.PerformanceId = characterRole.PerformanceId;

        try
        {
            await _context.SaveChangesAsync();

            var performanceTitle = await _context.Performances
                .Where(performance =>
                    performance.Id == existingRole.PerformanceId)
                .Select(performance =>
                    performance.Title)
                .FirstOrDefaultAsync();

            await _actionLogService.LogAsync(
                User,
                "Edit",
                "CharacterRole",
                existingRole.Id,
                existingRole.Name,
                $"Відредаговано персонажа «{existingRole.Name}» для вистави «{performanceTitle}».");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CharacterRoleExistsAsync(id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["SuccessMessage"] =
            $"Персонажа «{existingRole.Name}» успішно оновлено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: CharacterRoles/Delete/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Performance)
            .Include(role => role.Assignments)
                .ThenInclude(assignment => assignment.Actor)
            .FirstOrDefaultAsync(role => role.Id == id.Value);

        if (characterRole == null)
        {
            return NotFound();
        }

        return View(characterRole);
    }

    // POST: CharacterRoles/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var characterRole = await _context.CharacterRoles
            .Include(role => role.Performance)
            .FirstOrDefaultAsync(role => role.Id == id);

        if (characterRole == null)
        {
            return NotFound();
        }

        var roleName = characterRole.Name;
        var performanceTitle = characterRole.Performance.Title;

        _context.CharacterRoles.Remove(characterRole);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "CharacterRole",
            id,
            roleName,
            $"Видалено персонажа «{roleName}» з вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Персонажа «{roleName}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    // Завантажує список вистав для Create та Edit.
    private async Task LoadPerformancesAsync(
        int? selectedPerformanceId = null)
    {
        var performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance => performance.Title)
            .ToListAsync();

        ViewData["PerformanceId"] = new SelectList(
            performances,
            nameof(Performance.Id),
            nameof(Performance.Title),
            selectedPerformanceId);
    }

    // Перевіряє, чи справді обрана вистава існує.
    private async Task ValidatePerformanceAsync(int performanceId)
    {
        var performanceExists = await _context.Performances
            .AnyAsync(performance => performance.Id == performanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(CharacterRole.PerformanceId),
                "Оберіть чинну виставу.");
        }
    }

    private Task<bool> CharacterRoleExistsAsync(int id)
    {
        return _context.CharacterRoles
            .AnyAsync(role => role.Id == id);
    }
}
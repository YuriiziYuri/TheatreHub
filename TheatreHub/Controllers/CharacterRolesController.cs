using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class CharacterRolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CharacterRolesController(ApplicationDbContext context)
    {
        _context = context;
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
    public async Task<IActionResult> Create()
    {
        await LoadPerformancesAsync();

        return View(new CharacterRole());
    }

    // POST: CharacterRoles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
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

        TempData["SuccessMessage"] =
            $"Персонажа «{characterRole.Name}» успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: CharacterRoles/Edit/5
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
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var characterRole = await _context.CharacterRoles
            .FirstOrDefaultAsync(role => role.Id == id);

        if (characterRole == null)
        {
            return NotFound();
        }

        var roleName = characterRole.Name;

        // Пов’язані RoleAssignment мають видалитися автоматично,
        // якщо в ApplicationDbContext налаштовано DeleteBehavior.Cascade.
        _context.CharacterRoles.Remove(characterRole);
        await _context.SaveChangesAsync();

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
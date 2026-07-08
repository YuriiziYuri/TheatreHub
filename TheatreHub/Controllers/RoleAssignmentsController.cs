using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class RoleAssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoleAssignmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: RoleAssignments
    // Показує всі призначення або призначення конкретної ролі.
    public async Task<IActionResult> Index(int? characterRoleId)
    {
        var query = _context.RoleAssignments
            .AsNoTracking()
            .Include(assignment => assignment.CharacterRole)
                .ThenInclude(role => role.Performance)
            .Include(assignment => assignment.Actor)
            .AsQueryable();

        if (characterRoleId.HasValue)
        {
            query = query.Where(
                assignment =>
                    assignment.CharacterRoleId == characterRoleId.Value);
        }

        var assignments = await query
            .OrderBy(assignment =>
                assignment.CharacterRole.Performance.Title)
            .ThenBy(assignment =>
                assignment.CharacterRole.Name)
            .ThenByDescending(assignment =>
                assignment.IsCurrent)
            .ThenBy(assignment =>
                assignment.CastType)
            .ThenByDescending(assignment =>
                assignment.StartDate)
            .ToListAsync();

        ViewBag.CharacterRoleId = characterRoleId;

        if (characterRoleId.HasValue)
        {
            var selectedRole = await _context.CharacterRoles
                .AsNoTracking()
                .Include(role => role.Performance)
                .FirstOrDefaultAsync(
                    role => role.Id == characterRoleId.Value);

            if (selectedRole != null)
            {
                ViewBag.CharacterRoleName =
                    $"{selectedRole.Performance.Title} — {selectedRole.Name}";
            }
        }

        return View(assignments);
    }

    // GET: RoleAssignments/Create
    public async Task<IActionResult> Create(int? characterRoleId)
    {
        var assignment = new RoleAssignment
        {
            CharacterRoleId = characterRoleId ?? 0,
            StartDate = DateTime.Today,
            IsCurrent = true
        };

        await PopulateSelectListsAsync(
            assignment.CharacterRoleId,
            null);

        return View(assignment);
    }

    // POST: RoleAssignments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "CharacterRoleId,ActorId,CastType,StartDate," +
            "EndDate,IsCurrent,IsPublic,Status,Notes")]
        RoleAssignment assignment)
    {
        await ValidateReferencesAsync(assignment);

        await ValidateAssignmentRulesAsync(assignment);

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(
                assignment.CharacterRoleId,
                assignment.ActorId);

            return View(assignment);
        }

        NormalizeAssignment(assignment);

        _context.RoleAssignments.Add(assignment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Не вдалося створити призначення. " +
                "Можливо, таке призначення вже існує.");

            await PopulateSelectListsAsync(
                assignment.CharacterRoleId,
                assignment.ActorId);

            return View(assignment);
        }

        TempData["SuccessMessage"] =
            "Акторське призначення успішно створено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                characterRoleId = assignment.CharacterRoleId
            });
    }

    // GET: RoleAssignments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var assignment = await _context.RoleAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == id.Value);

        if (assignment == null)
        {
            return NotFound();
        }

        await PopulateSelectListsAsync(
            assignment.CharacterRoleId,
            assignment.ActorId);

        return View(assignment);
    }

    // POST: RoleAssignments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,CharacterRoleId,ActorId,CastType,StartDate," +
            "EndDate,IsCurrent,IsPublic,Status,Notes")]
        RoleAssignment assignment)
    {
        if (id != assignment.Id)
        {
            return NotFound();
        }

        await ValidateReferencesAsync(assignment);

        await ValidateAssignmentRulesAsync(
            assignment,
            excludedAssignmentId: id);

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(
                assignment.CharacterRoleId,
                assignment.ActorId);

            return View(assignment);
        }

        var existingAssignment = await _context.RoleAssignments
            .FirstOrDefaultAsync(
                item => item.Id == id);

        if (existingAssignment == null)
        {
            return NotFound();
        }

        existingAssignment.CharacterRoleId =
            assignment.CharacterRoleId;

        existingAssignment.ActorId =
            assignment.ActorId;

        existingAssignment.CastType =
            assignment.CastType;

        existingAssignment.StartDate =
            assignment.StartDate.Date;

        existingAssignment.EndDate =
            assignment.EndDate?.Date;

        existingAssignment.IsCurrent =
            assignment.IsCurrent;

        existingAssignment.IsPublic =
            assignment.IsPublic;

        existingAssignment.Status =
            assignment.Status;

        existingAssignment.Notes =
            NormalizeOptionalText(assignment.Notes);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _context.RoleAssignments
                .AnyAsync(item => item.Id == id);

            if (!exists)
            {
                return NotFound();
            }

            throw;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Не вдалося зберегти зміни. " +
                "Можливо, таке призначення вже існує.");

            await PopulateSelectListsAsync(
                assignment.CharacterRoleId,
                assignment.ActorId);

            return View(assignment);
        }

        TempData["SuccessMessage"] =
            "Акторське призначення успішно оновлено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                characterRoleId =
                    existingAssignment.CharacterRoleId
            });
    }

    // POST: RoleAssignments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await _context.RoleAssignments
            .FirstOrDefaultAsync(item => item.Id == id);

        if (assignment == null)
        {
            return NotFound();
        }

        var characterRoleId =
            assignment.CharacterRoleId;

        _context.RoleAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Акторське призначення видалено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                characterRoleId
            });
    }

    // Перевіряє, чи існують вибрані роль та актор.
    private async Task ValidateReferencesAsync(
        RoleAssignment assignment)
    {
        if (assignment.CharacterRoleId <= 0)
        {
            ModelState.AddModelError(
                nameof(assignment.CharacterRoleId),
                "Оберіть роль.");
        }
        else
        {
            var roleExists = await _context.CharacterRoles
                .AnyAsync(
                    role =>
                        role.Id == assignment.CharacterRoleId);

            if (!roleExists)
            {
                ModelState.AddModelError(
                    nameof(assignment.CharacterRoleId),
                    "Обрана роль не існує.");
            }
        }

        if (assignment.ActorId <= 0)
        {
            ModelState.AddModelError(
                nameof(assignment.ActorId),
                "Оберіть актора.");
        }
        else
        {
            var actorExists = await _context.Actors
                .AnyAsync(
                    actor =>
                        actor.Id == assignment.ActorId);

            if (!actorExists)
            {
                ModelState.AddModelError(
                    nameof(assignment.ActorId),
                    "Обраний актор не існує.");
            }
        }
    }

    // Перевіряє дати та дублікати призначень.
    private async Task ValidateAssignmentRulesAsync(
        RoleAssignment assignment,
        int? excludedAssignmentId = null)
    {
        if (assignment.EndDate.HasValue &&
            assignment.EndDate.Value.Date <
            assignment.StartDate.Date)
        {
            ModelState.AddModelError(
                nameof(assignment.EndDate),
                "Дата завершення не може бути раніше дати початку.");
        }

        if (assignment.IsCurrent &&
            assignment.EndDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(assignment.EndDate),
                "Поточний виконавець не повинен мати дату завершення.");
        }

        if (!assignment.IsCurrent &&
            !assignment.EndDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(assignment.EndDate),
                "Для завершеного призначення потрібно вказати дату завершення.");
        }

        if (assignment.CharacterRoleId <= 0 ||
            assignment.ActorId <= 0)
        {
            return;
        }

        var sameCurrentAssignmentExists =
            await _context.RoleAssignments
                .AnyAsync(existing =>
                    existing.CharacterRoleId ==
                        assignment.CharacterRoleId &&
                    existing.ActorId ==
                        assignment.ActorId &&
                    existing.IsCurrent &&
                    assignment.IsCurrent &&
                    (!excludedAssignmentId.HasValue ||
                     existing.Id !=
                        excludedAssignmentId.Value));

        if (sameCurrentAssignmentExists)
        {
            ModelState.AddModelError(
                nameof(assignment.ActorId),
                "Цей актор уже є поточним виконавцем цієї ролі.");
        }

        var sameStartDateAssignmentExists =
            await _context.RoleAssignments
                .AnyAsync(existing =>
                    existing.CharacterRoleId ==
                        assignment.CharacterRoleId &&
                    existing.ActorId ==
                        assignment.ActorId &&
                    existing.StartDate.Date ==
                        assignment.StartDate.Date &&
                    (!excludedAssignmentId.HasValue ||
                     existing.Id !=
                        excludedAssignmentId.Value));

        if (sameStartDateAssignmentExists)
        {
            ModelState.AddModelError(
                nameof(assignment.StartDate),
                "Для цього актора вже існує призначення " +
                "на цю роль із такою датою початку.");
        }
    }

    // Завантажує списки ролей та акторів для форми.
    private async Task PopulateSelectListsAsync(
        int? selectedRoleId,
        int? selectedActorId)
    {
        var roles = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Performance)
            .OrderBy(role => role.Performance.Title)
            .ThenBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                Label =
                    role.Performance.Title +
                    " — " +
                    role.Name
            })
            .ToListAsync();

        var actors = await _context.Actors
            .AsNoTracking()
            .OrderBy(actor => actor.LastName)
            .ThenBy(actor => actor.FirstName)
            .Select(actor => new
            {
                actor.Id,
                Label =
                    actor.FirstName +
                    " " +
                    actor.LastName
            })
            .ToListAsync();

        ViewData["CharacterRoleId"] =
            new SelectList(
                roles,
                "Id",
                "Label",
                selectedRoleId);

        ViewData["ActorId"] =
            new SelectList(
                actors,
                "Id",
                "Label",
                selectedActorId);
    }

    private static void NormalizeAssignment(
        RoleAssignment assignment)
    {
        assignment.StartDate =
            assignment.StartDate.Date;

        assignment.EndDate =
            assignment.EndDate?.Date;

        assignment.Notes =
            NormalizeOptionalText(assignment.Notes);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class ActorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public ActorsController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: Actors
    public async Task<IActionResult> Index()
    {
        var actors = await _context.Actors
            .AsNoTracking()
            .OrderBy(actor => actor.LastName)
            .ThenBy(actor => actor.FirstName)
            .ToListAsync();

        return View(actors);
    }

    // GET: Actors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actor = await _context.Actors
            .AsNoTracking()
            .Include(item => item.RoleAssignments)
                .ThenInclude(assignment => assignment.CharacterRole)
                    .ThenInclude(role => role.Performance)
            .Include(item => item.RehearsalParticipants)
                .ThenInclude(participant => participant.Rehearsal)
                    .ThenInclude(rehearsal => rehearsal.Performance)
            .Include(item => item.RehearsalParticipants)
                .ThenInclude(participant => participant.Rehearsal)
                    .ThenInclude(rehearsal => rehearsal.Hall)
                        .ThenInclude(hall => hall.Venue)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (actor == null)
        {
            return NotFound();
        }

        actor.RoleAssignments = actor.RoleAssignments
            .OrderByDescending(assignment => assignment.IsCurrent)
            .ThenBy(assignment => assignment.CharacterRole.Performance.Title)
            .ThenBy(assignment => assignment.CharacterRole.Name)
            .ToList();

        actor.RehearsalParticipants = actor.RehearsalParticipants
            .OrderByDescending(participant =>
                participant.Rehearsal.StartDateTime)
            .ToList();

        return View(actor);
    }

    // GET: Actors/Create
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Actors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create(
        [Bind("Id,FirstName,LastName,Email,PhoneNumber,Notes")]
        Actor actor)
    {
        NormalizeActor(actor);

        if (!ModelState.IsValid)
        {
            return View(actor);
        }

        _context.Actors.Add(actor);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "Actor",
            actor.Id,
            actor.FullName,
            $"Створено актора «{actor.FullName}».");

        TempData["SuccessMessage"] =
            $"Актора «{actor.FullName}» успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Actors/Edit/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actor = await _context.Actors
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (actor == null)
        {
            return NotFound();
        }

        return View(actor);
    }

    // POST: Actors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,FirstName,LastName,Email,PhoneNumber,Notes")]
        Actor actor)
    {
        if (id != actor.Id)
        {
            return NotFound();
        }

        NormalizeActor(actor);

        if (!ModelState.IsValid)
        {
            return View(actor);
        }

        var existingActor = await _context.Actors
            .FirstOrDefaultAsync(item => item.Id == id);

        if (existingActor == null)
        {
            return NotFound();
        }

        existingActor.FirstName = actor.FirstName;
        existingActor.LastName = actor.LastName;
        existingActor.Email = actor.Email;
        existingActor.PhoneNumber = actor.PhoneNumber;
        existingActor.Notes = actor.Notes;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "Actor",
            existingActor.Id,
            existingActor.FullName,
            $"Відредаговано актора «{existingActor.FullName}».");

        TempData["SuccessMessage"] =
            $"Актора «{existingActor.FullName}» успішно оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id = existingActor.Id });
    }

    // GET: Actors/Delete/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actor = await _context.Actors
            .AsNoTracking()
            .Include(item => item.RoleAssignments)
            .Include(item => item.RehearsalParticipants)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (actor == null)
        {
            return NotFound();
        }

        return View(actor);
    }

    // POST: Actors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var actor = await _context.Actors
            .FirstOrDefaultAsync(item => item.Id == id);

        if (actor == null)
        {
            return NotFound();
        }

        var hasRoleAssignments =
            await _context.RoleAssignments
                .AnyAsync(assignment =>
                    assignment.ActorId == id);

        var hasRehearsalParticipations =
            await _context.RehearsalParticipants
                .AnyAsync(participant =>
                    participant.ActorId == id);

        if (hasRoleAssignments ||
            hasRehearsalParticipations)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити актора, бо він уже має призначення на ролі або участь у репетиціях.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var actorName = actor.FullName;

        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "Actor",
            id,
            actorName,
            $"Видалено актора «{actorName}».");

        TempData["SuccessMessage"] =
            $"Актора «{actorName}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeActor(Actor actor)
    {
        actor.FirstName =
            actor.FirstName?.Trim()
            ?? string.Empty;

        actor.LastName =
            actor.LastName?.Trim()
            ?? string.Empty;

        actor.Email =
            NormalizeOptionalText(actor.Email);

        actor.PhoneNumber =
            NormalizeOptionalText(actor.PhoneNumber);

        actor.Notes =
            NormalizeOptionalText(actor.Notes);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
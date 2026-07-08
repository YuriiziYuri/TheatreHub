using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class RehearsalsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RehearsalsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Список репетицій
    public async Task<IActionResult> Index()
    {
        var rehearsals = await _context.Rehearsals
            .Include(r => r.Performance)
            .Include(r => r.Participants)
                .ThenInclude(p => p.Actor)
            .OrderBy(r => r.StartDateTime)
            .ToListAsync();

        return View(rehearsals);
    }

    // Деталі репетиції
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .Include(r => r.Performance)
            .Include(r => r.Participants)
                .ThenInclude(p => p.Actor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        return View(rehearsal);
    }

    // Форма створення
    public async Task<IActionResult> Create()
    {
        var model = new RehearsalFormViewModel
        {
            StartDateTime = DateTime.Today.AddDays(1).AddHours(16),
            EndDateTime = DateTime.Today.AddDays(1).AddHours(18)
        };

        await PopulateFormDataAsync(model);

        return View(model);
    }

    // Створення репетиції
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RehearsalFormViewModel model)
    {
        ValidateTime(model);

        var selectedActorIds = model.Actors
            .Where(a => a.IsSelected)
            .Select(a => a.ActorId)
            .ToList();

        if (selectedActorIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "Оберіть хоча б одного учасника репетиції.");
        }

        await ValidateConflictsAsync(
            model,
            selectedActorIds);

        if (!ModelState.IsValid)
        {
            await PopulateFormDataAsync(
                model,
                selectedActorIds);

            return View(model);
        }

        var rehearsal = new Rehearsal
        {
            PerformanceId = model.PerformanceId,
            StartDateTime = model.StartDateTime,
            EndDateTime = model.EndDateTime,
            Location = model.Location.Trim(),
            Notes = model.Notes,
            Status = model.Status
        };

        foreach (var actorId in selectedActorIds)
        {
            rehearsal.Participants.Add(
                new RehearsalParticipant
                {
                    ActorId = actorId
                });
        }

        _context.Rehearsals.Add(rehearsal);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Форма редагування
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        var selectedActorIds = rehearsal.Participants
            .Select(p => p.ActorId)
            .ToList();

        var model = new RehearsalFormViewModel
        {
            Id = rehearsal.Id,
            PerformanceId = rehearsal.PerformanceId,
            StartDateTime = rehearsal.StartDateTime,
            EndDateTime = rehearsal.EndDateTime,
            Location = rehearsal.Location,
            Notes = rehearsal.Notes,
            Status = rehearsal.Status
        };

        await PopulateFormDataAsync(
            model,
            selectedActorIds);

        return View(model);
    }

    // Збереження редагування
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        RehearsalFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        ValidateTime(model);

        var selectedActorIds = model.Actors
            .Where(a => a.IsSelected)
            .Select(a => a.ActorId)
            .ToList();

        if (selectedActorIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "Оберіть хоча б одного учасника репетиції.");
        }

        await ValidateConflictsAsync(
            model,
            selectedActorIds,
            id);

        if (!ModelState.IsValid)
        {
            await PopulateFormDataAsync(
                model,
                selectedActorIds);

            return View(model);
        }

        var rehearsal = await _context.Rehearsals
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        rehearsal.PerformanceId = model.PerformanceId;
        rehearsal.StartDateTime = model.StartDateTime;
        rehearsal.EndDateTime = model.EndDateTime;
        rehearsal.Location = model.Location.Trim();
        rehearsal.Notes = model.Notes;
        rehearsal.Status = model.Status;

        _context.RehearsalParticipants.RemoveRange(
            rehearsal.Participants);

        rehearsal.Participants = selectedActorIds
            .Select(actorId => new RehearsalParticipant
            {
                RehearsalId = rehearsal.Id,
                ActorId = actorId
            })
            .ToList();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Підтвердження видалення
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .Include(r => r.Performance)
            .Include(r => r.Participants)
                .ThenInclude(p => p.Actor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        return View(rehearsal);
    }

    // Видалення
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rehearsal = await _context.Rehearsals
            .FindAsync(id);

        if (rehearsal != null)
        {
            _context.Rehearsals.Remove(rehearsal);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateTime(
        RehearsalFormViewModel model)
    {
        if (model.EndDateTime <= model.StartDateTime)
        {
            ModelState.AddModelError(
                nameof(model.EndDateTime),
                "Час завершення має бути пізніше часу початку.");
        }
    }

    private async Task ValidateConflictsAsync(
        RehearsalFormViewModel model,
        List<int> selectedActorIds,
        int? excludedRehearsalId = null)
    {
        if (model.EndDateTime <= model.StartDateTime)
        {
            return;
        }

        var overlappingRehearsals =
            await _context.Rehearsals
                .Include(r => r.Participants)
                    .ThenInclude(p => p.Actor)
                .Where(r =>
                    (!excludedRehearsalId.HasValue ||
                     r.Id != excludedRehearsalId.Value) &&
                    r.StartDateTime < model.EndDateTime &&
                    r.EndDateTime > model.StartDateTime)
                .ToListAsync();

        var actorConflicts = overlappingRehearsals
            .SelectMany(r => r.Participants)
            .Where(p =>
                selectedActorIds.Contains(p.ActorId))
            .Select(p => p.Actor.FullName)
            .Distinct()
            .ToList();

        if (actorConflicts.Count > 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "У цей час зайняті актори: " +
                string.Join(", ", actorConflicts));
        }

        var normalizedLocation =
            model.Location?.Trim();

        var locationConflict =
            overlappingRehearsals.Any(r =>
                string.Equals(
                    r.Location.Trim(),
                    normalizedLocation,
                    StringComparison.OrdinalIgnoreCase));

        if (locationConflict)
        {
            ModelState.AddModelError(
                nameof(model.Location),
                "У цей час вказане місце вже зайняте.");
        }
    }

    private async Task PopulateFormDataAsync(
        RehearsalFormViewModel model,
        IEnumerable<int>? selectedActorIds = null)
    {
        var selectedIds =
            selectedActorIds?.ToHashSet()
            ?? new HashSet<int>();

        model.Performances =
            await _context.Performances
                .OrderBy(p => p.Title)
                .ToListAsync();

        model.Actors =
            await _context.Actors
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .Select(a =>
                    new ActorSelectionViewModel
                    {
                        ActorId = a.Id,
                        FullName =
                            a.FirstName + " " + a.LastName,
                        IsSelected =
                            selectedIds.Contains(a.Id)
                    })
                .ToListAsync();
    }
}
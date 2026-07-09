using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class RehearsalsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RehearsalsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // СПИСОК РЕПЕТИЦІЙ
    // =========================================================

    // GET: Rehearsals
    public async Task<IActionResult> Index()
    {
        var rehearsals = await _context.Rehearsals
            .AsNoTracking()
            .Include(rehearsal => rehearsal.Performance)
            .Include(rehearsal => rehearsal.Hall)
                .ThenInclude(hall => hall.Venue)
            .Include(rehearsal => rehearsal.Participants)
                .ThenInclude(participant => participant.Actor)
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToListAsync();

        return View(rehearsals);
    }

    // =========================================================
    // ДЕТАЛІ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall.Venue)
            .Include(item => item.Participants)
                .ThenInclude(participant => participant.Actor)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (rehearsal == null)
        {
            return NotFound();
        }

        return View(rehearsal);
    }

    // =========================================================
    // СТВОРЕННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Create
    public async Task<IActionResult> Create()
    {
        var model = new RehearsalFormViewModel
        {
            StartDateTime = DateTime.Today
                .AddDays(1)
                .AddHours(16),

            EndDateTime = DateTime.Today
                .AddDays(1)
                .AddHours(18),

            Status = RehearsalStatus.Planned
        };

        await PopulateFormDataAsync(model);

        return View(model);
    }

    // POST: Rehearsals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RehearsalFormViewModel model)
    {
        ValidateTime(model);

        await ValidatePerformanceAsync(
            model.PerformanceId);

        await ValidateHallAsync(
            model.HallId);

        var selectedActorIds = model.Actors
            .Where(actor => actor.IsSelected)
            .Select(actor => actor.ActorId)
            .Distinct()
            .ToList();

        if (selectedActorIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "Оберіть хоча б одного учасника репетиції.");
        }
        else
        {
            await ValidateSelectedActorsAsync(
                selectedActorIds);
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
            HallId = model.HallId,
            StartDateTime = model.StartDateTime,
            EndDateTime = model.EndDateTime,
            Notes = NormalizeOptionalText(model.Notes),
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

        TempData["SuccessMessage"] =
            "Репетицію успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // РЕДАГУВАННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (rehearsal == null)
        {
            return NotFound();
        }

        var selectedActorIds = rehearsal.Participants
            .Select(participant => participant.ActorId)
            .ToList();

        var model = new RehearsalFormViewModel
        {
            Id = rehearsal.Id,
            PerformanceId = rehearsal.PerformanceId,
            HallId = rehearsal.HallId,
            StartDateTime = rehearsal.StartDateTime,
            EndDateTime = rehearsal.EndDateTime,
            Notes = rehearsal.Notes,
            Status = rehearsal.Status
        };

        await PopulateFormDataAsync(
            model,
            selectedActorIds);

        return View(model);
    }

    // POST: Rehearsals/Edit/5
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

        await ValidatePerformanceAsync(
            model.PerformanceId);

        await ValidateHallAsync(
            model.HallId);

        var selectedActorIds = model.Actors
            .Where(actor => actor.IsSelected)
            .Select(actor => actor.ActorId)
            .Distinct()
            .ToList();

        if (selectedActorIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "Оберіть хоча б одного учасника репетиції.");
        }
        else
        {
            await ValidateSelectedActorsAsync(
                selectedActorIds);
        }

        await ValidateConflictsAsync(
            model,
            selectedActorIds,
            excludedRehearsalId: id);

        if (!ModelState.IsValid)
        {
            await PopulateFormDataAsync(
                model,
                selectedActorIds);

            return View(model);
        }

        var rehearsal = await _context.Rehearsals
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        rehearsal.PerformanceId =
            model.PerformanceId;

        rehearsal.HallId =
            model.HallId;

        rehearsal.StartDateTime =
            model.StartDateTime;

        rehearsal.EndDateTime =
            model.EndDateTime;

        rehearsal.Notes =
            NormalizeOptionalText(model.Notes);

        rehearsal.Status =
            model.Status;

        /*
         * Не видаляємо всіх учасників повністю.
         * Інакше зітруться їхні відмітки про відвідування.
         */

        var selectedIds =
            selectedActorIds.ToHashSet();

        var participantsToRemove =
            rehearsal.Participants
                .Where(participant =>
                    !selectedIds.Contains(
                        participant.ActorId))
                .ToList();

        _context.RehearsalParticipants.RemoveRange(
            participantsToRemove);

        var existingActorIds =
            rehearsal.Participants
                .Where(participant =>
                    selectedIds.Contains(
                        participant.ActorId))
                .Select(participant =>
                    participant.ActorId)
                .ToHashSet();

        var actorIdsToAdd =
            selectedIds
                .Where(actorId =>
                    !existingActorIds.Contains(actorId))
                .ToList();

        foreach (var actorId in actorIdsToAdd)
        {
            rehearsal.Participants.Add(
                new RehearsalParticipant
                {
                    ActorId = actorId
                });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Репетицію успішно оновлено.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // ВІДВІДУВАННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Attendance/5
    public async Task<IActionResult> Attendance(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall.Venue)
            .Include(item => item.Participants)
                .ThenInclude(participant => participant.Actor)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (rehearsal == null)
        {
            return NotFound();
        }

        var model = new RehearsalAttendanceViewModel
        {
            RehearsalId = rehearsal.Id,

            PerformanceTitle =
                rehearsal.Performance.Title,

            StartDateTime =
                rehearsal.StartDateTime,

            EndDateTime =
                rehearsal.EndDateTime,

            HallName =
                rehearsal.Hall.Name,

            VenueName =
                rehearsal.Hall.Venue.Name,

            Participants = rehearsal.Participants
                .OrderBy(participant =>
                    participant.Actor.LastName)
                .ThenBy(participant =>
                    participant.Actor.FirstName)
                .Select(participant =>
                    new AttendanceParticipantViewModel
                    {
                        ActorId =
                            participant.ActorId,

                        FullName =
                            participant.Actor.FullName,

                        ResponseStatus =
                            participant.ResponseStatus,

                        AttendanceStatus =
                            participant.AttendanceStatus,

                        LateMinutes =
                            participant.LateMinutes,

                        AbsenceReason =
                            participant.AbsenceReason,

                        Comment =
                            participant.Comment
                    })
                .ToList()
        };

        return View(model);
    }

    // POST: Rehearsals/Attendance/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Attendance(
        int id,
        RehearsalAttendanceViewModel model)
    {
        if (id != model.RehearsalId)
        {
            return NotFound();
        }

        ValidateAttendance(model);

        var rehearsal = await _context.Rehearsals
            .Include(item => item.Participants)
                .ThenInclude(participant =>
                    participant.Actor)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        var duplicateActorIds = model.Participants
            .GroupBy(participant => participant.ActorId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateActorIds.Count > 0)
        {
            ModelState.AddModelError(
                nameof(model.Participants),
                "Список учасників містить повторювані записи.");
        }

        var actualActorIds = rehearsal.Participants
            .Select(participant => participant.ActorId)
            .ToHashSet();

        var submittedActorIds = model.Participants
            .Select(participant => participant.ActorId)
            .ToHashSet();

        if (!actualActorIds.SetEquals(submittedActorIds))
        {
            ModelState.AddModelError(
                nameof(model.Participants),
                "Склад учасників репетиції змінився. Оновіть сторінку.");
        }

        if (!ModelState.IsValid)
        {
            await RestoreAttendanceHeaderAsync(model);

            return View(model);
        }

        var submittedParticipants = model.Participants
            .ToDictionary(
                participant => participant.ActorId);

        foreach (var participant in rehearsal.Participants)
        {
            var submitted =
                submittedParticipants[participant.ActorId];

            participant.ResponseStatus =
                submitted.ResponseStatus;

            participant.AttendanceStatus =
                submitted.AttendanceStatus;

            participant.LateMinutes =
                submitted.AttendanceStatus ==
                    AttendanceStatus.Late
                        ? submitted.LateMinutes
                        : 0;

            if (submitted.AttendanceStatus ==
                    AttendanceStatus.Absent ||
                submitted.AttendanceStatus ==
                    AttendanceStatus.Excused)
            {
                participant.AbsenceReason =
                    NormalizeOptionalText(
                        submitted.AbsenceReason);
            }
            else
            {
                participant.AbsenceReason = null;
            }

            participant.Comment =
                NormalizeOptionalText(
                    submitted.Comment);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Дані про відвідування успішно збережено.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    // =========================================================
    // ВИДАЛЕННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall.Venue)
            .Include(item => item.Participants)
                .ThenInclude(participant => participant.Actor)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (rehearsal == null)
        {
            return NotFound();
        }

        return View(rehearsal);
    }

    // POST: Rehearsals/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rehearsal = await _context.Rehearsals
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        _context.RehearsalParticipants.RemoveRange(
            rehearsal.Participants);

        _context.Rehearsals.Remove(rehearsal);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Репетицію видалено.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // ВАЛІДАЦІЯ ЧАСУ
    // =========================================================

    private void ValidateTime(
        RehearsalFormViewModel model)
    {
        if (model.StartDateTime == default)
        {
            ModelState.AddModelError(
                nameof(model.StartDateTime),
                "Вкажіть дату та час початку.");
        }

        if (model.EndDateTime == default)
        {
            ModelState.AddModelError(
                nameof(model.EndDateTime),
                "Вкажіть дату та час завершення.");
        }

        if (model.StartDateTime != default &&
            model.EndDateTime != default &&
            model.EndDateTime <= model.StartDateTime)
        {
            ModelState.AddModelError(
                nameof(model.EndDateTime),
                "Час завершення має бути пізніше часу початку.");
        }
    }

    // =========================================================
    // ВАЛІДАЦІЯ ВИСТАВИ
    // =========================================================

    private async Task ValidatePerformanceAsync(
        int performanceId)
    {
        if (performanceId <= 0)
        {
            ModelState.AddModelError(
                nameof(RehearsalFormViewModel.PerformanceId),
                "Оберіть виставу.");

            return;
        }

        var performanceExists =
            await _context.Performances.AnyAsync(
                performance =>
                    performance.Id == performanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(RehearsalFormViewModel.PerformanceId),
                "Обрана вистава не існує.");
        }
    }

    // =========================================================
    // ВАЛІДАЦІЯ ЗАЛУ
    // =========================================================

    private async Task ValidateHallAsync(int hallId)
    {
        if (hallId <= 0)
        {
            ModelState.AddModelError(
                nameof(RehearsalFormViewModel.HallId),
                "Оберіть зал.");

            return;
        }

        var hallExists =
            await _context.Halls.AnyAsync(
                hall => hall.Id == hallId);

        if (!hallExists)
        {
            ModelState.AddModelError(
                nameof(RehearsalFormViewModel.HallId),
                "Обраний зал не існує.");
        }
    }

    // =========================================================
    // ВАЛІДАЦІЯ АКТОРІВ
    // =========================================================

    private async Task ValidateSelectedActorsAsync(
        IReadOnlyCollection<int> selectedActorIds)
    {
        var existingActorsCount =
            await _context.Actors.CountAsync(
                actor =>
                    selectedActorIds.Contains(actor.Id));

        if (existingActorsCount != selectedActorIds.Count)
        {
            ModelState.AddModelError(
                nameof(RehearsalFormViewModel.Actors),
                "Один або декілька вибраних акторів не існують.");
        }
    }

    // =========================================================
    // ПЕРЕВІРКА КОНФЛІКТІВ
    // =========================================================

    private async Task ValidateConflictsAsync(
        RehearsalFormViewModel model,
        IReadOnlyCollection<int> selectedActorIds,
        int? excludedRehearsalId = null)
    {
        if (model.StartDateTime == default ||
            model.EndDateTime == default ||
            model.EndDateTime <= model.StartDateTime)
        {
            return;
        }

        // Скасована репетиція не займає зал і акторів.
        if (model.Status == RehearsalStatus.Cancelled)
        {
            return;
        }

        var overlappingRehearsals =
            await _context.Rehearsals
                .AsNoTracking()
                .Include(rehearsal =>
                    rehearsal.Participants)
                    .ThenInclude(participant =>
                        participant.Actor)
                .Where(rehearsal =>
                    rehearsal.Status !=
                        RehearsalStatus.Cancelled &&

                    (!excludedRehearsalId.HasValue ||
                     rehearsal.Id !=
                        excludedRehearsalId.Value) &&

                    rehearsal.StartDateTime <
                        model.EndDateTime &&

                    rehearsal.EndDateTime >
                        model.StartDateTime)
                .ToListAsync();

        var actorConflicts = overlappingRehearsals
            .SelectMany(rehearsal =>
                rehearsal.Participants)
            .Where(participant =>
                selectedActorIds.Contains(
                    participant.ActorId))
            .Select(participant =>
                participant.Actor.FullName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        if (actorConflicts.Count > 0)
        {
            ModelState.AddModelError(
                nameof(model.Actors),
                "У цей час зайняті актори: " +
                string.Join(", ", actorConflicts));
        }

        if (model.HallId > 0)
        {
            var hallConflict =
                overlappingRehearsals.Any(
                    rehearsal =>
                        rehearsal.HallId ==
                        model.HallId);

            if (hallConflict)
            {
                ModelState.AddModelError(
                    nameof(model.HallId),
                    "У цей час обраний зал уже зайнятий.");
            }
        }
    }

    // =========================================================
    // ВАЛІДАЦІЯ ВІДВІДУВАННЯ
    // =========================================================

    private void ValidateAttendance(
        RehearsalAttendanceViewModel model)
    {
        if (model.Participants == null ||
            model.Participants.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.Participants),
                "У репетиції немає учасників.");

            return;
        }

        for (var i = 0;
             i < model.Participants.Count;
             i++)
        {
            var participant =
                model.Participants[i];

            if (!Enum.IsDefined(
                    typeof(ParticipationResponseStatus),
                    participant.ResponseStatus))
            {
                ModelState.AddModelError(
                    $"Participants[{i}].ResponseStatus",
                    "Оберіть правильний статус підтвердження.");
            }

            if (!Enum.IsDefined(
                    typeof(AttendanceStatus),
                    participant.AttendanceStatus))
            {
                ModelState.AddModelError(
                    $"Participants[{i}].AttendanceStatus",
                    "Оберіть правильний статус відвідування.");
            }

            if (participant.AttendanceStatus ==
                    AttendanceStatus.Late &&
                participant.LateMinutes <= 0)
            {
                ModelState.AddModelError(
                    $"Participants[{i}].LateMinutes",
                    "Для запізнення вкажіть кількість хвилин.");
            }

            if (participant.LateMinutes < 0 ||
                participant.LateMinutes > 600)
            {
                ModelState.AddModelError(
                    $"Participants[{i}].LateMinutes",
                    "Кількість хвилин має бути від 0 до 600.");
            }

            var absenceRequiresReason =
                participant.AttendanceStatus ==
                    AttendanceStatus.Absent ||
                participant.AttendanceStatus ==
                    AttendanceStatus.Excused;

            if (absenceRequiresReason &&
                string.IsNullOrWhiteSpace(
                    participant.AbsenceReason))
            {
                ModelState.AddModelError(
                    $"Participants[{i}].AbsenceReason",
                    "Вкажіть причину відсутності.");
            }
        }
    }

    // =========================================================
    // ЗАПОВНЕННЯ ФОРМИ РЕПЕТИЦІЇ
    // =========================================================

    private async Task PopulateFormDataAsync(
        RehearsalFormViewModel model,
        IEnumerable<int>? selectedActorIds = null)
    {
        var selectedIds =
            selectedActorIds?.ToHashSet()
            ?? new HashSet<int>();

        model.Performances =
            await _context.Performances
                .AsNoTracking()
                .OrderBy(performance =>
                    performance.Title)
                .ToListAsync();

        model.Halls =
            await _context.Halls
                .AsNoTracking()
                .Include(hall => hall.Venue)
                .Where(hall =>
                    (hall.IsActive &&
                     hall.Venue.IsActive) ||
                    hall.Id == model.HallId)
                .OrderBy(hall =>
                    hall.Venue.Name)
                .ThenBy(hall =>
                    hall.Name)
                .ToListAsync();

        model.Actors =
            await _context.Actors
                .AsNoTracking()
                .OrderBy(actor =>
                    actor.LastName)
                .ThenBy(actor =>
                    actor.FirstName)
                .Select(actor =>
                    new ActorSelectionViewModel
                    {
                        ActorId = actor.Id,

                        FullName =
                            actor.FirstName +
                            " " +
                            actor.LastName,

                        IsSelected =
                            selectedIds.Contains(actor.Id)
                    })
                .ToListAsync();
    }

    // =========================================================
    // ВІДНОВЛЕННЯ ДАНИХ СТОРІНКИ ВІДВІДУВАННЯ
    // =========================================================

    private async Task RestoreAttendanceHeaderAsync(
        RehearsalAttendanceViewModel model)
    {
        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall.Venue)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == model.RehearsalId);

        if (rehearsal == null)
        {
            return;
        }

        model.PerformanceTitle =
            rehearsal.Performance.Title;

        model.StartDateTime =
            rehearsal.StartDateTime;

        model.EndDateTime =
            rehearsal.EndDateTime;

        model.HallName =
            rehearsal.Hall.Name;

        model.VenueName =
            rehearsal.Hall.Venue.Name;

        var actorIds = model.Participants
            .Select(participant =>
                participant.ActorId)
            .Distinct()
            .ToList();

        var actorNames =
            await _context.Actors
                .AsNoTracking()
                .Where(actor =>
                    actorIds.Contains(actor.Id))
                .ToDictionaryAsync(
                    actor => actor.Id,
                    actor =>
                        actor.FirstName +
                        " " +
                        actor.LastName);

        foreach (var participant in model.Participants)
        {
            if (actorNames.TryGetValue(
                    participant.ActorId,
                    out var fullName))
            {
                participant.FullName =
                    fullName;
            }
        }
    }

    // =========================================================
    // НОРМАЛІЗАЦІЯ ТЕКСТУ
    // =========================================================

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
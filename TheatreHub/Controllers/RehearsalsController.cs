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
public class RehearsalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public RehearsalsController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
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
            .Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
                .ThenInclude(scene => scene!.Act)
            .Include(rehearsal => rehearsal.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Include(rehearsal => rehearsal.Participants)
                .ThenInclude(participant => participant.Actor)
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToListAsync();

        return View(rehearsals);
    }

    public async Task<IActionResult> Calendar(
        DateTime? startDate,
        DateTime? endDate,
        int? performanceId,
        int? actorId,
        int? hallId)
    {
        var today = DateTime.Today;

        var calendarStartDate =
            startDate?.Date ?? today;

        var calendarEndDate =
            endDate?.Date ?? today.AddDays(14);

        if (calendarEndDate < calendarStartDate)
        {
            calendarEndDate =
                calendarStartDate.AddDays(14);
        }

        var queryStart =
            calendarStartDate;

        var queryEnd =
            calendarEndDate.AddDays(1);

        var rehearsalQuery = _context.Rehearsals
            .AsNoTracking()
            .Include(rehearsal => rehearsal.Performance)
            .Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
            .Include(rehearsal => rehearsal.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Include(rehearsal => rehearsal.Participants)
            .Where(rehearsal =>
                rehearsal.StartDateTime < queryEnd &&
                rehearsal.EndDateTime >= queryStart);

        if (performanceId.HasValue)
        {
            rehearsalQuery = rehearsalQuery.Where(rehearsal =>
                rehearsal.PerformanceId == performanceId.Value);
        }

        if (actorId.HasValue)
        {
            rehearsalQuery = rehearsalQuery.Where(rehearsal =>
                rehearsal.Participants.Any(participant =>
                    participant.ActorId == actorId.Value));
        }

        if (hallId.HasValue)
        {
            rehearsalQuery = rehearsalQuery.Where(rehearsal =>
                rehearsal.HallId == hallId.Value);
        }

        var rehearsals = await rehearsalQuery
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToListAsync();

        var rehearsalItems = rehearsals
            .Select(rehearsal =>
                new RehearsalCalendarItemViewModel
                {
                    Id = rehearsal.Id,

                    IsPerformanceShow = false,
                    EventTypeText = "Репетиція",
                    EventBadgeClass = "text-bg-primary",

                    PerformanceTitle =
                        rehearsal.Performance.Title,

                    TargetText =
                        GetRehearsalTargetText(rehearsal),

                    HallText =
                        GetRehearsalHallText(rehearsal),

                    StartDateTime =
                        rehearsal.StartDateTime,

                    EndDateTime =
                        rehearsal.EndDateTime,

                    StatusText =
                        GetRehearsalStatusText(rehearsal.Status),

                    ParticipantsCount =
                        rehearsal.Participants.Count
                })
            .ToList();

        var showQuery = _context.PerformanceShows
            .AsNoTracking()
            .Include(show => show.Performance)
            .Include(show => show.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Where(show =>
                show.StartDateTime < queryEnd &&
                show.EndDateTime >= queryStart &&
                show.Status != PerformanceShowStatus.Cancelled);

        if (performanceId.HasValue)
        {
            showQuery = showQuery.Where(show =>
                show.PerformanceId == performanceId.Value);
        }

        if (hallId.HasValue)
        {
            showQuery = showQuery.Where(show =>
                show.HallId == hallId.Value);
        }

        if (actorId.HasValue)
        {
            var performanceIdsForActor = await _context.RoleAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.ActorId == actorId.Value &&
                    assignment.IsCurrent &&
                    assignment.Status == RoleAssignmentStatus.Approved)
                .Select(assignment =>
                    assignment.CharacterRole.PerformanceId)
                .Distinct()
                .ToListAsync();

            showQuery = showQuery.Where(show =>
                performanceIdsForActor.Contains(show.PerformanceId));
        }

        var shows = await showQuery
            .OrderBy(show => show.StartDateTime)
            .ToListAsync();

        var showItems = shows
            .Select(show =>
                new RehearsalCalendarItemViewModel
                {
                    Id = show.Id,

                    IsPerformanceShow = true,
                    EventTypeText = "Показ",
                    EventBadgeClass = "text-bg-success",

                    PerformanceTitle =
                        show.Performance.Title,

                    TargetText =
                        GetShowTargetText(show),

                    HallText =
                        GetShowLocationText(show),

                    StartDateTime =
                        show.StartDateTime,

                    EndDateTime =
                        show.EndDateTime,

                    StatusText =
                        GetShowStatusText(show.Status),

                    ParticipantsCount = 0
                })
            .ToList();

        var calendarItems = rehearsalItems
            .Concat(showItems)
            .OrderBy(item => item.StartDateTime)
            .ThenBy(item => item.EndDateTime)
            .ToList();

        var days = new List<RehearsalCalendarDayViewModel>();

        for (var date = calendarStartDate;
             date <= calendarEndDate;
             date = date.AddDays(1))
        {
            var dayItems = calendarItems
                .Where(item =>
                    item.StartDateTime.Date == date)
                .OrderBy(item =>
                    item.StartDateTime)
                .ToList();

            days.Add(
                new RehearsalCalendarDayViewModel
                {
                    Date = date,
                    Rehearsals = dayItems
                });
        }

        var model = new RehearsalCalendarViewModel
        {
            StartDate = calendarStartDate,
            EndDate = calendarEndDate,
            PerformanceId = performanceId,
            ActorId = actorId,
            HallId = hallId,
            Days = days,

            Performances = await _context.Performances
                .AsNoTracking()
                .OrderBy(performance =>
                    performance.Title)
                .ToListAsync(),

            Actors = await _context.Actors
                .AsNoTracking()
                .OrderBy(actor =>
                    actor.LastName)
                .ThenBy(actor =>
                    actor.FirstName)
                .ToListAsync(),

            Halls = await _context.Halls
                .AsNoTracking()
                .Include(hall => hall.Venue)
                .Where(hall =>
                    hall.IsActive &&
                    hall.Venue!.IsActive)
                .OrderBy(hall =>
                    hall.Venue!.Name)
                .ThenBy(hall =>
                    hall.Name)
                .ToListAsync()
        };

        return View(model);
    }

    private static string GetRehearsalTargetText(
        Rehearsal rehearsal)
    {
        if (rehearsal.Scene != null)
        {
            return
                $"Сцена {rehearsal.Scene.Number}. {rehearsal.Scene.Title}";
        }

        if (rehearsal.Act != null)
        {
            return rehearsal.Act.DisplayName;
        }

        return "Уся вистава";
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
            .Include(item => item.Performance).Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
                .ThenInclude(scene => scene!.Act)

            .Include(item => item.Hall)
                .ThenInclude(hall => hall!.Venue!)
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
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
    public async Task<IActionResult> Create(
    int? performanceId,
    int? actId,
    int? sceneId)
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

        if (sceneId.HasValue)
        {
            var scene = await _context.Scenes
                .AsNoTracking()
                .Include(item => item.Act)
                .FirstOrDefaultAsync(item =>
                    item.Id == sceneId.Value);

            if (scene == null)
            {
                return NotFound();
            }

            model.PerformanceId =
                scene.Act.PerformanceId;

            model.ActId =
                scene.ActId;

            model.SceneId =
                scene.Id;
        }
        else if (actId.HasValue)
        {
            var act = await _context.Acts
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == actId.Value);

            if (act == null)
            {
                return NotFound();
            }

            model.PerformanceId =
                act.PerformanceId;

            model.ActId =
                act.Id;
        }
        else if (performanceId.HasValue)
        {
            var performanceExists =
                await _context.Performances
                    .AsNoTracking()
                    .AnyAsync(item =>
                        item.Id == performanceId.Value);

            if (!performanceExists)
            {
                return NotFound();
            }

            model.PerformanceId =
                performanceId.Value;
        }

        await PopulateFormDataAsync(model);

        return View(model);
    }

    // POST: Rehearsals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
    public async Task<IActionResult> Create(
    RehearsalFormViewModel model,
    string? submitAction)
    {
        await ValidatePerformanceAsync(model.PerformanceId);
        await ValidateStructureTargetAsync(model);

        if (submitAction == "fillActors")
        {
            var suggestedActorIds =
                await GetSuggestedActorIdsAsync(model);

            await PopulateFormDataAsync(
                model,
                suggestedActorIds);

            ModelState.Clear();

            TempData["SuccessMessage"] =
                suggestedActorIds.Count == 0
                    ? "Для вибраної сцени, дії або вистави не знайдено затверджених акторів основного складу."
                    : $"Автоматично підібрано акторів: {suggestedActorIds.Count}.";

            return View(model);
        }

        ValidateTime(model);
        await ValidateHallAsync(model.HallId);

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
            ActId = model.ActId,
            SceneId = model.SceneId,
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

        var performanceTitle = await _context.Performances
            .Where(performance =>
                performance.Id == rehearsal.PerformanceId)
            .Select(performance =>
                performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "Rehearsal",
            rehearsal.Id,
            performanceTitle,
            $"Створено репетицію вистави «{performanceTitle}» на {rehearsal.StartDateTime:dd.MM.yyyy HH:mm}.");

        TempData["SuccessMessage"] =
            "Репетицію успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // РЕДАГУВАННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Edit/5
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
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
            ActId = rehearsal.ActId,
            SceneId = rehearsal.SceneId,
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
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
    public async Task<IActionResult> Edit(
    int id,
    RehearsalFormViewModel model,
    string? submitAction)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        await ValidatePerformanceAsync(model.PerformanceId);
        await ValidateStructureTargetAsync(model);

        if (submitAction == "fillActors")
        {
            var suggestedActorIds =
                await GetSuggestedActorIdsAsync(model);

            await PopulateFormDataAsync(
                model,
                suggestedActorIds);

            ModelState.Clear();

            TempData["SuccessMessage"] =
                suggestedActorIds.Count == 0
                    ? "Для вибраної сцени, дії або вистави не знайдено затверджених акторів основного складу."
                    : $"Автоматично підібрано акторів: {suggestedActorIds.Count}.";

            return View(model);
        }

        ValidateTime(model);
        await ValidateHallAsync(model.HallId);

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
            .Include(item => item.Performance)
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        rehearsal.PerformanceId =
            model.PerformanceId;

        rehearsal.ActId =
            model.ActId;

        rehearsal.SceneId =
            model.SceneId;

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

        var performanceTitle = await _context.Performances
            .Where(performance =>
                performance.Id == rehearsal.PerformanceId)
            .Select(performance =>
                performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "Rehearsal",
            rehearsal.Id,
            performanceTitle,
            $"Оновлено репетицію вистави «{performanceTitle}» на {rehearsal.StartDateTime:dd.MM.yyyy HH:mm}.");

        TempData["SuccessMessage"] =
            "Репетицію успішно оновлено.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================================
    // ВІДВІДУВАННЯ РЕПЕТИЦІЇ
    // =========================================================

    // GET: Rehearsals/Attendance/5
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
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
                .ThenInclude(hall => hall!.Venue!)
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
                rehearsal.Hall.Venue!.Name,

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
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
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
            .Include(item => item.Performance)
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

        await _actionLogService.LogAsync(
            User,
            "UpdateAttendance",
            "Rehearsal",
            rehearsal.Id,
            rehearsal.Performance.Title,
            $"Оновлено відвідування репетиції вистави «{rehearsal.Performance.Title}» на {rehearsal.StartDateTime:dd.MM.yyyy HH:mm}.");

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
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rehearsal = await _context.Rehearsals
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
                .ThenInclude(scene => scene!.Act)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall!.Venue!)
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
    [Authorize(Policy = AppPolicies.CanManageRehearsals)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rehearsal = await _context.Rehearsals
            .Include(item => item.Performance)
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (rehearsal == null)
        {
            return NotFound();
        }

        var performanceTitle = rehearsal.Performance.Title;
        var rehearsalDateTime = rehearsal.StartDateTime;

        _context.RehearsalParticipants.RemoveRange(
            rehearsal.Participants);

        _context.Rehearsals.Remove(rehearsal);

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "Rehearsal",
            id,
            performanceTitle,
            $"Видалено репетицію вистави «{performanceTitle}» на {rehearsalDateTime:dd.MM.yyyy HH:mm}.");

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

        model.Performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .ToListAsync();

        model.Acts = await _context.Acts
            .AsNoTracking()
            .Include(act => act.Performance)
            .OrderBy(act =>
                act.Performance.Title)
            .ThenBy(act =>
                act.Position)
            .ThenBy(act =>
                act.Number)
            .ToListAsync();

        model.Scenes = await _context.Scenes
            .AsNoTracking()
            .Include(scene => scene.Act)
                .ThenInclude(act => act.Performance)
            .OrderBy(scene =>
                scene.Act.Performance.Title)
            .ThenBy(scene =>
                scene.Act.Position)
            .ThenBy(scene =>
                scene.Act.Number)
            .ThenBy(scene =>
                scene.Position)
            .ThenBy(scene =>
                scene.Number)
            .ToListAsync();

        model.Halls = await _context.Halls
            .AsNoTracking()
            .Include(hall => hall.Venue)
            .Where(hall =>
                hall.IsActive &&
                hall.Venue!.IsActive)
            .OrderBy(hall =>
                hall.Venue!.Name)
            .ThenBy(hall =>
                hall.Name)
            .ToListAsync();

        model.Actors = await _context.Actors
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
                .ThenInclude(hall => hall!.Venue!)
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
            rehearsal.Hall.Venue!.Name;

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

    private async Task ValidateStructureTargetAsync(
    RehearsalFormViewModel model)
    {
        if (!model.ActId.HasValue &&
            !model.SceneId.HasValue)
        {
            return;
        }

        Act? selectedAct = null;

        if (model.ActId.HasValue)
        {
            selectedAct = await _context.Acts
                .AsNoTracking()
                .FirstOrDefaultAsync(act =>
                    act.Id == model.ActId.Value);

            if (selectedAct == null)
            {
                ModelState.AddModelError(
                    nameof(model.ActId),
                    "Обрана дія не існує.");

                return;
            }

            if (selectedAct.PerformanceId != model.PerformanceId)
            {
                ModelState.AddModelError(
                    nameof(model.ActId),
                    "Обрана дія не належить вибраній виставі.");
            }
        }

        if (model.SceneId.HasValue)
        {
            var selectedScene = await _context.Scenes
                .AsNoTracking()
                .Include(scene => scene.Act)
                .FirstOrDefaultAsync(scene =>
                    scene.Id == model.SceneId.Value);

            if (selectedScene == null)
            {
                ModelState.AddModelError(
                    nameof(model.SceneId),
                    "Обрана сцена не існує.");

                return;
            }

            if (selectedScene.Act.PerformanceId !=
                model.PerformanceId)
            {
                ModelState.AddModelError(
                    nameof(model.SceneId),
                    "Обрана сцена не належить вибраній виставі.");
            }

            if (model.ActId.HasValue &&
                selectedScene.ActId != model.ActId.Value)
            {
                ModelState.AddModelError(
                    nameof(model.SceneId),
                    "Обрана сцена не належить вибраній дії.");
            }

            if (!model.ActId.HasValue)
            {
                model.ActId = selectedScene.ActId;
            }
        }
    }

    // =========================================================

    private async Task<List<int>> GetSuggestedActorIdsAsync(
    RehearsalFormViewModel model)
    {
        if (model.SceneId.HasValue)
        {
            return await GetActorIdsForSceneAsync(
                model.SceneId.Value);
        }

        if (model.ActId.HasValue)
        {
            return await GetActorIdsForActAsync(
                model.ActId.Value);
        }

        if (model.PerformanceId > 0)
        {
            return await GetActorIdsForPerformanceAsync(
                model.PerformanceId);
        }

        return [];
    }

    private async Task<List<int>> GetActorIdsForSceneAsync(
        int sceneId)
    {
        return await _context.SceneRoles
            .AsNoTracking()
            .Where(sceneRole =>
                sceneRole.SceneId == sceneId &&
                sceneRole.IsRequired)
            .SelectMany(sceneRole =>
                sceneRole.CharacterRole.Assignments)
            .Where(assignment =>
                assignment.IsCurrent &&
                assignment.Status ==
                    RoleAssignmentStatus.Approved &&
                assignment.CastType ==
                    CastType.Main)
            .Select(assignment =>
                assignment.ActorId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<int>> GetActorIdsForActAsync(
        int actId)
    {
        return await _context.SceneRoles
            .AsNoTracking()
            .Where(sceneRole =>
                sceneRole.Scene.ActId == actId &&
                sceneRole.IsRequired)
            .SelectMany(sceneRole =>
                sceneRole.CharacterRole.Assignments)
            .Where(assignment =>
                assignment.IsCurrent &&
                assignment.Status ==
                    RoleAssignmentStatus.Approved &&
                assignment.CastType ==
                    CastType.Main)
            .Select(assignment =>
                assignment.ActorId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<int>> GetActorIdsForPerformanceAsync(
        int performanceId)
    {
        return await _context.RoleAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CharacterRole.PerformanceId ==
                    performanceId &&
                assignment.IsCurrent &&
                assignment.Status ==
                    RoleAssignmentStatus.Approved &&
                assignment.CastType ==
                    CastType.Main)
            .Select(assignment =>
                assignment.ActorId)
            .Distinct()
            .ToListAsync();
    }

    // =========================================================
    // ТЕКСТ ДЛЯ КАЛЕНДАРЯ
    // =========================================================

    private static string GetRehearsalHallText(
        Rehearsal rehearsal)
    {
        return rehearsal.Hall?.Venue == null
            ? rehearsal.Hall?.Name ?? "Зал не вказано"
            : $"{rehearsal.Hall.Venue.Name} — {rehearsal.Hall.Name}";
    }

    private static string GetRehearsalStatusText(
        RehearsalStatus status)
    {
        return status.ToString() switch
        {
            "Planned" => "Заплановано",
            "Confirmed" => "Підтверджено",
            "Done" => "Проведено",
            "Completed" => "Проведено",
            "Cancelled" => "Скасовано",
            _ => status.ToString()
        };
    }

    private static string GetShowTargetText(
        PerformanceShow show)
    {
        return show.Type switch
        {
            PerformanceShowType.Premiere => "Прем’єра",
            PerformanceShowType.Regular => "Звичайний показ",
            PerformanceShowType.Touring => "Виїзний показ",
            PerformanceShowType.Closed => "Закритий показ",
            PerformanceShowType.Charity => "Благодійний показ",
            PerformanceShowType.Other => "Інший показ",
            _ => "Показ вистави"
        };
    }

    private static string GetShowLocationText(
        PerformanceShow show)
    {
        if (show.Hall != null)
        {
            return show.Hall.Venue == null
                ? show.Hall.Name
                : $"{show.Hall.Venue.Name} — {show.Hall.Name}";
        }

        return string.IsNullOrWhiteSpace(show.ExternalLocation)
            ? "Локацію не вказано"
            : show.ExternalLocation;
    }

    private static string GetShowStatusText(
        PerformanceShowStatus status)
    {
        return status switch
        {
            PerformanceShowStatus.Planned => "Заплановано",
            PerformanceShowStatus.Confirmed => "Підтверджено",
            PerformanceShowStatus.Completed => "Проведено",
            PerformanceShowStatus.Cancelled => "Скасовано",
            PerformanceShowStatus.Postponed => "Перенесено",
            _ => status.ToString()
        };
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
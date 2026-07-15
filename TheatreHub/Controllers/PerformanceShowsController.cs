using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class PerformanceShowsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PerformanceShowsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PerformanceShows
    public async Task<IActionResult> Index(
        int? performanceId,
        PerformanceShowType? type,
        PerformanceShowStatus? status)
    {
        var query = _context.PerformanceShows
            .AsNoTracking()
            .Include(show => show.Performance)
            .Include(show => show.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .AsQueryable();

        if (performanceId.HasValue)
        {
            query = query.Where(show =>
                show.PerformanceId == performanceId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(show =>
                show.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(show =>
                show.Status == status.Value);
        }

        var shows = await query
            .OrderBy(show =>
                show.Status == PerformanceShowStatus.Completed ||
                show.Status == PerformanceShowStatus.Cancelled)
            .ThenBy(show =>
                show.StartDateTime)
            .ToListAsync();

        await LoadIndexDataAsync(
            performanceId,
            type,
            status);

        return View(shows);
    }

    // GET: PerformanceShows/Calendar
    public async Task<IActionResult> Calendar(
        DateTime? startDate,
        DateTime? endDate,
        int? performanceId,
        int? hallId,
        PerformanceShowType? type,
        PerformanceShowStatus? status)
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

        var query = _context.PerformanceShows
            .AsNoTracking()
            .Include(show => show.Performance)
            .Include(show => show.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Where(show =>
                show.StartDateTime < queryEnd &&
                show.EndDateTime >= queryStart);

        if (performanceId.HasValue)
        {
            query = query.Where(show =>
                show.PerformanceId == performanceId.Value);
        }

        if (hallId.HasValue)
        {
            query = query.Where(show =>
                show.HallId == hallId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(show =>
                show.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(show =>
                show.Status == status.Value);
        }

        var shows = await query
            .OrderBy(show =>
                show.StartDateTime)
            .ToListAsync();

        var showItems = shows
            .Select(show =>
                new PerformanceShowCalendarItemViewModel
                {
                    Id = show.Id,

                    PerformanceTitle =
                        show.Performance.Title,

                    TypeText =
                        GetTypeText(show.Type),

                    StatusText =
                        GetStatusText(show.Status),

                    StatusBadgeClass =
                        GetStatusBadgeClass(show.Status),

                    LocationText =
                        GetLocationText(show),

                    StartDateTime =
                        show.StartDateTime,

                    EndDateTime =
                        show.EndDateTime,

                    ExpectedAudienceCount =
                        show.ExpectedAudienceCount,

                    ActualAudienceCount =
                        show.ActualAudienceCount
                })
            .ToList();

        var days = new List<PerformanceShowCalendarDayViewModel>();

        for (var date = calendarStartDate;
             date <= calendarEndDate;
             date = date.AddDays(1))
        {
            var dayShows = showItems
                .Where(show =>
                    show.StartDateTime.Date == date)
                .OrderBy(show =>
                    show.StartDateTime)
                .ToList();

            days.Add(
                new PerformanceShowCalendarDayViewModel
                {
                    Date = date,
                    Shows = dayShows
                });
        }

        var model = new PerformanceShowCalendarViewModel
        {
            StartDate = calendarStartDate,
            EndDate = calendarEndDate,
            PerformanceId = performanceId,
            HallId = hallId,
            Type = type,
            Status = status,
            Days = days,

            Performances = await _context.Performances
                .AsNoTracking()
                .OrderBy(performance =>
                    performance.Title)
                .ToListAsync(),

            Halls = await _context.Halls
                .AsNoTracking()
                .Include(hall => hall.Venue!)
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

    // GET: PerformanceShows/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var show = await _context.PerformanceShows
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (show == null)
        {
            return NotFound();
        }

        return View(show);
    }

    // GET: PerformanceShows/Create
    public async Task<IActionResult> Create(
        int? performanceId,
        int? hallId)
    {
        var show = new PerformanceShow
        {
            PerformanceId = performanceId ?? 0,
            HallId = hallId,
            StartDateTime = DateTime.Today.AddHours(18),
            EndDateTime = DateTime.Today.AddHours(20),
            Type = PerformanceShowType.Regular,
            Status = PerformanceShowStatus.Planned
        };

        await LoadFormDataAsync(show);

        return View(show);
    }

    // POST: PerformanceShows/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "PerformanceId,HallId,ExternalLocation," +
            "StartDateTime,EndDateTime,Type,Status," +
            "ExpectedAudienceCount,ActualAudienceCount,Notes")]
        PerformanceShow show)
    {
        NormalizeShow(show);

        await ValidateShowAsync(show);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(show);

            return View(show);
        }

        show.CreatedAt = DateTime.Now;

        _context.PerformanceShows.Add(show);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Показ успішно створено.";

        return RedirectToAction(
            nameof(Details),
            new { id = show.Id });
    }

    // GET: PerformanceShows/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var show = await _context.PerformanceShows
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (show == null)
        {
            return NotFound();
        }

        await LoadFormDataAsync(show);

        return View(show);
    }

    // POST: PerformanceShows/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,PerformanceId,HallId,ExternalLocation," +
            "StartDateTime,EndDateTime,Type,Status," +
            "ExpectedAudienceCount,ActualAudienceCount,Notes")]
        PerformanceShow show)
    {
        if (id != show.Id)
        {
            return NotFound();
        }

        NormalizeShow(show);

        await ValidateShowAsync(
            show,
            excludedShowId: id);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(show);

            return View(show);
        }

        var existingShow = await _context.PerformanceShows
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (existingShow == null)
        {
            return NotFound();
        }

        existingShow.PerformanceId =
            show.PerformanceId;

        existingShow.HallId =
            show.HallId;

        existingShow.ExternalLocation =
            show.ExternalLocation;

        existingShow.StartDateTime =
            show.StartDateTime;

        existingShow.EndDateTime =
            show.EndDateTime;

        existingShow.Type =
            show.Type;

        existingShow.Status =
            show.Status;

        existingShow.ExpectedAudienceCount =
            show.ExpectedAudienceCount;

        existingShow.ActualAudienceCount =
            show.ActualAudienceCount;

        existingShow.Notes =
            show.Notes;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Показ успішно оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id = existingShow.Id });
    }

    // GET: PerformanceShows/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var show = await _context.PerformanceShows
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (show == null)
        {
            return NotFound();
        }

        return View(show);
    }

    // POST: PerformanceShows/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var show = await _context.PerformanceShows
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (show == null)
        {
            return NotFound();
        }

        _context.PerformanceShows.Remove(show);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Показ видалено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        PerformanceShowStatus status,
        string? returnUrl)
    {
        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var show = await _context.PerformanceShows
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (show == null)
        {
            return NotFound();
        }

        show.Status =
            status;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Статус показу оновлено.";

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    private async Task LoadIndexDataAsync(
        int? performanceId,
        PerformanceShowType? type,
        PerformanceShowStatus? status)
    {
        ViewBag.Performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .ToListAsync();

        ViewBag.PerformanceId =
            performanceId;

        ViewBag.Type =
            type;

        ViewBag.Status =
            status;
    }

    private async Task LoadFormDataAsync(
        PerformanceShow show)
    {
        var performanceOptions = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .Select(performance =>
                new SelectListItem
                {
                    Value = performance.Id.ToString(),
                    Text = performance.Title
                })
            .ToListAsync();

        performanceOptions.Insert(
            0,
            new SelectListItem
            {
                Value = "",
                Text = "Оберіть виставу"
            });

        ViewBag.PerformanceOptions =
            new SelectList(
                performanceOptions,
                "Value",
                "Text",
                show.PerformanceId > 0
                    ? show.PerformanceId.ToString()
                    : "");

        var hallOptions = await _context.Halls
            .AsNoTracking()
            .Include(hall => hall.Venue!)
            .OrderBy(hall =>
                hall.Venue!.Name)
            .ThenBy(hall =>
                hall.Name)
            .Select(hall =>
                new SelectListItem
                {
                    Value = hall.Id.ToString(),
                    Text = hall.Venue!.Name + " — " + hall.Name
                })
            .ToListAsync();

        hallOptions.Insert(
            0,
            new SelectListItem
            {
                Value = "",
                Text = "Без залу з бази / зовнішня локація"
            });

        ViewBag.HallOptions =
            new SelectList(
                hallOptions,
                "Value",
                "Text",
                show.HallId?.ToString());

        ViewBag.TypeOptions =
            Enum.GetValues<PerformanceShowType>()
                .Select(type =>
                    new SelectListItem
                    {
                        Value = type.ToString(),
                        Text = GetTypeText(type),
                        Selected = type == show.Type
                    })
                .ToList();

        ViewBag.StatusOptions =
            Enum.GetValues<PerformanceShowStatus>()
                .Select(status =>
                    new SelectListItem
                    {
                        Value = status.ToString(),
                        Text = GetStatusText(status),
                        Selected = status == show.Status
                    })
                .ToList();
    }

    private async Task ValidateShowAsync(
        PerformanceShow show,
        int? excludedShowId = null)
    {
        if (!Enum.IsDefined(show.Type))
        {
            ModelState.AddModelError(
                nameof(show.Type),
                "Оберіть правильний тип показу.");
        }

        if (!Enum.IsDefined(show.Status))
        {
            ModelState.AddModelError(
                nameof(show.Status),
                "Оберіть правильний статус показу.");
        }

        if (show.PerformanceId <= 0)
        {
            ModelState.AddModelError(
                nameof(show.PerformanceId),
                "Оберіть виставу.");

            return;
        }

        var performanceExists =
            await _context.Performances
                .AsNoTracking()
                .AnyAsync(performance =>
                    performance.Id == show.PerformanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(show.PerformanceId),
                "Обрана вистава не існує.");

            return;
        }

        if (show.EndDateTime <= show.StartDateTime)
        {
            ModelState.AddModelError(
                nameof(show.EndDateTime),
                "Час завершення має бути пізніше часу початку.");
        }

        if (!show.HallId.HasValue &&
            string.IsNullOrWhiteSpace(show.ExternalLocation))
        {
            ModelState.AddModelError(
                nameof(show.ExternalLocation),
                "Вкажіть зал або зовнішню локацію.");
        }

        if (show.HallId.HasValue)
        {
            var hallExists = await _context.Halls
                .AsNoTracking()
                .AnyAsync(hall =>
                    hall.Id == show.HallId.Value);

            if (!hallExists)
            {
                ModelState.AddModelError(
                    nameof(show.HallId),
                    "Обраний зал не існує.");
            }

            var hasShowConflict = await _context.PerformanceShows
                .AsNoTracking()
                .AnyAsync(existing =>
                    existing.HallId == show.HallId.Value &&
                    existing.Status != PerformanceShowStatus.Cancelled &&
                    existing.Status != PerformanceShowStatus.Postponed &&
                    (!excludedShowId.HasValue ||
                     existing.Id != excludedShowId.Value) &&
                    show.StartDateTime < existing.EndDateTime &&
                    show.EndDateTime > existing.StartDateTime);

            if (hasShowConflict)
            {
                ModelState.AddModelError(
                    nameof(show.HallId),
                    "У цей час у залі вже заплановано інший показ.");
            }

            var hasRehearsalConflict = await _context.Rehearsals
                .AsNoTracking()
                .AnyAsync(rehearsal =>
                    rehearsal.HallId == show.HallId.Value &&
                    rehearsal.Status.ToString() != "Cancelled" &&
                    show.StartDateTime < rehearsal.EndDateTime &&
                    show.EndDateTime > rehearsal.StartDateTime);

            if (hasRehearsalConflict)
            {
                ModelState.AddModelError(
                    nameof(show.HallId),
                    "У цей час у залі вже запланована репетиція.");
            }
        }
    }

    private static void NormalizeShow(
        PerformanceShow show)
    {
        show.ExternalLocation =
            NormalizeOptionalText(show.ExternalLocation);

        show.Notes =
            NormalizeOptionalText(show.Notes);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetTypeText(
        PerformanceShowType type)
    {
        return type switch
        {
            PerformanceShowType.Premiere => "Прем’єра",
            PerformanceShowType.Regular => "Звичайний показ",
            PerformanceShowType.Touring => "Виїзний показ",
            PerformanceShowType.Closed => "Закритий показ",
            PerformanceShowType.Charity => "Благодійний показ",
            PerformanceShowType.Other => "Інше",
            _ => type.ToString()
        };
    }

    private static string GetStatusText(
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

    private static string GetStatusBadgeClass(
        PerformanceShowStatus status)
    {
        return status switch
        {
            PerformanceShowStatus.Planned => "text-bg-secondary",
            PerformanceShowStatus.Confirmed => "text-bg-primary",
            PerformanceShowStatus.Completed => "text-bg-success",
            PerformanceShowStatus.Cancelled => "text-bg-dark",
            PerformanceShowStatus.Postponed => "text-bg-warning",
            _ => "text-bg-secondary"
        };
    }

    private static string GetLocationText(
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
}
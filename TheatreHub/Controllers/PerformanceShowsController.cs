using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class PerformanceShowsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

  public PerformanceShowsController(
    ApplicationDbContext context,
    IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
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
                .ThenInclude(hall => hall!.Venue)
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
                .ThenInclude(hall => hall!.Venue)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (show == null)
        {
            return NotFound();
        }

        var budgetTransactions = await _context.BudgetTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.PerformanceShowId == show.Id &&
                transaction.Status != BudgetTransactionStatus.Cancelled)
            .ToListAsync();

        ViewBag.PlannedIncomeTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Income)
                .Sum(transaction =>
                    transaction.PlannedAmount);

        ViewBag.PlannedExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.PlannedAmount);

        ViewBag.PlannedProfit =
            ViewBag.PlannedIncomeTotal - ViewBag.PlannedExpenseTotal;

        ViewBag.ActualIncomeTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Income)
                .Sum(transaction =>
                    transaction.ActualAmount ?? 0);

        ViewBag.ActualExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.ActualAmount ?? 0);

        ViewBag.ActualProfit =
            ViewBag.ActualIncomeTotal - ViewBag.ActualExpenseTotal;

        ViewBag.BudgetTransactionsCount =
            budgetTransactions.Count;

        return View(show);
    }

    // GET: PerformanceShows/Create
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == show.PerformanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "PerformanceShow",
            show.Id,
            performanceTitle,
            $"Створено показ вистави «{performanceTitle}» на {show.StartDateTime:dd.MM.yyyy HH:mm}.");

        TempData["SuccessMessage"] =
            "Показ успішно створено.";

        return RedirectToAction(
            nameof(Details),
            new { id = show.Id });
    }

    // GET: PerformanceShows/Edit/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

        existingShow.PerformanceId = show.PerformanceId;
        existingShow.HallId = show.HallId;
        existingShow.ExternalLocation = show.ExternalLocation;
        existingShow.StartDateTime = show.StartDateTime;
        existingShow.EndDateTime = show.EndDateTime;
        existingShow.Type = show.Type;
        existingShow.Status = show.Status;
        existingShow.ExpectedAudienceCount = show.ExpectedAudienceCount;
        existingShow.ActualAudienceCount = show.ActualAudienceCount;
        existingShow.Notes = show.Notes;

        await _context.SaveChangesAsync();
        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == existingShow.PerformanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "PerformanceShow",
            existingShow.Id,
            performanceTitle,
            $"Оновлено показ вистави «{performanceTitle}» на {existingShow.StartDateTime:dd.MM.yyyy HH:mm}.");

        TempData["SuccessMessage"] =
            "Показ успішно оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id = existingShow.Id });
    }

    // GET: PerformanceShows/Delete/5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
                .ThenInclude(hall => hall!.Venue)
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var show = await _context.PerformanceShows
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (show == null)
        {
            return NotFound();
        }

        _context.PerformanceShows.Remove(show);

        var performanceTitle = show.Performance.Title;
        var showDateTime = show.StartDateTime;

        await _context.SaveChangesAsync();
        await _actionLogService.LogAsync(
            User,
            "Delete",
            "PerformanceShow",
            id,
            performanceTitle,
            $"Видалено показ вистави «{performanceTitle}» на {showDateTime:dd.MM.yyyy HH:mm}.");

        TempData["SuccessMessage"] =
            "Показ видалено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (show == null)
        {
            return NotFound();
        }

        var oldStatus = show.Status;

        show.Status = status;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "ChangeStatus",
            "PerformanceShow",
            show.Id,
            show.Performance.Title,
            $"Статус показу вистави «{show.Performance.Title}» змінено з {oldStatus} на {status}.");

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

        ViewBag.PerformanceId = performanceId;
        ViewBag.Type = type;
        ViewBag.Status = status;
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

        var halls = await _context.Halls
            .AsNoTracking()
            .Include(hall => hall.Venue)
            .Where(hall =>
                hall.IsActive &&
                hall.Venue!.IsActive)
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

        halls.Insert(
            0,
            new SelectListItem
            {
                Value = "",
                Text = "Без залу з бази / зовнішня локація"
            });

        ViewBag.HallOptions =
            new SelectList(
                halls,
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
}
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
public class ProductionItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public ProductionItemsController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: ProductionItems
    public async Task<IActionResult> Index(
        int? performanceId,
        ProductionItemType? type,
        ProductionItemStatus? status)
    {
        var query = _context.ProductionItems
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Act)
            .Include(item => item.Scene)
            .AsQueryable();

        if (performanceId.HasValue)
        {
            query = query.Where(item =>
                item.PerformanceId == performanceId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(item =>
                item.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(item =>
                item.Status == status.Value);
        }

        var items = await query
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

        await LoadIndexDataAsync(
            performanceId,
            type,
            status);

        return View(items);
    }

    // GET: ProductionItems/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.ProductionItems
            .AsNoTracking()
            .Include(x => x.Performance)
            .Include(x => x.Act)
            .Include(x => x.Scene)
            .FirstOrDefaultAsync(x =>
                x.Id == id.Value);

        if (item == null)
        {
            return NotFound();
        }

        var budgetTransactions = await _context.BudgetTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.ProductionItemId == item.Id &&
                transaction.Status != BudgetTransactionStatus.Cancelled)
            .ToListAsync();

        ViewBag.PlannedExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.PlannedAmount);

        ViewBag.ActualExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.ActualAmount ?? 0);

        ViewBag.BudgetTransactionsCount =
            budgetTransactions.Count;

        return View(item);
    }

    // GET: ProductionItems/Create
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> Create(
        int? performanceId,
        int? actId,
        int? sceneId)
    {
        var item = new ProductionItem
        {
            Quantity = 1,
            Type = ProductionItemType.Prop,
            Status = ProductionItemStatus.Needed
        };

        if (sceneId.HasValue)
        {
            var scene = await _context.Scenes
                .AsNoTracking()
                .Include(x => x.Act)
                .FirstOrDefaultAsync(x =>
                    x.Id == sceneId.Value);

            if (scene == null)
            {
                return NotFound();
            }

            item.PerformanceId =
                scene.Act.PerformanceId;

            item.ActId =
                scene.ActId;

            item.SceneId =
                scene.Id;
        }
        else if (actId.HasValue)
        {
            var act = await _context.Acts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == actId.Value);

            if (act == null)
            {
                return NotFound();
            }

            item.PerformanceId =
                act.PerformanceId;

            item.ActId =
                act.Id;
        }
        else if (performanceId.HasValue)
        {
            var performanceExists =
                await _context.Performances
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Id == performanceId.Value);

            if (!performanceExists)
            {
                return NotFound();
            }

            item.PerformanceId =
                performanceId.Value;
        }

        await LoadFormDataAsync(item);

        return View(item);
    }

    // POST: ProductionItems/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> Create(
        [Bind(
            "Name,Description,Type,Status,Quantity," +
            "ResponsibleName,StorageLocation,NeededBy,Notes," +
            "PerformanceId,ActId,SceneId")]
        ProductionItem item)
    {
        NormalizeItem(item);

        await ValidateItemAsync(item);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(item);

            return View(item);
        }

        item.CreatedAt = DateTime.Now;

        _context.ProductionItems.Add(item);
        await _context.SaveChangesAsync();

        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == item.PerformanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "ProductionItem",
            item.Id,
            item.Name,
            $"Створено постановочний елемент «{item.Name}» для вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Елемент «{item.Name}» створено.";

        return RedirectToAction(
            nameof(Details),
            new { id = item.Id });
    }

    // GET: ProductionItems/Edit/5
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.ProductionItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id.Value);

        if (item == null)
        {
            return NotFound();
        }

        await LoadFormDataAsync(item);

        return View(item);
    }

    // POST: ProductionItems/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,Name,Description,Type,Status,Quantity," +
            "ResponsibleName,StorageLocation,NeededBy,Notes," +
            "PerformanceId,ActId,SceneId")]
        ProductionItem item)
    {
        if (id != item.Id)
        {
            return NotFound();
        }

        NormalizeItem(item);

        await ValidateItemAsync(item);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(item);

            return View(item);
        }

        var existingItem = await _context.ProductionItems
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (existingItem == null)
        {
            return NotFound();
        }

        existingItem.Name = item.Name;
        existingItem.Description = item.Description;
        existingItem.Type = item.Type;
        existingItem.Status = item.Status;
        existingItem.Quantity = item.Quantity;
        existingItem.ResponsibleName = item.ResponsibleName;
        existingItem.StorageLocation = item.StorageLocation;
        existingItem.NeededBy = item.NeededBy;
        existingItem.Notes = item.Notes;
        existingItem.PerformanceId = item.PerformanceId;
        existingItem.ActId = item.ActId;
        existingItem.SceneId = item.SceneId;

        await _context.SaveChangesAsync();

        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == existingItem.PerformanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "ProductionItem",
            existingItem.Id,
            existingItem.Name,
            $"Оновлено постановочний елемент «{existingItem.Name}» для вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Елемент «{existingItem.Name}» оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    // GET: ProductionItems/Delete/5
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.ProductionItems
            .AsNoTracking()
            .Include(x => x.Performance)
            .Include(x => x.Act)
            .Include(x => x.Scene)
            .FirstOrDefaultAsync(x =>
                x.Id == id.Value);

        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    // POST: ProductionItems/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.ProductionItems
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        var name = item.Name;
        var performanceId = item.PerformanceId;

        _context.ProductionItems.Remove(item);
        await _context.SaveChangesAsync();

        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == performanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "ProductionItem",
            id,
            name,
            $"Видалено постановочний елемент «{name}» з вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Елемент «{name}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    // ChangeStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProduction)]
    public async Task<IActionResult> ChangeStatus(
        int id,
        ProductionItemStatus status,
        string? returnUrl)
    {
        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var item = await _context.ProductionItems
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        var oldStatus = item.Status;

        item.Status = status;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "ChangeStatus",
            "ProductionItem",
            item.Id,
            item.Name,
            $"Статус постановочного елемента «{item.Name}» змінено з {oldStatus} на {status}.");

        TempData["SuccessMessage"] =
            $"Статус елемента «{item.Name}» оновлено.";

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
        ProductionItemType? type,
        ProductionItemStatus? status)
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
        ProductionItem item)
    {
        var performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .Select(performance =>
                new SelectListItem
                {
                    Value = performance.Id.ToString(),
                    Text = performance.Title,
                    Selected =
                        performance.Id == item.PerformanceId
                })
            .ToListAsync();

        var acts = await _context.Acts
            .AsNoTracking()
            .Include(act => act.Performance)
            .OrderBy(act =>
                act.Performance.Title)
            .ThenBy(act =>
                act.Position)
            .ThenBy(act =>
                act.Number)
            .Select(act =>
                new SelectListItem
                {
                    Value = act.Id.ToString(),
                    Text =
                        act.Performance.Title +
                        " — " +
                        act.DisplayName,
                    Selected =
                        item.ActId.HasValue &&
                        act.Id == item.ActId.Value
                })
            .ToListAsync();

        var scenes = await _context.Scenes
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
            .Select(scene =>
                new SelectListItem
                {
                    Value = scene.Id.ToString(),
                    Text =
                        scene.Act.Performance.Title +
                        " — " +
                        scene.Act.DisplayName +
                        " — Сцена " +
                        scene.Number +
                        ". " +
                        scene.Title,
                    Selected =
                        item.SceneId.HasValue &&
                        scene.Id == item.SceneId.Value
                })
            .ToListAsync();

        acts.Insert(
            0,
            new SelectListItem
            {
                Value = "",
                Text = "Без прив’язки до дії"
            });

        scenes.Insert(
            0,
            new SelectListItem
            {
                Value = "",
                Text = "Без прив’язки до сцени"
            });

        ViewBag.PerformanceOptions = performances;
        ViewBag.ActOptions = acts;
        ViewBag.SceneOptions = scenes;

        ViewBag.TypeOptions =
            Enum.GetValues<ProductionItemType>()
                .Select(type =>
                    new SelectListItem
                    {
                        Value = type.ToString(),
                        Text = GetTypeText(type),
                        Selected = type == item.Type
                    })
                .ToList();

        ViewBag.StatusOptions =
            Enum.GetValues<ProductionItemStatus>()
                .Select(status =>
                    new SelectListItem
                    {
                        Value = status.ToString(),
                        Text = GetStatusText(status),
                        Selected = status == item.Status
                    })
                .ToList();
    }

    private async Task ValidateItemAsync(
        ProductionItem item)
    {
        if (!Enum.IsDefined(item.Type))
        {
            ModelState.AddModelError(
                nameof(item.Type),
                "Оберіть правильний тип.");
        }

        if (!Enum.IsDefined(item.Status))
        {
            ModelState.AddModelError(
                nameof(item.Status),
                "Оберіть правильний статус.");
        }

        if (item.PerformanceId <= 0)
        {
            ModelState.AddModelError(
                nameof(item.PerformanceId),
                "Оберіть виставу.");

            return;
        }

        var performanceExists = await _context.Performances
            .AnyAsync(performance =>
                performance.Id == item.PerformanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(item.PerformanceId),
                "Обрана вистава не існує.");

            return;
        }

        if (item.ActId.HasValue)
        {
            var act = await _context.Acts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == item.ActId.Value);

            if (act == null)
            {
                ModelState.AddModelError(
                    nameof(item.ActId),
                    "Обрана дія не існує.");
            }
            else if (act.PerformanceId != item.PerformanceId)
            {
                ModelState.AddModelError(
                    nameof(item.ActId),
                    "Обрана дія не належить вибраній виставі.");
            }
        }

        if (item.SceneId.HasValue)
        {
            var scene = await _context.Scenes
                .AsNoTracking()
                .Include(x => x.Act)
                .FirstOrDefaultAsync(x =>
                    x.Id == item.SceneId.Value);

            if (scene == null)
            {
                ModelState.AddModelError(
                    nameof(item.SceneId),
                    "Обрана сцена не існує.");
            }
            else
            {
                if (scene.Act.PerformanceId != item.PerformanceId)
                {
                    ModelState.AddModelError(
                        nameof(item.SceneId),
                        "Обрана сцена не належить вибраній виставі.");
                }

                if (item.ActId.HasValue &&
                    scene.ActId != item.ActId.Value)
                {
                    ModelState.AddModelError(
                        nameof(item.SceneId),
                        "Обрана сцена не належить вибраній дії.");
                }

                if (!item.ActId.HasValue)
                {
                    item.ActId = scene.ActId;
                }
            }
        }
    }

    private static void NormalizeItem(
        ProductionItem item)
    {
        item.Name =
            item.Name?.Trim() ?? string.Empty;

        item.Description =
            NormalizeOptionalText(item.Description);

        item.ResponsibleName =
            NormalizeOptionalText(item.ResponsibleName);

        item.StorageLocation =
            NormalizeOptionalText(item.StorageLocation);

        item.Notes =
            NormalizeOptionalText(item.Notes);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetTypeText(
        ProductionItemType type)
    {
        return type switch
        {
            ProductionItemType.Prop => "Реквізит",
            ProductionItemType.Costume => "Костюм",
            ProductionItemType.Makeup => "Грим",
            ProductionItemType.Decoration => "Декорація",
            ProductionItemType.Equipment => "Обладнання",
            ProductionItemType.Other => "Інше",
            _ => type.ToString()
        };
    }

    private static string GetStatusText(
        ProductionItemStatus status)
    {
        return status switch
        {
            ProductionItemStatus.Needed => "Потрібно",
            ProductionItemStatus.InProgress => "У роботі",
            ProductionItemStatus.Ready => "Готово",
            ProductionItemStatus.Missing => "Проблема",
            ProductionItemStatus.Cancelled => "Скасовано",
            _ => status.ToString()
        };
    }
}
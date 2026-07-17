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
public class TheatreTasksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public TheatreTasksController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    // GET: TheatreTasks
    public async Task<IActionResult> Index(
        int? performanceId,
        TheatreTaskStatus? status,
        TheatreTaskPriority? priority)
    {
        var query = _context.TheatreTasks
            .AsNoTracking()
            .Include(task => task.Performance)
            .Include(task => task.Act)
            .Include(task => task.Scene)
            .AsQueryable();

        if (performanceId.HasValue)
        {
            query = query.Where(task =>
                task.PerformanceId == performanceId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(task =>
                task.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(task =>
                task.Priority == priority.Value);
        }

        var tasks = await query
            .OrderBy(task =>
                task.Status == TheatreTaskStatus.Done ||
                task.Status == TheatreTaskStatus.Cancelled)
            .ThenBy(task =>
                task.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(task =>
                task.Priority)
            .ThenBy(task =>
                task.Title)
            .ToListAsync();

        await LoadIndexDataAsync(
            performanceId,
            status,
            priority);

        return View(tasks);
    }

    // GET: TheatreTasks/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var task = await _context.TheatreTasks
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Act)
            .Include(item => item.Scene)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (task == null)
        {
            return NotFound();
        }

        ViewBag.Comments = await _context.TheatreTaskComments
    .AsNoTracking()
    .Where(comment =>
        comment.TheatreTaskId == task.Id)
    .OrderByDescending(comment =>
        comment.CreatedAt)
    .ToListAsync();

        return View(task);
    }

    // GET: TheatreTasks/Create
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> Create(
        int? performanceId,
        int? actId,
        int? sceneId)
    {
        var task = new TheatreTask
        {
            Status = TheatreTaskStatus.Planned,
            Priority = TheatreTaskPriority.Normal
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

            task.PerformanceId =
                scene.Act.PerformanceId;

            task.ActId =
                scene.ActId;

            task.SceneId =
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

            task.PerformanceId =
                act.PerformanceId;

            task.ActId =
                act.Id;
        }
        else if (performanceId.HasValue)
        {
            var performanceExists = await _context.Performances
                .AsNoTracking()
                .AnyAsync(item =>
                    item.Id == performanceId.Value);

            if (!performanceExists)
            {
                return NotFound();
            }

            task.PerformanceId =
                performanceId.Value;
        }

        await LoadFormDataAsync(task);

        return View(task);
    }

    // POST: TheatreTasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> Create(
        [Bind(
            "Title,Description,ResponsibleName,Deadline," +
            "Status,Priority,PerformanceId,ActId,SceneId")]
        TheatreTask task)
    {
        NormalizeTask(task);

        await ValidateTaskAsync(task);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(task);

            return View(task);
        }

        task.CreatedAt = DateTime.Now;

        _context.TheatreTasks.Add(task);
        await _context.SaveChangesAsync();

        var performanceTitle = await _context.Performances
            .Where(performance => performance.Id == task.PerformanceId)
            .Select(performance => performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "TheatreTask",
            task.Id,
            task.Title,
            $"Створено завдання «{task.Title}» для вистави «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Завдання «{task.Title}» створено.";

        return RedirectToAction(nameof(Details), new { id = task.Id });
    }

    // GET: TheatreTasks/Edit/5
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var task = await _context.TheatreTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (task == null)
        {
            return NotFound();
        }

        await LoadFormDataAsync(task);

        return View(task);
    }

    // POST: TheatreTasks/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,Title,Description,ResponsibleName,Deadline," +
            "Status,Priority,PerformanceId,ActId,SceneId")]
        TheatreTask task)
    {
        if (id != task.Id)
        {
            return NotFound();
        }

        NormalizeTask(task);

        await ValidateTaskAsync(task);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(task);

            return View(task);
        }

        var existingTask = await _context.TheatreTasks
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (existingTask == null)
        {
            return NotFound();
        }

        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.ResponsibleName = task.ResponsibleName;
        existingTask.Deadline = task.Deadline;
        existingTask.Status = task.Status;
        existingTask.Priority = task.Priority;
        existingTask.PerformanceId = task.PerformanceId;
        existingTask.ActId = task.ActId;
        existingTask.SceneId = task.SceneId;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "TheatreTask",
            existingTask.Id,
            existingTask.Title,
            $"Оновлено завдання «{existingTask.Title}».");

        TempData["SuccessMessage"] =
            $"Завдання «{existingTask.Title}» оновлено.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: TheatreTasks/Delete/5
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var task = await _context.TheatreTasks
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Act)
            .Include(item => item.Scene)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    // POST: TheatreTasks/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _context.TheatreTasks
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (task == null)
        {
            return NotFound();
        }

        var title = task.Title;

        _context.TheatreTasks.Remove(task);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "TheatreTask",
            id,
            title,
            $"Видалено завдання «{title}».");

        TempData["SuccessMessage"] =
            $"Завдання «{title}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    // ChangeStatus

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> ChangeStatus(
    int id,
    TheatreTaskStatus status,
    string? returnUrl)
    {
        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var task = await _context.TheatreTasks
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (task == null)
        {
            return NotFound();
        }

        var oldStatus = task.Status;

        task.Status = status;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "ChangeStatus",
            "TheatreTask",
            task.Id,
            task.Title,
            $"Статус завдання «{task.Title}» змінено з {oldStatus} на {status}.");

        TempData["SuccessMessage"] =
            $"Статус завдання «{task.Title}» оновлено.";

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    //коментарі
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> AddComment(
    int theatreTaskId,
    string commentText,
    string? authorName,
    string? returnUrl)
    {
        var task = await _context.TheatreTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == theatreTaskId);

        if (task == null)
        {
            return NotFound();
        }

        commentText =
            commentText?.Trim() ?? string.Empty;

        authorName =
            string.IsNullOrWhiteSpace(authorName)
                ? null
                : authorName.Trim();

        if (string.IsNullOrWhiteSpace(commentText))
        {
            TempData["ErrorMessage"] =
                "Коментар не може бути порожнім.";

            return RedirectToAction(
                nameof(Details),
                new { id = theatreTaskId });
        }

        var comment = new TheatreTaskComment
        {
            TheatreTaskId = theatreTaskId,
            AuthorName = authorName,
            Text = commentText,
            CreatedAt = DateTime.Now
        };

        _context.TheatreTaskComments.Add(comment);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "AddComment",
            "TheatreTaskComment",
            comment.Id,
            task.Title,
            $"Додано коментар до завдання «{task.Title}».");

        TempData["SuccessMessage"] =
            "Коментар додано.";

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(
            nameof(Details),
            new { id = theatreTaskId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageTasks)]
    public async Task<IActionResult> DeleteComment(
    int id,
    string? returnUrl)
    {
        var comment = await _context.TheatreTaskComments
            .Include(item => item.TheatreTask)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (comment == null)
        {
            return NotFound();
        }

        var taskId = comment.TheatreTaskId;
        var taskTitle = comment.TheatreTask?.Title ?? "Завдання";

        _context.TheatreTaskComments.Remove(comment);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "DeleteComment",
            "TheatreTaskComment",
            id,
            taskTitle,
            $"Видалено коментар із завдання «{taskTitle}».");

        TempData["SuccessMessage"] =
            "Коментар видалено.";

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(
            nameof(Details),
            new { id = taskId });
    }
    private async Task LoadIndexDataAsync(
        int? performanceId,
        TheatreTaskStatus? status,
        TheatreTaskPriority? priority)
    {
        ViewBag.Performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .ToListAsync();

        ViewBag.PerformanceId = performanceId;
        ViewBag.Status = status;
        ViewBag.Priority = priority;
    }

    private async Task LoadFormDataAsync(
        TheatreTask task)
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
                        performance.Id == task.PerformanceId
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
                        task.ActId.HasValue &&
                        act.Id == task.ActId.Value
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
                        task.SceneId.HasValue &&
                        scene.Id == task.SceneId.Value
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

        ViewBag.StatusOptions =
            Enum.GetValues<TheatreTaskStatus>()
                .Select(status =>
                    new SelectListItem
                    {
                        Value = status.ToString(),
                        Text = GetStatusText(status),
                        Selected = status == task.Status
                    })
                .ToList();

        ViewBag.PriorityOptions =
            Enum.GetValues<TheatreTaskPriority>()
                .Select(priority =>
                    new SelectListItem
                    {
                        Value = priority.ToString(),
                        Text = GetPriorityText(priority),
                        Selected = priority == task.Priority
                    })
                .ToList();
    }

    private async Task ValidateTaskAsync(
        TheatreTask task)
    {
        if (!Enum.IsDefined(task.Status))
        {
            ModelState.AddModelError(
                nameof(task.Status),
                "Оберіть правильний статус завдання.");
        }

        if (!Enum.IsDefined(task.Priority))
        {
            ModelState.AddModelError(
                nameof(task.Priority),
                "Оберіть правильний пріоритет завдання.");
        }

        if (task.PerformanceId <= 0)
        {
            ModelState.AddModelError(
                nameof(task.PerformanceId),
                "Оберіть виставу.");

            return;
        }

        var performanceExists = await _context.Performances
            .AnyAsync(performance =>
                performance.Id == task.PerformanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(task.PerformanceId),
                "Обрана вистава не існує.");

            return;
        }

        if (task.ActId.HasValue)
        {
            var act = await _context.Acts
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == task.ActId.Value);

            if (act == null)
            {
                ModelState.AddModelError(
                    nameof(task.ActId),
                    "Обрана дія не існує.");
            }
            else if (act.PerformanceId != task.PerformanceId)
            {
                ModelState.AddModelError(
                    nameof(task.ActId),
                    "Обрана дія не належить вибраній виставі.");
            }
        }

        if (task.SceneId.HasValue)
        {
            var scene = await _context.Scenes
                .AsNoTracking()
                .Include(item => item.Act)
                .FirstOrDefaultAsync(item =>
                    item.Id == task.SceneId.Value);

            if (scene == null)
            {
                ModelState.AddModelError(
                    nameof(task.SceneId),
                    "Обрана сцена не існує.");
            }
            else
            {
                if (scene.Act.PerformanceId != task.PerformanceId)
                {
                    ModelState.AddModelError(
                        nameof(task.SceneId),
                        "Обрана сцена не належить вибраній виставі.");
                }

                if (task.ActId.HasValue &&
                    scene.ActId != task.ActId.Value)
                {
                    ModelState.AddModelError(
                        nameof(task.SceneId),
                        "Обрана сцена не належить вибраній дії.");
                }

                if (!task.ActId.HasValue)
                {
                    task.ActId = scene.ActId;
                }
            }
        }
    }

    private static void NormalizeTask(
        TheatreTask task)
    {
        task.Title =
            task.Title?.Trim() ?? string.Empty;

        task.Description =
            NormalizeOptionalText(task.Description);

        task.ResponsibleName =
            NormalizeOptionalText(task.ResponsibleName);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetStatusText(
        TheatreTaskStatus status)
    {
        return status switch
        {
            TheatreTaskStatus.Planned => "Заплановано",
            TheatreTaskStatus.InProgress => "У роботі",
            TheatreTaskStatus.Done => "Виконано",
            TheatreTaskStatus.Cancelled => "Скасовано",
            _ => status.ToString()
        };
    }

    private static string GetPriorityText(
        TheatreTaskPriority priority)
    {
        return priority switch
        {
            TheatreTaskPriority.Low => "Низький",
            TheatreTaskPriority.Normal => "Звичайний",
            TheatreTaskPriority.High => "Високий",
            TheatreTaskPriority.Critical => "Критичний",
            _ => priority.ToString()
        };
    }
}
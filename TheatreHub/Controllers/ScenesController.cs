using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class ScenesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ScenesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Scenes?actId=5
    public async Task<IActionResult> Index(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act == null)
        {
            return NotFound();
        }

        var scenes = await _context.Scenes
            .AsNoTracking()
            .Where(scene => scene.ActId == actId)
            .OrderBy(scene => scene.Position)
            .ThenBy(scene => scene.Number)
            .ToListAsync();

        ViewBag.ActId = act.Id;
        ViewBag.ActName = act.DisplayName;
        ViewBag.PerformanceId = act.PerformanceId;
        ViewBag.PerformanceTitle = act.Performance.Title;

        return View(scenes);
    }

    // GET: Scenes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        return View(scene);
    }

    // GET: Scenes/Create?actId=5
    public async Task<IActionResult> Create(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act == null)
        {
            return NotFound();
        }

        var nextNumber = await _context.Scenes
            .Where(scene => scene.ActId == actId)
            .Select(scene => (int?)scene.Number)
            .MaxAsync() ?? 0;

        var nextPosition = await _context.Scenes
            .Where(scene => scene.ActId == actId)
            .Select(scene => (int?)scene.Position)
            .MaxAsync() ?? 0;

        var model = new Scene
        {
            ActId = actId,
            Number = nextNumber + 1,
            Position = nextPosition + 1
        };

        SetActHeader(act);

        return View(model);
    }

    // POST: Scenes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "ActId,Number,Title,Synopsis," +
            "DurationMinutes,Position")]
        Scene scene)
    {
        NormalizeScene(scene);

        await ValidateSceneAsync(scene);

        if (!ModelState.IsValid)
        {
            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        _context.Scenes.Add(scene);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");

            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        TempData["SuccessMessage"] =
            $"Сцену «{scene.Title}» успішно створено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = scene.ActId });
    }

    // GET: Scenes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        SetActHeader(scene.Act);

        return View(scene);
    }

    // POST: Scenes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,ActId,Number,Title,Synopsis," +
            "DurationMinutes,Position")]
        Scene scene)
    {
        if (id != scene.Id)
        {
            return NotFound();
        }

        NormalizeScene(scene);

        await ValidateSceneAsync(
            scene,
            excludedSceneId: id);

        if (!ModelState.IsValid)
        {
            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        var existingScene = await _context.Scenes
            .FirstOrDefaultAsync(item => item.Id == id);

        if (existingScene == null)
        {
            return NotFound();
        }

        // Дію через форму редагування не змінюємо.
        if (existingScene.ActId != scene.ActId)
        {
            return BadRequest();
        }

        existingScene.Number = scene.Number;
        existingScene.Title = scene.Title;
        existingScene.Synopsis = scene.Synopsis;
        existingScene.DurationMinutes = scene.DurationMinutes;
        existingScene.Position = scene.Position;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");

            await LoadActHeaderAsync(scene.ActId);

            return View(scene);
        }

        TempData["SuccessMessage"] =
            $"Сцену «{existingScene.Title}» успішно оновлено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = existingScene.ActId });
    }

    // GET: Scenes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Act)
                .ThenInclude(act => act.Performance)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (scene == null)
        {
            return NotFound();
        }

        return View(scene);
    }

    // POST: Scenes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var scene = await _context.Scenes
            .FirstOrDefaultAsync(item => item.Id == id);

        if (scene == null)
        {
            return NotFound();
        }

        var actId = scene.ActId;
        var sceneTitle = scene.Title;

        _context.Scenes.Remove(scene);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Сцену «{sceneTitle}» видалено.";

        return RedirectToAction(
            "Details",
            "Acts",
            new { id = actId });
    }

    private async Task ValidateSceneAsync(
        Scene scene,
        int? excludedSceneId = null)
    {
        var actExists = await _context.Acts
            .AnyAsync(act => act.Id == scene.ActId);

        if (!actExists)
        {
            ModelState.AddModelError(
                nameof(scene.ActId),
                "Обрана дія не існує.");

            return;
        }

        if (scene.Number <= 0)
        {
            return;
        }

        var duplicateExists = await _context.Scenes
            .AnyAsync(existing =>
                existing.ActId == scene.ActId &&
                existing.Number == scene.Number &&
                (!excludedSceneId.HasValue ||
                 existing.Id != excludedSceneId.Value));

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(scene.Number),
                "У цій дії вже існує сцена з таким номером.");
        }
    }

    private async Task LoadActHeaderAsync(int actId)
    {
        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item => item.Id == actId);

        if (act != null)
        {
            SetActHeader(act);
        }
    }

    private void SetActHeader(Act act)
    {
        ViewBag.ActName = act.DisplayName;
        ViewBag.PerformanceId = act.PerformanceId;
        ViewBag.PerformanceTitle = act.Performance.Title;
    }

    private static void NormalizeScene(Scene scene)
    {
        scene.Title = scene.Title?.Trim() ?? string.Empty;
        scene.Synopsis = NormalizeOptionalText(scene.Synopsis);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
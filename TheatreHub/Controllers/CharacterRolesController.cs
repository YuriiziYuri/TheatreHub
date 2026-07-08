using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class CharacterRolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CharacterRolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CharacterRoles
    public async Task<IActionResult> Index()
    {
        var characterRoles = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Actor)
            .Include(role => role.Performance)
            .OrderBy(role => role.Performance.Title)
            .ThenBy(role => role.Name)
            .ToListAsync();

        return View(characterRoles);
    }

    // GET: CharacterRoles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole = await _context.CharacterRoles
            .Include(role => role.Performance)
            .Include(role => role.Actor)
            .FirstOrDefaultAsync(role => role.Id == id);

        if (characterRole == null)
        {
            return NotFound();
        }

        return View(characterRole);
    }

    // GET: CharacterRoles/Create
    public IActionResult Create()
    {
        PrepareSelectLists();

        return View();
    }

    // POST: CharacterRoles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,IsMainRole,PerformanceId,ActorId")]
        CharacterRole characterRole)
    {
        if (ModelState.IsValid)
        {
            _context.CharacterRoles.Add(characterRole);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        PrepareSelectLists(
            characterRole.PerformanceId,
            characterRole.ActorId);

        return View(characterRole);
    }

    // GET: CharacterRoles/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole =
            await _context.CharacterRoles.FindAsync(id);

        if (characterRole == null)
        {
            return NotFound();
        }

        PrepareSelectLists(
            characterRole.PerformanceId,
            characterRole.ActorId);

        return View(characterRole);
    }

    // POST: CharacterRoles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,IsMainRole,PerformanceId,ActorId")]
        CharacterRole characterRole)
    {
        if (id != characterRole.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PrepareSelectLists(
                characterRole.PerformanceId,
                characterRole.ActorId);

            return View(characterRole);
        }

        try
        {
            _context.CharacterRoles.Update(characterRole);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CharacterRoleExists(characterRole.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: CharacterRoles/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var characterRole = await _context.CharacterRoles
            .AsNoTracking()
            .Include(role => role.Actor)
            .Include(role => role.Performance)
            .FirstOrDefaultAsync(role => role.Id == id);

        if (characterRole == null)
        {
            return NotFound();
        }

        return View(characterRole);
    }

    // POST: CharacterRoles/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var characterRole =
            await _context.CharacterRoles.FindAsync(id);

        if (characterRole != null)
        {
            _context.CharacterRoles.Remove(characterRole);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private void PrepareSelectLists(
        int? selectedPerformanceId = null,
        int? selectedActorId = null)
    {
        ViewData["PerformanceId"] = new SelectList(
            _context.Performances
                .AsNoTracking()
                .OrderBy(performance => performance.Title),
            "Id",
            "Title",
            selectedPerformanceId);

        ViewData["ActorId"] = new SelectList(
            _context.Actors
                .AsNoTracking()
                .OrderBy(actor => actor.LastName)
                .ThenBy(actor => actor.FirstName),
            "Id",
            "FullName",
            selectedActorId);
    }

    private bool CharacterRoleExists(int id)
    {
        return _context.CharacterRoles.Any(role => role.Id == id);
    }
}
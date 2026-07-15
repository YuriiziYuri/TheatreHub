using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class HallsController : Controller
{
    private readonly ApplicationDbContext _context;

    public HallsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Halls
    public async Task<IActionResult> Index(int? venueId)
    {
        var query = _context.Halls
            .AsNoTracking()
            .Include(hall => hall.Venue)
            .AsQueryable();

        if (venueId.HasValue)
        {
            query = query.Where(hall => hall.VenueId == venueId.Value);

            var venue = await _context.Venues
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == venueId.Value);

            ViewBag.VenueId = venueId;
            ViewBag.VenueName = venue?.Name;
        }

        var halls = await query
            .OrderBy(hall => hall.Venue.Name)
            .ThenBy(hall => hall.Name)
            .ToListAsync();

        return View(halls);
    }

    // GET: Halls/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hall = await _context.Halls
            .AsNoTracking()
            .Include(item => item.Venue)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Performance)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (hall == null)
        {
            return NotFound();
        }

        hall.Rehearsals = hall.Rehearsals
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToList();

        return View(hall);
    }

    // GET: Halls/Create
    public async Task<IActionResult> Create(int? venueId)
    {
        var hall = new Hall
        {
            VenueId = venueId ?? 0,
            IsActive = true
        };

        await LoadVenuesAsync(hall.VenueId);

        return View(hall);
    }

    // POST: Halls/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "VenueId,Name,Capacity,RentalCost," +
            "HasStage,HasCurtains,HasDressingRooms," +
            "HasLighting,HasSound,HasMicrophones,HasProjector," +
            "EquipmentNotes,Notes,IsActive")]
        Hall hall)
    {
        NormalizeHall(hall);

        await ValidateHallAsync(hall);

        if (!ModelState.IsValid)
        {
            await LoadVenuesAsync(hall.VenueId);
            return View(hall);
        }

        _context.Halls.Add(hall);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(hall.Name),
                "На цьому майданчику вже існує зал із такою назвою.");

            await LoadVenuesAsync(hall.VenueId);
            return View(hall);
        }

        TempData["SuccessMessage"] =
            $"Зал «{hall.Name}» успішно створено.";

        return RedirectToAction(
            nameof(Index),
            new { venueId = hall.VenueId });
    }

    // GET: Halls/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hall = await _context.Halls
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (hall == null)
        {
            return NotFound();
        }

        await LoadVenuesAsync(hall.VenueId);

        return View(hall);
    }

    // POST: Halls/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,VenueId,Name,Capacity,RentalCost," +
            "HasStage,HasCurtains,HasDressingRooms," +
            "HasLighting,HasSound,HasMicrophones,HasProjector," +
            "EquipmentNotes,Notes,IsActive")]
        Hall hall)
    {
        if (id != hall.Id)
        {
            return NotFound();
        }

        NormalizeHall(hall);

        await ValidateHallAsync(hall, excludedHallId: id);

        if (!ModelState.IsValid)
        {
            await LoadVenuesAsync(hall.VenueId);
            return View(hall);
        }

        var existingHall = await _context.Halls
            .FirstOrDefaultAsync(item => item.Id == id);

        if (existingHall == null)
        {
            return NotFound();
        }

        existingHall.VenueId = hall.VenueId;
        existingHall.Name = hall.Name;
        existingHall.Capacity = hall.Capacity;
        existingHall.RentalCost = hall.RentalCost;
        existingHall.HasStage = hall.HasStage;
        existingHall.HasCurtains = hall.HasCurtains;
        existingHall.HasDressingRooms = hall.HasDressingRooms;
        existingHall.HasLighting = hall.HasLighting;
        existingHall.HasSound = hall.HasSound;
        existingHall.HasMicrophones = hall.HasMicrophones;
        existingHall.HasProjector = hall.HasProjector;
        existingHall.EquipmentNotes = hall.EquipmentNotes;
        existingHall.Notes = hall.Notes;
        existingHall.IsActive = hall.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(hall.Name),
                "На цьому майданчику вже існує зал із такою назвою.");

            await LoadVenuesAsync(hall.VenueId);
            return View(hall);
        }

        TempData["SuccessMessage"] =
            $"Зал «{existingHall.Name}» успішно оновлено.";

        return RedirectToAction(
            nameof(Details),
            new { id = existingHall.Id });
    }

    // GET: Halls/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hall = await _context.Halls
            .AsNoTracking()
            .Include(item => item.Venue)
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (hall == null)
        {
            return NotFound();
        }

        return View(hall);
    }

    // POST: Halls/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var hall = await _context.Halls
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (hall == null)
        {
            return NotFound();
        }

        if (hall.Rehearsals.Any())
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити зал, оскільки з ним пов’язані репетиції.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var venueId = hall.VenueId;
        var hallName = hall.Name;

        _context.Halls.Remove(hall);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Зал «{hallName}» видалено.";

        return RedirectToAction(
            nameof(Index),
            new { venueId });
    }

    private async Task LoadVenuesAsync(
        int? selectedVenueId = null)
    {
        var venues = await _context.Venues
            .AsNoTracking()
            .Where(venue =>
                venue.IsActive ||
                venue.Id == selectedVenueId)
            .OrderBy(venue => venue.Name)
            .ToListAsync();

        ViewData["VenueId"] = new SelectList(
            venues,
            nameof(Venue.Id),
            nameof(Venue.Name),
            selectedVenueId);
    }

    private async Task ValidateHallAsync(
        Hall hall,
        int? excludedHallId = null)
    {
        var venueExists = await _context.Venues
            .AnyAsync(venue => venue.Id == hall.VenueId);

        if (!venueExists)
        {
            ModelState.AddModelError(
                nameof(hall.VenueId),
                "Оберіть чинний майданчик.");
        }

        if (string.IsNullOrWhiteSpace(hall.Name) ||
            hall.VenueId <= 0)
        {
            return;
        }

        var duplicateExists = await _context.Halls
            .AnyAsync(existing =>
                existing.VenueId == hall.VenueId &&
                existing.Name.ToLower() == hall.Name.ToLower() &&
                (!excludedHallId.HasValue ||
                 existing.Id != excludedHallId.Value));

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(hall.Name),
                "На цьому майданчику вже існує зал із такою назвою.");
        }
    }

    private static void NormalizeHall(Hall hall)
    {
        hall.Name = hall.Name?.Trim() ?? string.Empty;

        hall.EquipmentNotes =
            NormalizeOptionalText(hall.EquipmentNotes);

        hall.Notes =
            NormalizeOptionalText(hall.Notes);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

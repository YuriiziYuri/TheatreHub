using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class VenuesController : Controller
{
    private readonly ApplicationDbContext _context;

    public VenuesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Venues
    public async Task<IActionResult> Index()
    {
        var venues = await _context.Venues
            .AsNoTracking()
            .Include(venue => venue.Halls)
            .OrderBy(venue => venue.Name)
            .ToListAsync();

        return View(venues);
    }

    // GET: Venues/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var venue = await _context.Venues
            .AsNoTracking()
            .Include(item => item.Halls)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (venue == null)
        {
            return NotFound();
        }

        return View(venue);
    }

    // GET: Venues/Create
    public IActionResult Create()
    {
        return View(new Venue
        {
            IsActive = true
        });
    }

    // POST: Venues/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "Name,Address,City,ContactPerson," +
            "PhoneNumber,Email,Notes,IsActive")]
        Venue venue)
    {
        NormalizeVenue(venue);

        await ValidateDuplicateVenueAsync(venue);

        if (!ModelState.IsValid)
        {
            return View(venue);
        }

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Майданчик «{venue.Name}» успішно створено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Venues/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var venue = await _context.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (venue == null)
        {
            return NotFound();
        }

        return View(venue);
    }

    // POST: Venues/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,Name,Address,City,ContactPerson," +
            "PhoneNumber,Email,Notes,IsActive")]
        Venue venue)
    {
        if (id != venue.Id)
        {
            return NotFound();
        }

        NormalizeVenue(venue);

        await ValidateDuplicateVenueAsync(
            venue,
            excludedVenueId: id);

        if (!ModelState.IsValid)
        {
            return View(venue);
        }

        var existingVenue = await _context.Venues
            .FirstOrDefaultAsync(item => item.Id == id);

        if (existingVenue == null)
        {
            return NotFound();
        }

        existingVenue.Name = venue.Name;
        existingVenue.Address = venue.Address;
        existingVenue.City = venue.City;
        existingVenue.ContactPerson = venue.ContactPerson;
        existingVenue.PhoneNumber = venue.PhoneNumber;
        existingVenue.Email = venue.Email;
        existingVenue.Notes = venue.Notes;
        existingVenue.IsActive = venue.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Майданчик «{existingVenue.Name}» успішно оновлено.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Venues/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var venue = await _context.Venues
            .AsNoTracking()
            .Include(item => item.Halls)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (venue == null)
        {
            return NotFound();
        }

        return View(venue);
    }

    // POST: Venues/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var venue = await _context.Venues
            .Include(item => item.Halls)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (venue == null)
        {
            return NotFound();
        }

        if (venue.Halls.Any())
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити майданчик, поки до нього прив’язані зали.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var venueName = venue.Name;

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Майданчик «{venueName}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateDuplicateVenueAsync(
        Venue venue,
        int? excludedVenueId = null)
    {
        if (string.IsNullOrWhiteSpace(venue.Name) ||
            string.IsNullOrWhiteSpace(venue.Address))
        {
            return;
        }

        var duplicateExists = await _context.Venues.AnyAsync(
            existing =>
                existing.Name.ToLower() == venue.Name.ToLower() &&
                existing.Address.ToLower() == venue.Address.ToLower() &&
                (!excludedVenueId.HasValue ||
                 existing.Id != excludedVenueId.Value));

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(venue.Name),
                "Майданчик із такою назвою та адресою вже існує.");
        }
    }

    private static void NormalizeVenue(Venue venue)
    {
        venue.Name = venue.Name?.Trim() ?? string.Empty;
        venue.Address = venue.Address?.Trim() ?? string.Empty;
        venue.City = NormalizeOptionalText(venue.City);
        venue.ContactPerson = NormalizeOptionalText(venue.ContactPerson);
        venue.PhoneNumber = NormalizeOptionalText(venue.PhoneNumber);
        venue.Email = NormalizeOptionalText(venue.Email);
        venue.Notes = NormalizeOptionalText(venue.Notes);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
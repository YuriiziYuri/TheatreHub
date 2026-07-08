using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;

        var model = new DashboardViewModel
        {
            PerformancesCount =
                await _context.Performances.CountAsync(),

            ActorsCount =
                await _context.Actors.CountAsync(),

            // ¬акантна роль Ч немаЇ затвердженого
            // поточного виконавц€ основного складу.
            VacantRolesCount =
                await _context.CharacterRoles.CountAsync(role =>
                    !role.Assignments.Any(assignment =>
                        assignment.IsCurrent &&
                        assignment.CastType == CastType.Main &&
                        assignment.Status ==
                            RoleAssignmentStatus.Approved)),

            UpcomingRehearsalsCount =
                await _context.Rehearsals.CountAsync(rehearsal =>
                    rehearsal.StartDateTime >= now &&
                    rehearsal.Status != RehearsalStatus.Cancelled),

            UpcomingRehearsals =
                await _context.Rehearsals
                    .AsNoTracking()
                    .Include(rehearsal => rehearsal.Performance)
                    .Include(rehearsal => rehearsal.Participants)
                        .ThenInclude(participant => participant.Actor)
                    .Where(rehearsal =>
                        rehearsal.StartDateTime >= now &&
                        rehearsal.Status != RehearsalStatus.Cancelled)
                    .OrderBy(rehearsal =>
                        rehearsal.StartDateTime)
                    .Take(5)
                    .ToListAsync(),

            ActivePerformances =
                await _context.Performances
                    .AsNoTracking()
                    .Include(performance =>
                        performance.CharacterRoles)
                        .ThenInclude(role => role.Assignments)
                    .Where(performance =>
                        performance.Status !=
                            PerformanceStatus.Completed &&
                        performance.Status !=
                            PerformanceStatus.Cancelled)
                    .OrderBy(performance =>
                        performance.PremiereDate
                        ?? DateTime.MaxValue)
                    .Take(5)
                    .ToListAsync(),

            VacantRoles =
                await _context.CharacterRoles
                    .AsNoTracking()
                    .Include(role => role.Performance)
                    .Include(role => role.Assignments)
                    .Where(role =>
                        !role.Assignments.Any(assignment =>
                            assignment.IsCurrent &&
                            assignment.CastType ==
                                CastType.Main &&
                            assignment.Status ==
                                RoleAssignmentStatus.Approved))
                    .OrderBy(role =>
                        role.Performance.Title)
                    .ThenBy(role => role.Name)
                    .Take(5)
                    .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
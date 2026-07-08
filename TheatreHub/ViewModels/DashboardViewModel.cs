using TheatreHub.Models;

namespace TheatreHub.ViewModels;

public class DashboardViewModel
{
    public int PerformancesCount { get; set; }

    public int ActorsCount { get; set; }

    public int VacantRolesCount { get; set; }

    public int UpcomingRehearsalsCount { get; set; }

    public List<Rehearsal> UpcomingRehearsals { get; set; } = [];

    public List<Performance> ActivePerformances { get; set; } = [];

    public List<CharacterRole> VacantRoles { get; set; } = [];
}
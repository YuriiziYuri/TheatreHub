namespace TheatreHub.ViewModels;

public class SceneRolesViewModel
{
    public int SceneId { get; set; }

    public string SceneTitle { get; set; } = string.Empty;

    public string ActName { get; set; } = string.Empty;

    public int PerformanceId { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public List<SceneRoleSelectionViewModel> Roles { get; set; }
        = [];
}
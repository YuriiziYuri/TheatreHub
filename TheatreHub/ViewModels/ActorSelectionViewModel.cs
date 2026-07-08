namespace TheatreHub.ViewModels;

public class ActorSelectionViewModel
{
    public int ActorId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
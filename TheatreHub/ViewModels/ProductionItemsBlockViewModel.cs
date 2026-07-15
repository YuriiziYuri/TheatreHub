using TheatreHub.Models;

namespace TheatreHub.ViewModels;

public class ProductionItemsBlockViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? PerformanceId { get; set; }

    public int? ActId { get; set; }

    public int? SceneId { get; set; }

    public List<ProductionItem> Items { get; set; } = [];
}
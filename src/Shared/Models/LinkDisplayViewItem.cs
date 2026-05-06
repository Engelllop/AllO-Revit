namespace AllO.Models;

public class LinkDisplayViewItem
{
    public int ViewId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ViewType { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = false;
}

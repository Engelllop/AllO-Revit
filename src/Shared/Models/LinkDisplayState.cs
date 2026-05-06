namespace AllO.Models;

/// <summary>
/// Represents the display settings of a Revit link within a view.
/// </summary>
public class LinkDisplayState
{
    public int LinkInstanceId { get; set; }

    /// <summary>Whether the link is visible in the view.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Whether the link is shown in halftone.</summary>
    public bool IsHalftone { get; set; } = false;

    /// <summary>Display mode: ByHostView, ByLinkedView, or Custom.</summary>
    public string DisplayMode { get; set; } = "ByHostView";

    /// <summary>True if the user has modified this state and wants to apply it.</summary>
    public bool IsSelected { get; set; } = false;

    public bool WillChange => IsSelected;
}

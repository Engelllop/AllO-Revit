namespace AllO.Models;

/// <summary>Revit link instance in the host document (selector for source model).</summary>
public class LinkDocumentInfo
{
    public int LinkInstanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
}

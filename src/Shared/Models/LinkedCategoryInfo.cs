namespace AllO.Models;

/// <summary>Category in a linked document (families of this category can be copied).</summary>
public class LinkedCategoryInfo
{
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

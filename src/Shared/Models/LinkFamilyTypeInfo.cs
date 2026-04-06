namespace AllO.Models;

/// <summary>Family type (symbol) inside a linked document.</summary>
public class LinkFamilyTypeInfo
{
    public long FamilySymbolId { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}

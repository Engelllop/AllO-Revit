namespace AllO.Models;

/// <summary>
/// Modelo que representa un tipo de cajetin (TitleBlock) disponible en el proyecto.
/// </summary>
public class TitleBlockInfo
{
    public int TypeId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName => string.IsNullOrEmpty(TypeName)
        ? FamilyName
        : $"{FamilyName} : {TypeName}";
}

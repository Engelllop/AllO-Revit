using Autodesk.Revit.DB;

namespace AllO.Helpers;

/// <summary>Compatibilidad ElementId: Shared net48 corre también en Revit 2023,
/// que no tiene ElementId.Value ni ElementId(long) → MissingMethodException.</summary>
public static class ElementIdCompat
{
#if NET48
#pragma warning disable CS0618 // IntegerValue/ElementId(int) obsoletos en SDK 2024, únicos disponibles en 2023
    public static long ToLong(this ElementId id) => id.IntegerValue;
    public static ElementId ToElementId(this long value) => new((int)value);
#pragma warning restore CS0618
#else
    public static long ToLong(this ElementId id) => id.Value;
    public static ElementId ToElementId(this long value) => new(value);
#endif
}

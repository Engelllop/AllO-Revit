namespace AllO.Helpers;

/// <summary>Lógica pura de nombres/numeración de hojas (sin dependencias de Revit, testeable).</summary>
public static class SheetNaming
{
    /// <summary>Expande un patrón de numeración: {n}=número, {nn}=2 dígitos, {nnn}=3 dígitos.</summary>
    public static string ExpandNumberPattern(string pattern, int n)
    {
        if (string.IsNullOrEmpty(pattern)) return n.ToString();
        return pattern
            .Replace("{nnn}", n.ToString("D3"))
            .Replace("{nn}", n.ToString("D2"))
            .Replace("{n}", n.ToString());
    }

    /// <summary>Escapa un valor para CSV (comillas si contiene coma, comilla o salto de línea).</summary>
    public static string CsvEscape(string? v)
    {
        v ??= "";
        return v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? "\"" + v.Replace("\"", "\"\"") + "\""
            : v;
    }
}

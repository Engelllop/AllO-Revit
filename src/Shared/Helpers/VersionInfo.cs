using System.Reflection;

namespace AllO.Helpers;

public static class VersionInfo
{
    public static string Short { get; } =
        "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?");
}

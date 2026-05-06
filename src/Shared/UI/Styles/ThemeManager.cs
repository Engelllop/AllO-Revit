using System.Windows;
using AllO.Helpers;
using AllO.Services;

namespace AllO.UI.Styles;

/// <summary>
/// Aplica el tema (oscuro/claro) a Application.Resources al iniciar.
/// Decisión:
///   1. <see cref="AllOSettings.DarkTheme"/> si el usuario lo eligió explícitamente.
///   2. Si no, detecta el tema de Revit con <see cref="RevitTheme"/>.
/// El AllOTheme.xaml base define todos los estilos con claves de color;
/// AllOTheme.Light.xaml sólo sobrescribe los colores cuando aplica light.
/// </summary>
public static class ThemeManager
{
    private static bool _applied;

    public static void Apply()
    {
        if (_applied) return;
        _applied = true;

        var app = Application.Current ?? new Application();

        var baseDict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/AllO.Shared;component/UI/Styles/AllOTheme.xaml",
                UriKind.Absolute)
        };
        app.Resources.MergedDictionaries.Add(baseDict);

        var dark = AllOSettings.Current.DarkTheme && RevitTheme.IsDark();
        if (!dark)
        {
            var lightDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AllO.Shared;component/UI/Styles/AllOTheme.Light.xaml",
                    UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(lightDict);
        }
    }
}

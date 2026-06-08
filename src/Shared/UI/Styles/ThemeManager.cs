using System;
using System.Windows;

namespace AllO.UI.Styles;

public static class ThemeManager
{
    private static bool _applied;

    public static void Apply()
    {
        if (_applied) return;
        _applied = true;

        var app = Application.Current ?? new Application();

        var dict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/AllO.Shared;component/UI/Styles/AllOTheme.xaml", UriKind.Absolute)
        };

        app.Resources.MergedDictionaries.Add(dict);
    }
}

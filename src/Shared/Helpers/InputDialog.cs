using System.Windows;
using System.Windows.Controls;

namespace AllO.Helpers;

/// <summary>
/// Simple WPF input dialog that works on both net48 and net8.0-windows.
/// Usa la paleta AllO (estilo AllOWindow + controles temáticos) si está cargada.
/// </summary>
public static class InputDialog
{
    private static object? Res(string key) => Application.Current?.TryFindResource(key);

    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        var win = new Window
        {
            Title = title.StartsWith("AllO", StringComparison.OrdinalIgnoreCase) ? title : $"AllO — {title}",
            Width = 380,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize
        };
        if (Res("AllOWindow") is Style ws) win.Style = ws;
        else win.Background = System.Windows.Media.Brushes.White;

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8), FontSize = 12, TextWrapping = TextWrapping.Wrap };
        if (Res("TextSecondary") is System.Windows.Media.Brush fg) lbl.Foreground = fg;
        Grid.SetRow(lbl, 0);
        grid.Children.Add(lbl);

        var tb = new TextBox { Text = defaultValue, FontSize = 12.5, Padding = new Thickness(10, 8, 10, 8) };
        if (Res("SearchBox") is Style tbs) tb.Style = tbs;
        Grid.SetRow(tb, 1);
        grid.Children.Add(tb);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var okBtn = new Button { Content = "OK", MinWidth = 80, Padding = new Thickness(18, 6, 18, 6), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancelBtn = new Button { Content = "Cancel", MinWidth = 80, Padding = new Thickness(18, 6, 18, 6), IsCancel = true };
        if (Res("BtnPrimary") is Style ps) okBtn.Style = ps;
        if (Res("BtnSecondary") is Style ss) cancelBtn.Style = ss;
        btnPanel.Children.Add(cancelBtn);
        btnPanel.Children.Add(okBtn);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);

        win.Content = grid;

        string? result = null;
        bool confirmed = false;

        okBtn.Click += (_, _) => { confirmed = true; result = tb.Text; win.Close(); };
        cancelBtn.Click += (_, _) => { win.Close(); };

        tb.Focus();
        tb.SelectAll();
        win.ShowDialog();

        return confirmed ? result : null;
    }
}

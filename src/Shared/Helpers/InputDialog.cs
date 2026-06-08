using System.Windows;
using System.Windows.Controls;

namespace AllO.Helpers;

/// <summary>
/// Simple WPF input dialog that works on both net48 and net8.0-windows.
/// </summary>
public static class InputDialog
{
    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        var win = new Window
        {
            Title = title,
            Width = 360,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6), FontSize = 12 };
        Grid.SetRow(lbl, 0);
        grid.Children.Add(lbl);

        var tb = new TextBox { Text = defaultValue, FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
        Grid.SetRow(tb, 1);
        grid.Children.Add(tb);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var okBtn = new Button { Content = "OK", Width = 70, Height = 26, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancelBtn = new Button { Content = "Cancel", Width = 70, Height = 26, IsCancel = true };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
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

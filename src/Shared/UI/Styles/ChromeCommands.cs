using System.Windows;
using System.Windows.Input;

namespace AllO.UI.Styles;

/// <summary>
/// Attached behavior que registra los CommandBindings de SystemCommands
/// (cerrar / minimizar / maximizar / restaurar) en cualquier Window que use el
/// estilo AllOWindow, evitando code-behind por ventana.
/// </summary>
public static class ChromeCommands
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable", typeof(bool), typeof(ChromeCommands),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject d, bool value) => d.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject d) => (bool)d.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window w || !(bool)e.NewValue) return;

        w.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(w)));
        w.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(w),
            (_, a) => a.CanExecute = w.ResizeMode != ResizeMode.NoResize));
        w.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(w),
            (_, a) => a.CanExecute = w.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip));
        w.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(w),
            (_, a) => a.CanExecute = w.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip));
    }
}

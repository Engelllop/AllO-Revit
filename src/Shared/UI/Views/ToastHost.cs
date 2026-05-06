using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AllO.Services;

namespace AllO.UI.Views;

/// <summary>
/// Pinta toasts en la esquina inferior derecha de la pantalla activa.
/// API estática: <c>ToastHost.Show("Listo", "Sheet creado")</c>.
/// Respeta <see cref="AllOSettings.ShowToasts"/>. Thread-safe (marshala al UI thread).
/// </summary>
public static class ToastHost
{
    private static Window? _host;
    private static StackPanel? _stack;
    private static Dispatcher? _dispatcher;

    public static void Show(string title, string body = "",
        ToastNotification.ToastKind kind = ToastNotification.ToastKind.Info,
        int durationMs = 3500)
    {
        if (!AllOSettings.Current.ShowToasts) return;

        var d = _dispatcher ?? Application.Current?.Dispatcher;
        if (d == null) return;

        d.BeginInvoke(new Action(() => ShowInternal(title, body, kind, durationMs)));
    }

    private static void ShowInternal(string title, string body,
        ToastNotification.ToastKind kind, int durationMs)
    {
        EnsureHost();
        if (_stack == null || _host == null) return;

        var toast = new ToastNotification
        {
            Margin = new Thickness(0, 6, 0, 0),
            Opacity = 0
        };
        toast.SetContent(title, body, kind);
        _stack.Children.Add(toast);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (_, _) =>
            {
                _stack.Children.Remove(toast);
                if (_stack.Children.Count == 0) _host.Hide();
            };
            toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        };
        timer.Start();

        _host.Show();
    }

    private static void EnsureHost()
    {
        if (_host != null) return;

        _dispatcher = Application.Current?.Dispatcher;
        _stack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };

        _host = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            ResizeMode = ResizeMode.NoResize,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            Content = _stack,
            Focusable = false,
            ShowActivated = false
        };

        _host.Loaded += (_, _) =>
        {
            var work = SystemParameters.WorkArea;
            _host.Left = work.Right - _host.Width - 16;
            _host.Top  = work.Bottom - _host.ActualHeight - 16;
        };
        _host.SizeChanged += (_, _) =>
        {
            var work = SystemParameters.WorkArea;
            _host.Left = work.Right - _host.Width - 16;
            _host.Top  = work.Bottom - _host.ActualHeight - 16;
        };
    }
}

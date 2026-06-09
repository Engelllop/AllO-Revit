using System.Windows;
using AllO.Services;

namespace AllO.UI.Toast;

public static class ToastHost
{
    private static ToastWindow? _window;

    public static void Show(string title, string message, ToastKind kind = ToastKind.Info)
    {
        if (!AllOSettings.Current.ShowToasts) return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            _window ??= new ToastWindow();
            if (!_window.IsVisible) _window.Show();
            _window.AddToast(new ToastNotification { Title = title, Message = message, Kind = kind });
        });
    }
}

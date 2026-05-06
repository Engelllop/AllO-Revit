using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AllO.UI.Views;

public partial class ToastNotification : UserControl
{
    public enum ToastKind { Info, Success, Warning, Error }

    public ToastNotification()
    {
        InitializeComponent();
    }

    public void SetContent(string title, string body, ToastKind kind)
    {
        TitleText.Text = title;
        BodyText.Text = body;
        BodyText.Visibility = string.IsNullOrEmpty(body)
            ? Visibility.Collapsed : Visibility.Visible;

        var key = kind switch
        {
            ToastKind.Success => "Success",
            ToastKind.Warning => "Warning",
            ToastKind.Error   => "Danger",
            _                 => "Accent"
        };
        if (TryFindResource(key) is Brush b) IconBox.Background = b;
    }
}

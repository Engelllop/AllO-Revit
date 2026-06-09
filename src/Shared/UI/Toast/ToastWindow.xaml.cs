using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AllO.UI.Toast;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
        // Position bottom-right
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 20;
        Top = screen.Bottom - 100;
    }

    public void AddToast(ToastNotification toast)
    {
        var border = new Border
        {
            Background = toast.Kind switch
            {
                ToastKind.Success => new SolidColorBrush(Color.FromRgb(26, 58, 42)),
                ToastKind.Error => new SolidColorBrush(Color.FromRgb(58, 26, 26)),
                ToastKind.Warning => new SolidColorBrush(Color.FromRgb(58, 46, 26)),
                _ => new SolidColorBrush(Color.FromRgb(44, 44, 44))
            },
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(8, 4, 8, 4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            BorderThickness = new Thickness(1)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = toast.Title,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12
        });
        sp.Children.Add(new TextBlock
        {
            Text = toast.Message,
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        border.Child = sp;
        border.Opacity = 0;
        ToastPanel.Children.Insert(0, border);
        border.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));

        var timer = new DispatcherTimer { Interval = toast.Duration };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (_, _) =>
            {
                ToastPanel.Children.Remove(border);
                if (ToastPanel.Children.Count == 0) Hide();
            };
            border.BeginAnimation(OpacityProperty, fadeOut);
        };
        timer.Start();
    }
}

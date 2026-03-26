using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.UI;
using AllO.Models;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class ColorCoderWindow : Window
{
    public ColorCoderWindow(UIApplication uiApp)
    {
        InitializeComponent();
        var vm = new ColorCoderViewModel(uiApp);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }

    /// <summary>
    /// Handle clicking a color swatch to assign it to the parent document.
    /// </summary>
    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.Background is not SolidColorBrush brush) return;

        // Walk up the visual tree to find the DocumentColorInfo
        var parent = border;
        DocumentColorInfo? docInfo = null;
        DependencyObject current = border;

        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is DocumentColorInfo info)
            {
                docInfo = info;
                break;
            }
            current = VisualTreeHelper.GetParent(current);
        }

        if (docInfo != null)
        {
            docInfo.AssignedColor = brush.Color;
        }
    }
}

using System.Windows;
using Autodesk.Revit.UI;
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

        Loaded += (_, _) => UpdateMaximizeGlyph();
        StateChanged += (_, _) => UpdateMaximizeGlyph();
    }

    private void UpdateMaximizeGlyph()
    {
        if (MaximizeGlyph == null) return;
        MaximizeGlyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        if (MaximizeButton != null)
            MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseChrome_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

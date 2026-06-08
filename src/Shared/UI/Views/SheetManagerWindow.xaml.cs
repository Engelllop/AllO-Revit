using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

/// <summary>
/// SheetManagerWindow — Ventana principal del Sheet Manager de AllO.
/// Code-behind minimo: solo inyecta el ViewModel.
/// </summary>
public partial class SheetManagerWindow : Window
{
    public SheetManagerWindow(IRevitService service, int initialPanel = 0, bool showNav = true, string? title = null)
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(title))
            Title = $"AllO — {title}";
        if (!showNav)
        {
            SidebarBorder.Visibility = Visibility.Collapsed;
            RootGrid.ColumnDefinitions[0].Width = new GridLength(0);
        }
        var vm = new SheetManagerViewModel(service, initialPanel);
        vm.CloseAction = Close;
        vm.ShowNav = showNav;
        DataContext = vm;
    }

    /// <summary>
    /// Constructor sin parametros para el disenador XAML y testing.
    /// </summary>
    public SheetManagerWindow() : this(new MockService()) { }
}

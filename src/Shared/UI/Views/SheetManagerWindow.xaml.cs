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
    public SheetManagerWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new SheetManagerViewModel(service);
        vm.CloseAction = Close;
        DataContext = vm;
    }

    /// <summary>
    /// Constructor sin parametros para el disenador XAML y testing.
    /// </summary>
    public SheetManagerWindow() : this(new MockService()) { }
}

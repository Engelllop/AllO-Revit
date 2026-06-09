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
    }
}

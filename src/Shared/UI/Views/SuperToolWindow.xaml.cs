using System.Windows;
using System.Windows.Controls;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class SuperToolWindow : Window
{
    public SuperToolWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new SuperToolViewModel(service);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }

    private void NavTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && DataContext is SuperToolViewModel vm)
        {
            if (int.TryParse(tag, out int index))
                vm.SelectedTabIndex = index;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class SuperToolWindow : Window
{
    public SuperToolWindow(IRevitService service, int initialTab = 0, bool showNav = true, string? title = null)
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(title))
            Title = $"AllO — {title}";
        if (!showNav)
        {
            SidebarBorder.Visibility = Visibility.Collapsed;
            RootGrid.ColumnDefinitions[0].Width = new GridLength(0);
        }
        var vm = new SuperToolViewModel(service);
        vm.CloseAction = () => Close();
        vm.SelectedTabIndex = initialTab;
        vm.ShowNav = showNav;
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

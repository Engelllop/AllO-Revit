using System.Windows;
using System.Windows.Input;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class QuickSearchWindow : Window
{
    public QuickSearchWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new QuickSearchViewModel(service);
        vm.CloseAction = () => Close();
        DataContext = vm;

        // Auto-focus search box
        Loaded += (s, e) =>
        {
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
        };
    }
}

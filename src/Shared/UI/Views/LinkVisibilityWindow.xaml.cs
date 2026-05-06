using AllO.Services;
using AllO.UI.ViewModels;
using System.Windows;

namespace AllO.UI.Views;

public partial class LinkVisibilityWindow : Window
{
    public LinkVisibilityWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new LinkVisibilityViewModel(service);
        vm.RequestClose += () => Close();
        DataContext = vm;
    }
}

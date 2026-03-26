using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class PublishWindow : Window
{
    public PublishWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new PublishViewModel(service);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }
}

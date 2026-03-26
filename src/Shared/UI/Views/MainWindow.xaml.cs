using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(IRevitService revitService)
    {
        InitializeComponent();
        _viewModel = new MainViewModel(revitService);
        _viewModel.CloseAction = Close;
        DataContext = _viewModel;
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadSheetsCommand.Execute(null);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ApplyRenameCommand.Execute(null);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

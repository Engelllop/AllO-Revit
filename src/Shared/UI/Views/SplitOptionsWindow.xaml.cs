using System.Windows;

namespace AllO.UI.Views;

public partial class SplitOptionsWindow : Window
{
    public string Gap { get; set; } = "0";

    public SplitOptionsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Windows;

namespace AllO.UI.Views;

public partial class RerouteOptionsWindow : Window
{
    public string[] Directions { get; } = { "Right", "Left", "Up", "Down" };
    public string SelectedDirection { get; set; } = "Right";
    public string Offset { get; set; } = "2";

    public RerouteOptionsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

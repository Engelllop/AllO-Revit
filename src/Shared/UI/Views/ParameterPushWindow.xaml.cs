using System.Collections.Generic;
using System.Windows;

namespace AllO.UI.Views;

public partial class ParameterPushWindow : Window
{
    public List<string> Parameters { get; }
    public string? SelectedParameter { get; set; }

    public ParameterPushWindow(IEnumerable<string> parameters)
    {
        InitializeComponent();
        Parameters = new List<string>(parameters);
        SelectedParameter = Parameters.Count > 0 ? Parameters[0] : null;
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedParameter)) { DialogResult = false; return; }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Collections.Generic;
using System.Windows;

namespace AllO.UI.Views;

public partial class SyncViewsWindow : Window
{
    public List<string> Views { get; }
    public string? SelectedMaster { get; set; }
    public string? SelectedSlave { get; set; }

    public SyncViewsWindow(IEnumerable<string> views, string? defaultMaster = null)
    {
        InitializeComponent();
        Views = new List<string>(views);
        SelectedMaster = defaultMaster ?? (Views.Count > 0 ? Views[0] : null);
        DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedMaster) || string.IsNullOrWhiteSpace(SelectedSlave)
            || SelectedMaster == SelectedSlave)
        { DialogResult = false; return; }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System;
using System.IO;
using System.Windows;

namespace AllO.UI.Views;

public partial class FamilyExportWindow : Window
{
    public string SelectedFolder { get; private set; } = string.Empty;

    public FamilyExportWindow()
    {
        InitializeComponent();
        PathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        // Selector de carpeta gráfico (truco SaveFileDialog: se toma su directorio).
        var picker = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select destination folder (just click Save)",
            FileName = "Select Folder",
            Filter = "Folder|*.folder",
            CheckFileExists = false,
            CheckPathExists = true,
            InitialDirectory = Directory.Exists(PathBox.Text)
                ? PathBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
        if (picker.ShowDialog() == true)
        {
            var folder = Path.GetDirectoryName(picker.FileName);
            if (!string.IsNullOrWhiteSpace(folder)) PathBox.Text = folder;
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var folder = PathBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show(this, "Please choose a destination folder.", "AllO — Family Export",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SelectedFolder = folder;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Windows;
using AllO.Services.Mcp;

namespace AllO.UI.Views;

public partial class AiConnectorWindow : Window
{
    private static AiConnectorWindow? _open;

    /// <summary>Modeless y singleton: un ShowDialog modal bloquea el idle de Revit y los
    /// ExternalEvents del MCP nunca se procesan mientras la ventana esté abierta.</summary>
    public static void ShowSingleton()
    {
        if (_open != null)
        {
            _open.UpdateUi();
            _open.Activate();
            return;
        }
        _open = new AiConnectorWindow();
        _open.Closed += (_, _) => _open = null;
        _open.Show();
    }

    public AiConnectorWindow()
    {
        InitializeComponent();
        UpdateUi();
    }

    private void UpdateUi()
    {
        bool running = McpServerHost.IsRunning;
        StatusBadgeText.Text = running ? "ON" : "OFF";
        ToggleButton.Content = running ? "Stop" : "Start";
        EndpointBox.Text = running ? McpServerHost.Endpoint : "Stopped";
        ClaudeCmdBox.Text = running
            ? $"claude mcp add --transport http revit {McpServerHost.Endpoint}"
            : $"claude mcp add --transport http revit http://localhost:{McpServerHost.BasePort}/mcp";
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (McpServerHost.IsRunning)
        {
            McpServerHost.Stop();
        }
        else
        {
            string? error = McpServerHost.Start();
            if (error != null)
                MessageBox.Show(this, error, "AllO — A.I. Connector",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        UpdateUi();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(ClaudeCmdBox.Text); } catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

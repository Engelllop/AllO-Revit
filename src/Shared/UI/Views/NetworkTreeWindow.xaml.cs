using System.Windows;
using AllO.Models;

namespace AllO.UI.Views;

public partial class NetworkTreeWindow : Window
{
    public string RootTitle { get; }
    public string TotalLength { get; }
    public string LongestPath { get; }
    public int Elbows { get; }
    public int Forks { get; }
    public int Terminals { get; }
    public int Runs { get; }
    public string TruncatedNote { get; }
    public Visibility TruncatedVisibility { get; }
    public List<string> Lines { get; }

    private readonly string _csv;

    public NetworkTreeWindow(NetNode root, NetRun? rootRun, NetworkSummary summary,
        Func<double, string> fmt, bool truncated)
    {
        RootTitle = "Start: " + root.Label;
        TotalLength = fmt(summary.TotalLengthFt);
        LongestPath = fmt(summary.LongestPathFt);
        Elbows = summary.ElbowCount;
        Forks = summary.ForkCount;
        Terminals = summary.TerminalCount;
        Runs = summary.RunCount;
        TruncatedNote = "⚠ Network truncated (over 5000 elements) — totals are partial.";
        TruncatedVisibility = truncated ? Visibility.Visible : Visibility.Collapsed;
        Lines = NetworkTreeFormatter.Lines(root, rootRun, fmt);
        _csv = NetworkTreeFormatter.Csv(root, rootRun, fmt);

        InitializeComponent();
        DataContext = this;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var text = $"{RootTitle}\r\nTotal length: {TotalLength} · Longest run: {LongestPath} · " +
                   $"Elbows: {Elbows} · Forks: {Forks} · Terminals: {Terminals} · Runs: {Runs}\r\n\r\n" +
                   string.Join("\r\n", Lines);
        Clipboard.SetText(text);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "network_tree.csv"
        };
        if (dlg.ShowDialog(this) == true)
            File.WriteAllText(dlg.FileName, _csv, System.Text.Encoding.UTF8);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

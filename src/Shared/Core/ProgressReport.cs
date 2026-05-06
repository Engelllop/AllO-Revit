namespace AllO.Core;

/// <summary>
/// Reporte de progreso usado por servicios al ejecutar operaciones largas.
/// </summary>
public sealed class ProgressReport
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Percent => Total <= 0 ? 0 : (double)Current / Total * 100.0;

    public static ProgressReport Of(int current, int total, string message = "") =>
        new() { Current = current, Total = total, Message = message };
}

/// <summary>
/// Host UI que muestra progreso. Implementado por una ProgressWindow WPF.
/// </summary>
public interface IProgressHost
{
    void Report(ProgressReport report);
    bool IsCancellationRequested { get; }
    System.Threading.CancellationToken Token { get; }
}

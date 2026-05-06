namespace AllO.UI.Toast;

public enum ToastKind { Info, Success, Warning, Error }

public class ToastNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastKind Kind { get; set; } = ToastKind.Info;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);
}

using Autodesk.Revit.UI;

namespace AllO.Services;

/// <summary>
/// Permite ejecutar trabajo en el hilo de Revit desde un hilo background usando
/// ExternalEvent. Esto evita congelar la UI en operaciones largas (Publish, FamilyTransfer,
/// TableGen, etc.).
///
/// Uso:
///   await RevitAsyncHelper.RunOnRevitThreadAsync(uiApp => { ... });
///
/// Importante: solo crear UNA instancia por sesión (la registramos en App.OnStartup
/// y la guardamos en ServiceLocator). ExternalEvent debe crearse desde OnStartup.
/// </summary>
public sealed class RevitAsyncHelper : IExternalEventHandler
{
    private readonly ExternalEvent _event;
    private readonly Queue<WorkItem> _queue = new();
    private readonly object _lock = new();

    public RevitAsyncHelper()
    {
        _event = ExternalEvent.Create(this);
    }

    public Task RunAsync(Action<UIApplication> action)
    {
        var tcs = new TaskCompletionSource<bool>();
        lock (_lock)
        {
            _queue.Enqueue(new WorkItem(action, tcs));
        }
        _event.Raise();
        return tcs.Task;
    }

    public Task<T> RunAsync<T>(Func<UIApplication, T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        lock (_lock)
        {
            _queue.Enqueue(new WorkItem(uiApp =>
            {
                var result = func(uiApp);
                tcs.SetResult(result);
            }, null));
        }
        _event.Raise();
        return tcs.Task;
    }

    public void Execute(UIApplication app)
    {
        WorkItem? item;
        while (true)
        {
            lock (_lock)
            {
                if (_queue.Count == 0) return;
                item = _queue.Dequeue();
            }

            try
            {
                item!.Action(app);
                item.Tcs?.SetResult(true);
            }
            catch (Exception ex)
            {
                item!.Tcs?.SetException(ex);
            }
        }
    }

    public string GetName() => "AllO.RevitAsyncHelper";

    private sealed class WorkItem
    {
        public Action<UIApplication> Action { get; }
        public TaskCompletionSource<bool>? Tcs { get; }
        public WorkItem(Action<UIApplication> action, TaskCompletionSource<bool>? tcs)
        { Action = action; Tcs = tcs; }
    }
}

using Autodesk.Revit.UI;
using System;
using System.Threading.Tasks;

namespace AllO.Core;

/// <summary>
/// Helper to run code on the Revit thread without freezing the UI.
/// Uses <see cref="ExternalEvent" /> — the standard pattern for Revit add-ins.
/// </summary>
public class RevitAsyncHelper : IExternalEventHandler
{
    private readonly UIApplication _uiApp;
    private readonly ExternalEvent _externalEvent;
    private Action<UIApplication>? _pendingAction;
    private TaskCompletionSource<bool>? _tcs;

    public RevitAsyncHelper(UIApplication uiApp)
    {
        _uiApp = uiApp;
        _externalEvent = ExternalEvent.Create(this);
    }

    public Task RunAsync(Action<UIApplication> action)
    {
        _pendingAction = action;
        _tcs = new TaskCompletionSource<bool>();
        _externalEvent.Raise();
        return _tcs.Task;
    }

    public void Execute(UIApplication app)
    {
        try
        {
            _pendingAction?.Invoke(app);
            _tcs?.TrySetResult(true);
        }
        catch (Exception ex)
        {
            _tcs?.TrySetException(ex);
        }
        finally
        {
            _pendingAction = null;
        }
    }

    public string GetName() => "AllO RevitAsyncHelper";
}

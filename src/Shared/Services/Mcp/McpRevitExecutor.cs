using System.Collections.Concurrent;
using Autodesk.Revit.UI;
using AllO.Helpers;

namespace AllO.Services.Mcp;

/// <summary>
/// Puente HTTP→Revit: las tools llegan en threads del listener y la Revit API solo acepta
/// llamadas en contexto API, así que se encolan y un ExternalEvent las drena en el UI thread.
/// <see cref="EnsureCreated"/> DEBE llamarse desde un comando (ExternalEvent.Create lo exige).
/// </summary>
public sealed class McpRevitExecutor : IExternalEventHandler
{
    private sealed class Job
    {
        public Func<UIApplication, object?> Work = null!;
        public TaskCompletionSource<object?> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static McpRevitExecutor? _instance;
    private ExternalEvent _event = null!;
    private readonly ConcurrentQueue<Job> _jobs = new();

    private McpRevitExecutor()
    {
    }

    public static McpRevitExecutor? Instance => _instance;

    public static McpRevitExecutor EnsureCreated()
    {
        if (_instance == null)
        {
            var inst = new McpRevitExecutor();
            inst._event = ExternalEvent.Create(inst);
            _instance = inst;
        }
        return _instance;
    }

    /// <summary>Bloquea el thread HTTP hasta que Revit procese el trabajo (o timeout si el modelo está ocupado).</summary>
    public object? Run(Func<UIApplication, object?> work, int timeoutMs = 60000)
    {
        var job = new Job { Work = work };
        _jobs.Enqueue(job);
        _event.Raise();

        try
        {
            if (!job.Tcs.Task.Wait(timeoutMs))
                throw new TimeoutException(
                    "Revit did not process the request in time. Is a dialog open or a long operation running?");
            return job.Tcs.Task.Result;
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    public void Execute(UIApplication app)
    {
        while (_jobs.TryDequeue(out var job))
        {
            try { job.Tcs.TrySetResult(job.Work(app)); }
            catch (Exception ex)
            {
                Logging.Warning($"MCP tool failed: {ex.Message}");
                job.Tcs.TrySetException(ex);
            }
        }
    }

    public string GetName() => "AllO MCP Executor";
}

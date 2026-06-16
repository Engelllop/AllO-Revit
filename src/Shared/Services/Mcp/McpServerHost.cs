using System.Net;
using System.Text;
using AllO.Helpers;

namespace AllO.Services.Mcp;

/// <summary>
/// Servidor MCP local (Streamable HTTP, JSON-RPC 2.0) para conectar Claude Code / Claude Desktop
/// a Revit. Solo escucha en localhost; las tools se ejecutan en contexto Revit vía
/// <see cref="McpRevitExecutor"/>. Endpoint: POST http://localhost:{Port}/mcp
/// </summary>
public static class McpServerHost
{
    public const int BasePort = 48400;
    private const string ProtocolVersion = "2024-11-05";

    private static HttpListener? _listener;

    public static int Port { get; private set; }
    public static bool IsRunning => _listener is { IsListening: true };
    public static string Endpoint => $"http://localhost:{Port}/mcp";
    public static event Action? StatusChanged;

    public static string? Start()
    {
        if (IsRunning) return null;
        if (McpRevitExecutor.Instance == null)
            return "Internal error: the Revit executor was not initialized.";

        for (int port = BasePort; port < BasePort + 10; port++)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException)
            {
                listener.Close();
                continue;
            }

            _listener = listener;
            Port = port;
            Task.Run(AcceptLoop);
            Logging.Debug($"MCP server listening on {Endpoint}");
            StatusChanged?.Invoke();
            return null;
        }
        return $"No free port found in {BasePort}-{BasePort + 9}.";
    }

    public static void Stop()
    {
        var listener = _listener;
        _listener = null;
        if (listener != null)
        {
            try { listener.Stop(); listener.Close(); } catch { }
            Logging.Debug("MCP server stopped");
            try { StatusChanged?.Invoke(); } catch { }
        }
    }

    private static async Task AcceptLoop()
    {
        var listener = _listener;
        while (listener != null && listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
            catch { break; }
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.HttpMethod != "POST")
            {
                ctx.Response.StatusCode = 405;
                ctx.Response.Close();
                return;
            }

            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            if (JsonLite.Parse(body) is not Dictionary<string, object?> request)
            {
                WriteJson(ctx, 400, RpcError(null, -32700, "Parse error"));
                return;
            }

            request.TryGetValue("id", out var id);
            string method = request.TryGetValue("method", out var m) ? m?.ToString() ?? "" : "";

            // Notificaciones (sin id) no llevan respuesta JSON-RPC.
            if (!request.ContainsKey("id") || method.StartsWith("notifications/", StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = 202;
                ctx.Response.Close();
                return;
            }

            var args = request.TryGetValue("params", out var p) && p is Dictionary<string, object?> pd
                ? pd
                : new Dictionary<string, object?>();

            object? response = method switch
            {
                "initialize" => RpcResult(id, new Dictionary<string, object?>
                {
                    ["protocolVersion"] = args.TryGetValue("protocolVersion", out var pv) && pv is string s && s.Length > 0
                        ? s : ProtocolVersion,
                    ["capabilities"] = new Dictionary<string, object?>
                    {
                        ["tools"] = new Dictionary<string, object?>()
                    },
                    ["serverInfo"] = new Dictionary<string, object?>
                    {
                        ["name"] = "AllO Revit",
                        ["version"] = VersionInfo.Short
                    }
                }),
                "ping" => RpcResult(id, new Dictionary<string, object?>()),
                "tools/list" => RpcResult(id, new Dictionary<string, object?>
                {
                    ["tools"] = McpTools.ListToolDefs()
                }),
                "tools/call" => CallTool(id, args),
                _ => RpcError(id, -32601, $"Method '{method}' not found")
            };

            WriteJson(ctx, 200, response);
        }
        catch (Exception ex)
        {
            try { WriteJson(ctx, 500, RpcError(null, -32603, ex.Message)); }
            catch { }
        }
    }

    private static object CallTool(object? id, Dictionary<string, object?> args)
    {
        string name = args.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
        var toolArgs = args.TryGetValue("arguments", out var a) && a is Dictionary<string, object?> ad
            ? ad
            : new Dictionary<string, object?>();

        try
        {
            var executor = McpRevitExecutor.Instance
                ?? throw new InvalidOperationException("Revit executor not available.");
            object? result = executor.Run(app => McpTools.Call(name, toolArgs, app));
            return RpcResult(id, new Dictionary<string, object?>
            {
                ["content"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["type"] = "text", ["text"] = JsonLite.Serialize(result) }
                },
                ["isError"] = false
            });
        }
        catch (Exception ex)
        {
            return RpcResult(id, new Dictionary<string, object?>
            {
                ["content"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["type"] = "text", ["text"] = $"Error: {ex.Message}" }
                },
                ["isError"] = true
            });
        }
    }

    private static Dictionary<string, object?> RpcResult(object? id, object? result)
        => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };

    private static Dictionary<string, object?> RpcError(object? id, int code, string message)
        => new()
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new Dictionary<string, object?> { ["code"] = code, ["message"] = message }
        };

    private static void WriteJson(HttpListenerContext ctx, int status, object? payload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonLite.Serialize(payload));
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.Close();
    }
}

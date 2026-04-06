using System.Diagnostics;

namespace AllO.Helpers;

/// <summary>
/// Centralized logging utilities for AllO add-in.
/// All debug and error messages should go through this class.
/// </summary>
public static class Logging
{
    private const string Prefix = "[AllO]";
    private static readonly bool EnableDebug = true;

    /// <summary>
    /// Logs a debug message (only in debug builds or when enabled).
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        if (EnableDebug)
        {
            var fullMessage = string.IsNullOrEmpty(caller) 
                ? $"{Prefix} {message}" 
                : $"{Prefix} [{caller}] {message}";
            System.Diagnostics.Debug.WriteLine(fullMessage);
        }
    }

    /// <summary>
    /// Logs an error message with optional exception details.
    /// </summary>
    public static void Error(string message, Exception? ex = null, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        var fullMessage = string.IsNullOrEmpty(caller)
            ? $"{Prefix} ERROR: {message}"
            : $"{Prefix} ERROR [{caller}]: {message}";

        if (ex != null)
        {
            fullMessage += $"\nException: {ex.GetType().Name}: {ex.Message}\nStack: {ex.StackTrace}";
        }

        System.Diagnostics.Debug.WriteLine(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void Warning(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        var fullMessage = string.IsNullOrEmpty(caller)
            ? $"{Prefix} WARNING: {message}"
            : $"{Prefix} WARNING [{caller}]: {message}";

        System.Diagnostics.Debug.WriteLine(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    /// <summary>
    /// Logs operation timing information.
    /// </summary>
    public static void OperationComplete(string operation, TimeSpan elapsed, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        Debug($"{operation} completed in {elapsed.TotalMilliseconds:F2}ms", caller);
    }
}

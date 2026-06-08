using System.Collections.Concurrent;

namespace AllO.Core;

/// <summary>
/// Contenedor DI ligero. No queremos arrastrar Microsoft.Extensions.DependencyInjection
/// para mantener net48 limpio y evitar conflictos con Revit.
/// Soporta singletons (instancia o factory perezosa). Thread-safe.
/// </summary>
public static class ServiceLocator
{
    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();
    private static readonly ConcurrentDictionary<Type, object> Singletons = new();

    public static void RegisterSingleton<T>(T instance) where T : class
    {
        Singletons[typeof(T)] = instance;
    }

    public static void RegisterSingleton<T>(Func<T> factory) where T : class
    {
        Factories[typeof(T)] = () => factory();
        Singletons.TryRemove(typeof(T), out _);
    }

    public static T Resolve<T>() where T : class
    {
        var t = typeof(T);
        if (Singletons.TryGetValue(t, out var existing))
            return (T)existing;

        if (Factories.TryGetValue(t, out var factory))
        {
            // Factory registrada como singleton perezoso: se cachea en la primera resolución.
            var created = (T)factory();
            Singletons[t] = created;
            return created;
        }

        throw new InvalidOperationException($"ServiceLocator: tipo '{t.FullName}' no registrado.");
    }

    public static T? TryResolve<T>() where T : class
    {
        try { return Resolve<T>(); }
        catch { return null; }
    }

    public static void Reset()
    {
        Factories.Clear();
        Singletons.Clear();
    }
}

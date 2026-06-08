using AllO.Core;
using Xunit;

namespace AllO.Tests;

public class ServiceLocatorTests : IDisposable
{
    public ServiceLocatorTests() => ServiceLocator.Reset();
    public void Dispose() => ServiceLocator.Reset();

    private sealed class Foo { public int N; }

    [Fact]
    public void RegisterSingleton_Instancia_DevuelveLaMisma()
    {
        var foo = new Foo { N = 7 };
        ServiceLocator.RegisterSingleton(foo);
        Assert.Same(foo, ServiceLocator.Resolve<Foo>());
        Assert.Same(ServiceLocator.Resolve<Foo>(), ServiceLocator.Resolve<Foo>());
    }

    [Fact]
    public void RegisterSingleton_Factory_EsPerezosaYSeCachea()
    {
        int calls = 0;
        ServiceLocator.RegisterSingleton(() => { calls++; return new Foo(); });
        Assert.Equal(0, calls); // perezosa: no se invoca al registrar

        var a = ServiceLocator.Resolve<Foo>();
        var b = ServiceLocator.Resolve<Foo>();
        Assert.Same(a, b);
        Assert.Equal(1, calls); // cacheada tras la 1ª resolución
    }

    [Fact]
    public void Resolve_NoRegistrado_Lanza()
        => Assert.Throws<InvalidOperationException>(() => ServiceLocator.Resolve<Foo>());

    [Fact]
    public void TryResolve_NoRegistrado_DevuelveNull()
        => Assert.Null(ServiceLocator.TryResolve<Foo>());

    [Fact]
    public void Reset_LimpiaRegistros()
    {
        ServiceLocator.RegisterSingleton(new Foo());
        ServiceLocator.Reset();
        Assert.Null(ServiceLocator.TryResolve<Foo>());
    }
}

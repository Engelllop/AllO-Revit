using AllO.Services;
using Xunit;

namespace AllO.Tests;

public class MockServiceTests
{
    private readonly MockService _svc = new();

    [Fact]
    public void IsRevitSessionActive_EsFalse()
        => Assert.False(_svc.IsRevitSessionActive);

    [Fact]
    public void GetDocumentName_IndicaMock()
        => Assert.Contains("Mock", _svc.GetDocumentName());

    [Fact]
    public void GetAllSheets_DevuelveSetConIdsUnicos()
    {
        var sheets = _svc.GetAllSheets();
        Assert.NotEmpty(sheets);
        Assert.Equal(sheets.Count, sheets.Select(s => s.ElementId).Distinct().Count());
        Assert.All(sheets, s => Assert.False(string.IsNullOrWhiteSpace(s.SheetNumber)));
    }

    [Fact]
    public void CreateSheets_DevuelveUnIdPorRequest()
    {
        var reqs = new List<SheetCreateRequest> { new(), new(), new() };
        var ids = _svc.CreateSheets(1, reqs);
        Assert.Equal(reqs.Count, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void OperacionesPorLote_DevuelvenElConteoDeEntrada()
    {
        Assert.Equal(3, _svc.DeleteSheets(new List<int> { 1, 2, 3 }));
        Assert.Equal(2, _svc.RenameSheets(new Dictionary<int, string> { [1] = "a", [2] = "b" }));
        Assert.Equal(2, _svc.DeleteViews(new List<int> { 10, 11 }));
    }

    [Fact]
    public void GetSheetsForPublish_MapeaDesdeSheets()
    {
        var sheets = _svc.GetAllSheets();
        var pub = _svc.GetSheetsForPublish();
        Assert.Equal(sheets.Count, pub.Count);
        Assert.Equal(sheets.Select(s => s.ElementId), pub.Select(p => p.ElementId));
        Assert.All(pub, p => Assert.True(p.ParameterValues.ContainsKey("SISTEMA")));
    }

    [Fact]
    public void GetLinkDisplayState_DevuelveElIdSolicitado()
    {
        var state = _svc.GetLinkDisplayState(9001, 201);
        Assert.Equal(9001, state.LinkInstanceId);
        Assert.False(string.IsNullOrEmpty(state.DisplayMode));
    }
}

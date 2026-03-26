using AllO.Models;

namespace AllO.Services;

/// <summary>
/// Full mock for design-time and testing without Revit.
/// </summary>
public class MockService : IRevitService
{
    public bool IsRevitSessionActive => false;
    public string GetDocumentName() => "Modern_House_v3.rvt (Mock)";

    private int _nextId = 500;

    public List<SheetInfo> GetAllSheets()
    {
        return new List<SheetInfo>
        {
            new() { ElementId = 101, SheetNumber = "A101", OriginalName = "Architectural Floor Plans",         PreviewName = "Architectural Floor Plans",         TitleBlockTypeId = 1, TitleBlockName = "A1 Metric", ApprovedBy = "J. Smith", DesignedBy = "A. Davis", SheetIssueDate = "2025-01-15" },
            new() { ElementId = 102, SheetNumber = "A102", OriginalName = "Architectural Floor Plans L2",      PreviewName = "Architectural Floor Plans L2",      TitleBlockTypeId = 1, TitleBlockName = "A1 Metric", ApprovedBy = "J. Smith", DesignedBy = "A. Davis", SheetIssueDate = "2025-01-15" },
            new() { ElementId = 103, SheetNumber = "A201", OriginalName = "Architectural Building Elevations", PreviewName = "Architectural Building Elevations", TitleBlockTypeId = 1, TitleBlockName = "A1 Metric", ApprovedBy = "", DesignedBy = "A. Davis", SheetIssueDate = "" },
            new() { ElementId = 104, SheetNumber = "A301", OriginalName = "Architectural Building Sections",   PreviewName = "Architectural Building Sections",   TitleBlockTypeId = 1, TitleBlockName = "A1 Metric", ApprovedBy = "", DesignedBy = "M. Johnson", SheetIssueDate = "" },
            new() { ElementId = 105, SheetNumber = "S101", OriginalName = "Structural Floor Plans",            PreviewName = "Structural Floor Plans",            TitleBlockTypeId = 2, TitleBlockName = "A0 Metric", ApprovedBy = "", DesignedBy = "M. Johnson", SheetIssueDate = "" },
            new() { ElementId = 106, SheetNumber = "E101", OriginalName = "Electrical Floor Plans",            PreviewName = "Electrical Floor Plans",            TitleBlockTypeId = 2, TitleBlockName = "A0 Metric", ApprovedBy = "", DesignedBy = "", SheetIssueDate = "" },
            new() { ElementId = 107, SheetNumber = "G001", OriginalName = "Cover Sheet",                       PreviewName = "Cover Sheet",                       TitleBlockTypeId = 1, TitleBlockName = "A1 Metric", ApprovedBy = "J. Smith", DesignedBy = "A. Davis", SheetIssueDate = "2025-01-15" },
        };
    }

    public List<TitleBlockInfo> GetTitleBlocks()
    {
        return new List<TitleBlockInfo>
        {
            new() { TypeId = 1, FamilyName = "A1 Metric", TypeName = "A1 Metric" },
            new() { TypeId = 2, FamilyName = "A0 Metric", TypeName = "A0 Metric" },
            new() { TypeId = 3, FamilyName = "A2 Metric", TypeName = "A2 Metric" },
            new() { TypeId = 4, FamilyName = "A3 Metric", TypeName = "A3 Metric" },
        };
    }

    public int RenameSheets(Dictionary<int, string> renames) => renames.Count;
    public int RenumberSheets(Dictionary<int, string> renumbers) => renumbers.Count;
    public int DeleteSheets(List<int> elementIds) => elementIds.Count;
    public int DuplicateSheets(List<int> elementIds) => elementIds.Count;
    public int ChangeTitleBlock(int sheetElementId, int newTitleBlockTypeId) => 1;

    public List<int> CreateSheets(int titleBlockTypeId, List<SheetCreateRequest> requests)
    {
        var ids = new List<int>();
        foreach (var r in requests) ids.Add(_nextId++);
        return ids;
    }

    public List<ViewInfo> GetAllViews()
    {
        return new List<ViewInfo>
        {
            new() { ElementId = 201, Name = "Level 1 - Floor Plan",    ViewType = "FloorPlan",     Discipline = "Architectural", Scale = "1:100", ViewTemplateName = "Arch Plan", SheetNumber = "A101" },
            new() { ElementId = 202, Name = "Level 2 - Floor Plan",    ViewType = "FloorPlan",     Discipline = "Architectural", Scale = "1:100", ViewTemplateName = "Arch Plan", SheetNumber = "A102" },
            new() { ElementId = 203, Name = "North Elevation",         ViewType = "Elevation",     Discipline = "Architectural", Scale = "1:100", ViewTemplateName = "Arch Elevation", SheetNumber = "A201" },
            new() { ElementId = 204, Name = "Section A-A",             ViewType = "Section",       Discipline = "Architectural", Scale = "1:50",  ViewTemplateName = "Arch Section", SheetNumber = "A301" },
            new() { ElementId = 205, Name = "3D View - Exterior",      ViewType = "ThreeD",        Discipline = "",              Scale = "",      ViewTemplateName = "", SheetNumber = "" },
            new() { ElementId = 206, Name = "Structural Level 1",      ViewType = "FloorPlan",     Discipline = "Structural",    Scale = "1:100", ViewTemplateName = "Str Plan", SheetNumber = "S101" },
            new() { ElementId = 207, Name = "Electrical Level 1",      ViewType = "FloorPlan",     Discipline = "Electrical",    Scale = "1:100", ViewTemplateName = "", SheetNumber = "E101" },
            new() { ElementId = 208, Name = "Door Schedule",           ViewType = "Schedule",      Discipline = "Architectural", Scale = "",      ViewTemplateName = "", SheetNumber = "" },
        };
    }

    public int DeleteViews(List<int> elementIds) => elementIds.Count;
    public int RenameViews(Dictionary<int, string> renames) => renames.Count;

    public List<RevisionInfo> GetAllRevisions()
    {
        return new List<RevisionInfo>
        {
            new() { ElementId = 301, Sequence = 1, Date = "2025-01-15", Description = "Initial Issue",        IssuedBy = "J. Smith", IssuedTo = "Client",     Status = "Issued" },
            new() { ElementId = 302, Sequence = 2, Date = "2025-03-01", Description = "Design Development",   IssuedBy = "J. Smith", IssuedTo = "Client",     Status = "Issued" },
            new() { ElementId = 303, Sequence = 3, Date = "2025-07-20", Description = "MEP Coordination",     IssuedBy = "A. Davis", IssuedTo = "Contractor", Status = "Not Issued" },
        };
    }

    public int CreateRevision(string date, string description, string issuedBy, string issuedTo) => _nextId++;
    public int DeleteRevisions(List<int> elementIds) => elementIds.Count;
    public int UpdateRevision(int elementId, string date, string description, string issuedBy, string issuedTo) => 1;
    public int ToggleRevisionIssued(int elementId) => 1;

    public List<PublishSheetItem> GetSheetsForPublish()
    {
        return GetAllSheets().Select(s => new PublishSheetItem
        {
            ElementId = s.ElementId,
            SheetNumber = s.SheetNumber,
            SheetName = s.OriginalName,
            IsSelected = true,
            Status = "Pending"
        }).ToList();
    }

    public bool ExportSingleToPdf(int sheetElementId, string outputFolder, string namingPattern, bool combinePdf = false) => true;
    public bool ExportSingleToDwg(int sheetElementId, string outputFolder, string namingPattern) => true;

    // -- CopyCrop ---
    public List<CropViewInfo> GetCroppableViews() => new()
    {
        new() { ElementId = 201, Name = "Level 1 - Floor Plan",  ViewType = "FloorPlan", HasCropRegion = true,  IsCropActive = true },
        new() { ElementId = 202, Name = "Level 2 - Floor Plan",  ViewType = "FloorPlan", HasCropRegion = true,  IsCropActive = true },
        new() { ElementId = 203, Name = "North Elevation",       ViewType = "Elevation", HasCropRegion = true,  IsCropActive = false },
        new() { ElementId = 204, Name = "Section A-A",           ViewType = "Section",   HasCropRegion = false, IsCropActive = false },
        new() { ElementId = 206, Name = "Structural Level 1",    ViewType = "FloorPlan", HasCropRegion = true,  IsCropActive = true },
    };
    public int CopyCropRegion(int sourceViewId, List<int> targetViewIds) => targetViewIds.Count;

    // -- Families ---
    public List<FamilyInfo> GetAllFamilies() => new()
    {
        new() { ElementId = 401, FamilyName = "M_Single-Flush",    Category = "Doors",     TypeCount = 3, InstanceCount = 24, IsInPlace = false },
        new() { ElementId = 402, FamilyName = "M_Fixed",           Category = "Windows",   TypeCount = 5, InstanceCount = 18, IsInPlace = false },
        new() { ElementId = 403, FamilyName = "Basic Wall Chair",  Category = "Furniture", TypeCount = 1, InstanceCount = 0,  IsInPlace = false },
        new() { ElementId = 404, FamilyName = "Custom Reception",  Category = "Furniture", TypeCount = 1, InstanceCount = 2,  IsInPlace = true },
        new() { ElementId = 405, FamilyName = "Round Column",      Category = "Columns",   TypeCount = 2, InstanceCount = 12, IsInPlace = false },
        new() { ElementId = 406, FamilyName = "Old Fixture",       Category = "Plumbing",  TypeCount = 1, InstanceCount = 0,  IsInPlace = false },
    };
    public int DeleteFamilies(List<int> familyIds) => familyIds.Count;
    public int GetFamilyInstanceCount(int familyId) => 0;

    // -- Grids ---
    public List<GridInfo> GetAllGrids() => new()
    {
        new() { ElementId = 501, Name = "1",  Orientation = "Vertical",   Length = 25.0 },
        new() { ElementId = 502, Name = "2",  Orientation = "Vertical",   Length = 25.0 },
        new() { ElementId = 503, Name = "3",  Orientation = "Vertical",   Length = 25.0 },
        new() { ElementId = 504, Name = "A",  Orientation = "Horizontal", Length = 30.0 },
        new() { ElementId = 505, Name = "B",  Orientation = "Horizontal", Length = 30.0 },
        new() { ElementId = 506, Name = "C",  Orientation = "Horizontal", Length = 30.0 },
    };
    public int RenameGrids(Dictionary<int, string> renames) => renames.Count;
    public int DeleteGrids(List<int> elementIds) => elementIds.Count;

    // -- Levels ---
    public List<LevelInfo> GetAllLevels() => new()
    {
        new() { ElementId = 601, Name = "Level B1",  Elevation = -3.0,  IsStructural = true },
        new() { ElementId = 602, Name = "Level 0",   Elevation = 0.0,   IsStructural = true },
        new() { ElementId = 603, Name = "Level 1",   Elevation = 3.5,   IsStructural = false },
        new() { ElementId = 604, Name = "Level 2",   Elevation = 7.0,   IsStructural = false },
        new() { ElementId = 605, Name = "Roof",      Elevation = 10.5,  IsStructural = false },
    };
    public int RenameLevels(Dictionary<int, string> renames) => renames.Count;
    public int MoveLevels(Dictionary<int, double> newElevations) => newElevations.Count;
    public int DeleteLevels(List<int> elementIds) => elementIds.Count;

    // -- Align ---
    public List<AlignableElementInfo> GetSelectedElements() => new()
    {
        new() { ElementId = 701, Name = "Wall 1",     Category = "Walls",   X = 0,  Y = 5,  Z = 0 },
        new() { ElementId = 702, Name = "Wall 2",     Category = "Walls",   X = 10, Y = 3,  Z = 0 },
        new() { ElementId = 703, Name = "Column A1",  Category = "Columns", X = 0,  Y = 0,  Z = 0 },
        new() { ElementId = 704, Name = "Column B1",  Category = "Columns", X = 10, Y = 0,  Z = 0 },
        new() { ElementId = 705, Name = "Door 101",   Category = "Doors",   X = 5,  Y = 5,  Z = 0 },
    };
    public int AlignElements(List<int> elementIds, string alignmentMode) => elementIds.Count;
    public int AlignToGrid(List<int> elementIds, int gridId) => elementIds.Count;
    public int AlignToLevel(List<int> elementIds, int levelId) => elementIds.Count;
    public int DistributeElements(List<int> elementIds, string direction) => elementIds.Count;

    // -- Connector ---
    public List<DisconnectedConnectorInfo> FindDisconnectedElements() => new()
    {
        new() { ElementId = 801, ElementName = "Pipe 1",      Category = "Pipes",      SystemType = "Domestic Hot Water", DisconnectedCount = 1, X = 5,  Y = 10, Z = 3 },
        new() { ElementId = 802, ElementName = "Duct 1",      Category = "Ducts",       SystemType = "Supply Air",        DisconnectedCount = 2, X = 15, Y = 8,  Z = 3 },
        new() { ElementId = 803, ElementName = "Cable Tray 1", Category = "Cable Trays", SystemType = "Power",            DisconnectedCount = 1, X = 20, Y = 12, Z = 3.5 },
        new() { ElementId = 804, ElementName = "Pipe 2",      Category = "Pipes",      SystemType = "Sanitary",          DisconnectedCount = 1, X = 5,  Y = 12, Z = 0 },
    };
    public int AutoConnectNearby(double toleranceFeet) => 3;
    public int ConnectElements(int elementId1, int elementId2) => 1;
    public int HighlightElement(int elementId) => 1;

    // -- QuickSearch ---
    public List<SearchResultInfo> SearchElements(string query, bool byName, bool byId, bool byCategory, bool byFamily, bool byParameter)
    {
        var all = new List<SearchResultInfo>
        {
            new() { ElementId = 101, Name = "Basic Wall",       Category = "Walls",    FamilyType = "Generic - 200mm", LevelName = "Level 1" },
            new() { ElementId = 102, Name = "Floor Standard",   Category = "Floors",   FamilyType = "Generic 150mm",   LevelName = "Level 1" },
            new() { ElementId = 201, Name = "M_Single-Flush",   Category = "Doors",    FamilyType = "0915 x 2134mm",   LevelName = "Level 1" },
            new() { ElementId = 202, Name = "M_Fixed",          Category = "Windows",  FamilyType = "0610 x 1220mm",   LevelName = "Level 2" },
            new() { ElementId = 301, Name = "Desk",             Category = "Furniture", FamilyType = "1525 x 762mm",   LevelName = "Level 1" },
            new() { ElementId = 302, Name = "Chair-Task",       Category = "Furniture", FamilyType = "Standard",       LevelName = "Level 1" },
            new() { ElementId = 401, Name = "Round Column",     Category = "Columns",  FamilyType = "450mm",           LevelName = "Level 0" },
            new() { ElementId = 501, Name = "Pipe 1",           Category = "Pipes",    FamilyType = "Standard",        LevelName = "Level 1" },
        };
        if (string.IsNullOrWhiteSpace(query)) return all;
        return all.Where(r =>
            (byName && r.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (byId && r.ElementId.ToString().Contains(query)) ||
            (byCategory && r.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (byFamily && r.FamilyType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        ).Select(r => { r.MatchedOn = byName ? "Name" : byId ? "ID" : byCategory ? "Category" : "Family"; return r; }).ToList();
    }
    public int SelectElements(List<int> elementIds) => elementIds.Count;
    public int IsolateElements(List<int> elementIds) => elementIds.Count;

    // -- TableGen (Excel → Revit) ---
    public List<ExistingTableViewInfo> GetExistingTableViews() => new()
    {
        new() { ElementId = 901, Name = "Cuadro Cargas - Sheet1", ExcelPath = @"C:\Data\CuadroCargas.xlsx", SheetName = "Sheet1", Range = "A1:G20" },
        new() { ElementId = 902, Name = "Tabla Ductos - Cálculos", ExcelPath = @"C:\Data\Ductos.xlsx", SheetName = "Cálculos", Range = "A1:E15" },
    };
    public int ImportExcelAsTable(ExcelTableData data, string viewName, string viewType) => 1;
    public int ReloadTableView(int viewId, ExcelTableData data) => 1;
    public int DeleteTableViews(List<int> viewIds) => viewIds.Count;
}

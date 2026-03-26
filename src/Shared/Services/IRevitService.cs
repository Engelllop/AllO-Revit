using AllO.Models;

namespace AllO.Services;

/// <summary>
/// Contract that abstracts all Revit operations
/// needed for Sheet Manager, View Manager, and Revisions.
/// </summary>
public interface IRevitService
{
    bool IsRevitSessionActive { get; }
    string GetDocumentName();

    // -- Sheets ------------------------------------------------
    List<SheetInfo> GetAllSheets();
    int RenameSheets(Dictionary<int, string> renames);
    int RenumberSheets(Dictionary<int, string> renumbers);
    int DeleteSheets(List<int> elementIds);
    List<int> CreateSheets(int titleBlockTypeId, List<SheetCreateRequest> requests);
    int DuplicateSheets(List<int> elementIds);
    int ChangeTitleBlock(int sheetElementId, int newTitleBlockTypeId);

    // -- TitleBlocks -------------------------------------------
    List<TitleBlockInfo> GetTitleBlocks();

    // -- Views -------------------------------------------------
    List<ViewInfo> GetAllViews();
    int DeleteViews(List<int> elementIds);
    int RenameViews(Dictionary<int, string> renames);

    // -- Revisions ---------------------------------------------
    List<RevisionInfo> GetAllRevisions();
    int CreateRevision(string date, string description, string issuedBy, string issuedTo);
    int DeleteRevisions(List<int> elementIds);
    int UpdateRevision(int elementId, string date, string description, string issuedBy, string issuedTo);
    int ToggleRevisionIssued(int elementId);

    // -- Publishing / Export ------------------------------------
    List<PublishSheetItem> GetSheetsForPublish();
    bool ExportSingleToPdf(int sheetElementId, string outputFolder, string namingPattern, bool combinePdf = false);
    bool ExportSingleToDwg(int sheetElementId, string outputFolder, string namingPattern);

    // -- CopyCrop ---------------------------------------------
    List<CropViewInfo> GetCroppableViews();
    int CopyCropRegion(int sourceViewId, List<int> targetViewIds);

    // -- Families ---------------------------------------------
    List<FamilyInfo> GetAllFamilies();
    int DeleteFamilies(List<int> familyIds);
    int GetFamilyInstanceCount(int familyId);

    // -- Grids ------------------------------------------------
    List<GridInfo> GetAllGrids();
    int RenameGrids(Dictionary<int, string> renames);
    int DeleteGrids(List<int> elementIds);

    // -- Levels -----------------------------------------------
    List<LevelInfo> GetAllLevels();
    int RenameLevels(Dictionary<int, string> renames);
    int MoveLevels(Dictionary<int, double> newElevations);
    int DeleteLevels(List<int> elementIds);

    // -- Align ------------------------------------------------
    List<AlignableElementInfo> GetSelectedElements();
    int AlignElements(List<int> elementIds, string alignmentMode);  // Left, Right, Center, Top, Bottom, Middle
    int AlignToGrid(List<int> elementIds, int gridId);
    int AlignToLevel(List<int> elementIds, int levelId);
    int DistributeElements(List<int> elementIds, string direction);  // Horizontal, Vertical

    // -- Connector (MEP) --------------------------------------
    List<DisconnectedConnectorInfo> FindDisconnectedElements();
    int AutoConnectNearby(double toleranceFeet);
    int ConnectElements(int elementId1, int elementId2);
    int HighlightElement(int elementId);

    // -- QuickSearch --------------------------------------------
    List<SearchResultInfo> SearchElements(string query, bool byName, bool byId, bool byCategory, bool byFamily, bool byParameter);
    int SelectElements(List<int> elementIds);
    int IsolateElements(List<int> elementIds);

    // -- TableGen (Excel → Revit) --------------------------------
    List<ExistingTableViewInfo> GetExistingTableViews();
    int ImportExcelAsTable(ExcelTableData data, string viewName, string viewType);
    int ReloadTableView(int viewId, ExcelTableData data);
    int DeleteTableViews(List<int> viewIds);
}

/// <summary>
/// Request to create a new sheet.
/// </summary>
public class SheetCreateRequest
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

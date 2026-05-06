using Autodesk.Revit.DB;

namespace AllO.Helpers;

/// <summary>
/// Atajos para los BuiltInParameter más usados en AllO.
/// Centralizar aquí evita repetir el enum largo y facilita renombrados.
/// </summary>
public static class BuiltInParams
{
    // -- Sheets ---------------------------------------------------------
    public const BuiltInParameter SheetNumber  = BuiltInParameter.SHEET_NUMBER;
    public const BuiltInParameter SheetName    = BuiltInParameter.SHEET_NAME;
    public const BuiltInParameter SheetIssued  = BuiltInParameter.SHEET_ISSUE_DATE;
    public const BuiltInParameter SheetCurrentRev      = BuiltInParameter.SHEET_CURRENT_REVISION;
    public const BuiltInParameter SheetCurrentRevDate  = BuiltInParameter.SHEET_CURRENT_REVISION_DATE;
    public const BuiltInParameter SheetCurrentRevDesc  = BuiltInParameter.SHEET_CURRENT_REVISION_DESCRIPTION;

    // -- Views ----------------------------------------------------------
    public const BuiltInParameter ViewName        = BuiltInParameter.VIEW_NAME;
    public const BuiltInParameter ViewType        = BuiltInParameter.VIEW_TYPE;
    public const BuiltInParameter ViewScale       = BuiltInParameter.VIEW_SCALE;
    public const BuiltInParameter ViewTemplate    = BuiltInParameter.VIEW_TEMPLATE;
    public const BuiltInParameter ViewFamilyName  = BuiltInParameter.VIEW_FAMILY;

    // -- Datum ----------------------------------------------------------
    public const BuiltInParameter DatumName       = BuiltInParameter.DATUM_TEXT;
    public const BuiltInParameter LevelElevation  = BuiltInParameter.LEVEL_ELEV;

    // -- Family / Type --------------------------------------------------
    public const BuiltInParameter FamilyName      = BuiltInParameter.ALL_MODEL_FAMILY_NAME;
    public const BuiltInParameter TypeName        = BuiltInParameter.ALL_MODEL_TYPE_NAME;
    public const BuiltInParameter TypeMark        = BuiltInParameter.ALL_MODEL_TYPE_MARK;
    public const BuiltInParameter Mark            = BuiltInParameter.ALL_MODEL_MARK;

    // -- Revisions ------------------------------------------------------
    public const BuiltInParameter RevDate     = BuiltInParameter.PROJECT_REVISION_REVISION_DATE;
    public const BuiltInParameter RevDesc     = BuiltInParameter.PROJECT_REVISION_REVISION_DESCRIPTION;
    public const BuiltInParameter RevIssuedBy = BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_BY;
    public const BuiltInParameter RevIssuedTo = BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_TO;
    public const BuiltInParameter RevIssued   = BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED;
}

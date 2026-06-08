using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class FamilyExportCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // Ask for destination folder
            var folder = InputDialog.Show("Family Export", "Enter full path to destination folder:",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (string.IsNullOrWhiteSpace(folder)) return Result.Cancelled;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var families = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .Where(f => !f.IsInPlace) // skip in-place families
                .ToList();

            int exported = 0;
            int failed = 0;

            foreach (var family in families)
            {
                try
                {
                    var familyDoc = doc.EditFamily(family);
                    if (familyDoc == null) continue;

                    string fileName = $"{family.Name}.rfa";
                    // Sanitize filename
                    foreach (char c in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(c, '_');

                    string filePath = Path.Combine(folder, fileName);
                    familyDoc.SaveAs(filePath, new SaveAsOptions { OverwriteExistingFile = true });
                    familyDoc.Close(false);
                    exported++;
                }
                catch
                {
                    failed++;
                }
            }

            TaskDialog.Show("Family Export", $"Exported {exported} family(s).\nFailed: {failed}.");
            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}

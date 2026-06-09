using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;
using AllO.UI.Toast;

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
            var dialog = new AllO.UI.Views.FamilyExportWindow();
            if (dialog.ShowDialog() != true) return Result.Cancelled;
            var folder = dialog.SelectedFolder;
            if (string.IsNullOrWhiteSpace(folder)) return Result.Cancelled;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var families = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .Where(f => !f.IsInPlace) // skip in-place families
                .ToList();

            var sw = System.Diagnostics.Stopwatch.StartNew();
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

            Logging.OperationComplete($"Family Export ({exported} families)", sw.Elapsed);
            ToastHost.Show("Family Export",
                $"Exported {exported} family(s) in {sw.Elapsed.TotalSeconds:F1}s. Failed: {failed}.",
                failed > 0 ? ToastKind.Warning : ToastKind.Success);
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

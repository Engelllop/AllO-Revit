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
            // Selector de carpeta gráfico (truco SaveFileDialog: se toma su directorio).
            var picker = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select destination folder (just click Save)",
                FileName = "Select Folder",
                Filter = "Folder|*.folder",
                CheckFileExists = false,
                CheckPathExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (picker.ShowDialog() != true) return Result.Cancelled;
            var folder = Path.GetDirectoryName(picker.FileName);
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
            TaskDialog.Show("Family Export",
                $"Exported {exported} family(s) in {sw.Elapsed.TotalSeconds:F1}s.\nFailed: {failed}.");
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

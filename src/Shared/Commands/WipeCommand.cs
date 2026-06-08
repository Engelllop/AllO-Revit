using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Models;
using AllO.UI.ViewModels;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class WipeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var previewItems = GetCleanupPreview(doc);
            if (previewItems.Count == 0)
            {
                TaskDialog.Show("Wipe", "No unused items found in the document.");
                return Result.Succeeded;
            }

            var vm = new WipePreviewViewModel(previewItems);
            var window = new WipePreviewWindow(vm);
            var dialogResult = window.ShowDialogAndGetResult();

            if (dialogResult != true)
                return Result.Cancelled;

            var selectedCategories = vm.Items.Where(i => i.IsSelected).Select(i => i.Category).ToList();
            if (selectedCategories.Count == 0)
                return Result.Cancelled;

            var result = ExecuteCleanup(doc, selectedCategories);

            var report = $"Deleted:\n" +
                         $"  Views: {result.DeletedViews}\n" +
                         $"  Sheets: {result.DeletedSheets}\n" +
                         $"  View Templates: {result.DeletedTemplates}\n" +
                         $"  Filters: {result.DeletedFilters}\n" +
                         $"  Imports: {result.DeletedImports}\n" +
                         $"  Line Patterns: {result.DeletedLinePatterns}\n" +
                         $"  Fill Patterns: {result.DeletedFillPatterns}";

            TaskDialog.Show("Wipe - Cleanup Report", report);
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

    private static List<WipeItem> GetCleanupPreview(Document doc)
    {
        var items = new List<WipeItem>();

        // Views
        var viewsInUse = new HashSet<ElementId>();
        foreach (Viewport vp in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>())
            if (vp.ViewId != ElementId.InvalidElementId) viewsInUse.Add(vp.ViewId);

        var unusedViews = new FilteredElementCollector(doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => !v.IsTemplate && v.Id != doc.ActiveView.Id && !viewsInUse.Contains(v.Id) && !v.Name.Contains("{3D}"))
            .ToList();
        if (unusedViews.Count > 0)
            items.Add(new WipeItem { Category = "unusedviews", DisplayName = "Unused Views", Count = unusedViews.Count, IsSelected = true });

        // Sheets
        var allSheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
        var unusedSheets = allSheets.Where(s =>
            !new FilteredElementCollector(doc).OfClass(typeof(Viewport))
                .Cast<Viewport>().Any(vp => vp.SheetId == s.Id)).ToList();
        if (unusedSheets.Count > 0)
            items.Add(new WipeItem { Category = "unusedsheets", DisplayName = "Unused Sheets", Count = unusedSheets.Count, IsSelected = false });

        // View Templates
        var templatesInUse = new HashSet<ElementId>();
        foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
            if (view.ViewTemplateId != ElementId.InvalidElementId)
                templatesInUse.Add(view.ViewTemplateId);

        var unusedTemplates = new FilteredElementCollector(doc)
            .OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate && !templatesInUse.Contains(v.Id)).ToList();
        if (unusedTemplates.Count > 0)
            items.Add(new WipeItem { Category = "unusedviewtemplates", DisplayName = "Unused View Templates", Count = unusedTemplates.Count, IsSelected = false });

        // Filters
        var filtersInUse = new HashSet<ElementId>();
        foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
            foreach (ElementId id in view.GetFilters()) filtersInUse.Add(id);

        var unusedFilters = new FilteredElementCollector(doc)
            .OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>()
            .Where(f => !filtersInUse.Contains(f.Id)).ToList();
        if (unusedFilters.Count > 0)
            items.Add(new WipeItem { Category = "unusedfilters", DisplayName = "Unused Filters", Count = unusedFilters.Count, IsSelected = true });

        // Imports
        var imports = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().ToList();
        if (imports.Count > 0)
            items.Add(new WipeItem { Category = "unusedimports", DisplayName = "Import Instances", Count = imports.Count, IsSelected = true });

        // Line Patterns (CAD-like names only)
        var cadLinePatterns = new FilteredElementCollector(doc)
            .OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>()
            .Where(lp => lp.Name.Contains("Import") || lp.Name.Contains("DWG") || lp.Name.Contains("CAD")).ToList();
        if (cadLinePatterns.Count > 0)
            items.Add(new WipeItem { Category = "unusedlinepatterns", DisplayName = "CAD Line Patterns", Count = cadLinePatterns.Count, IsSelected = false });

        // Fill Patterns (CAD-like names only)
        var cadFillPatterns = new FilteredElementCollector(doc)
            .OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
            .Where(fp => fp.Name.Contains("Import") || fp.Name.Contains("DWG") || fp.Name.Contains("CAD")).ToList();
        if (cadFillPatterns.Count > 0)
            items.Add(new WipeItem { Category = "unusedfillpatterns", DisplayName = "CAD Fill Patterns", Count = cadFillPatterns.Count, IsSelected = false });

        return items;
    }

    private static CleanupResult ExecuteCleanup(Document doc, List<string> categories)
    {
        var result = new CleanupResult();
        using var tx = new Transaction(doc, "AllO - Wipe Cleanup");
        tx.Start();

        if (categories.Contains("unusedviews"))
        {
            var viewsInUse = new HashSet<ElementId>();
            foreach (Viewport vp in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>())
                if (vp.ViewId != ElementId.InvalidElementId) viewsInUse.Add(vp.ViewId);

            var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate && v.Id != doc.ActiveView.Id && !viewsInUse.Contains(v.Id) && !v.Name.Contains("{3D}")).ToList();
            foreach (var v in views)
            {
                try { doc.Delete(v.Id); result.DeletedViews++; }
                catch { }
            }
        }

        if (categories.Contains("unusedsheets"))
        {
            var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            foreach (var sheet in sheets)
            {
                var vps = new FilteredElementCollector(doc).OfClass(typeof(Viewport))
                    .Cast<Viewport>().Where(vp => vp.SheetId == sheet.Id);
                if (!vps.Any())
                {
                    try { doc.Delete(sheet.Id); result.DeletedSheets++; }
                    catch { }
                }
            }
        }

        if (categories.Contains("unusedviewtemplates"))
        {
            var templatesInUse = new HashSet<ElementId>();
            foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
                if (view.ViewTemplateId != ElementId.InvalidElementId)
                    templatesInUse.Add(view.ViewTemplateId);

            var templates = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => v.IsTemplate && !templatesInUse.Contains(v.Id)).ToList();
            foreach (var t in templates)
            {
                try { doc.Delete(t.Id); result.DeletedTemplates++; }
                catch { }
            }
        }

        if (categories.Contains("unusedfilters"))
        {
            var filtersInUse = new HashSet<ElementId>();
            foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
                foreach (ElementId id in view.GetFilters()) filtersInUse.Add(id);

            var filters = new FilteredElementCollector(doc)
                .OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>()
                .Where(f => !filtersInUse.Contains(f.Id)).ToList();
            foreach (var f in filters)
            {
                try { doc.Delete(f.Id); result.DeletedFilters++; }
                catch { }
            }
        }

        if (categories.Contains("unusedimports"))
        {
            var imports = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().ToList();
            foreach (var imp in imports)
            {
                try { doc.Delete(imp.Id); result.DeletedImports++; }
                catch { }
            }
        }

        if (categories.Contains("unusedlinepatterns"))
        {
            var linePatterns = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>()
                .Where(lp => lp.Name.Contains("Import") || lp.Name.Contains("DWG") || lp.Name.Contains("CAD")).ToList();
            foreach (var lp in linePatterns)
            {
                try { doc.Delete(lp.Id); result.DeletedLinePatterns++; }
                catch { }
            }
        }

        if (categories.Contains("unusedfillpatterns"))
        {
            var fillPatterns = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
                .Where(fp => fp.Name.Contains("Import") || fp.Name.Contains("DWG") || fp.Name.Contains("CAD")).ToList();
            foreach (var fp in fillPatterns)
            {
                try { doc.Delete(fp.Id); result.DeletedFillPatterns++; }
                catch { }
            }
        }

        tx.Commit();
        return result;
    }

    private class CleanupResult
    {
        public int DeletedViews;
        public int DeletedSheets;
        public int DeletedTemplates;
        public int DeletedFilters;
        public int DeletedImports;
        public int DeletedLinePatterns;
        public int DeletedFillPatterns;
    }
}

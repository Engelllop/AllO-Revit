using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;
using AllO.UI.ViewModels;
using AllO.UI.Views;
using AllO.UI.Toast;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ViewManagerCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new LevelOption { Id = l.Id.Value, Name = l.Name })
                .ToList();

            var templates = new List<NamedOption> { new() { Id = -1, Name = "— No template —" } };
            templates.AddRange(new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => v.IsTemplate).OrderBy(v => v.Name)
                .Select(v => new NamedOption { Id = v.Id.Value, Name = v.Name }));

            var titleBlocks = new List<NamedOption> { new() { Id = -1, Name = "— None —" } };
            titleBlocks.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType()
                .Cast<ElementType>().OrderBy(t => t.Name)
                .Select(t => new NamedOption { Id = t.Id.Value, Name = $"{t.FamilyName} : {t.Name}" }));

            var scopeBoxes = new List<NamedOption> { new() { Id = -1, Name = "— No scope box —" } };
            scopeBoxes.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest).WhereElementIsNotElementType()
                .OrderBy(e => e.Name)
                .Select(e => new NamedOption { Id = e.Id.Value, Name = e.Name }));

            var vm = new ViewManagerViewModel(levels, templates, titleBlocks, scopeBoxes);
            if (!new ViewManagerWindow(vm).ShowDialogAndGetResult()) return Result.Cancelled;

            var templateId = vm.SelectedTemplate is { Id: >= 0 } t2 ? new ElementId(t2.Id) : ElementId.InvalidElementId;
            var scopeBoxId = vm.SelectedScopeBox is { Id: >= 0 } sbId ? new ElementId(sbId.Id) : ElementId.InvalidElementId;
            var titleBlockId = vm.CreateSheets && vm.SelectedTitleBlock is { Id: >= 0 } tb
                ? new ElementId(tb.Id) : ElementId.InvalidElementId;
            var selectedLevelIds = vm.SelectedLevelIds.ToHashSet();

            using var tx = new Transaction(doc, "AllO - View Manager");
            tx.Start();

            var created = new List<View>();

            if (vm.IsFloor || vm.IsCeiling)
            {
                var family = vm.IsCeiling ? ViewFamily.CeilingPlan : ViewFamily.FloorPlan;
                var vft = FindVft(doc, family);
                if (vft == null) { tx.RollBack(); TaskDialog.Show("View Manager", "No plan ViewFamilyType found."); return Result.Failed; }
                foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                             .Where(l => selectedLevelIds.Contains(l.Id.Value)))
                {
                    var view = ViewPlan.Create(doc, vft.Id, level.Id);
                    view.Name = UniqueViewName(doc, $"{vm.Prefix}{level.Name}{vm.Suffix}");
                    created.Add(view);
                }
            }
            else if (vm.Is3D)
            {
                var vft = FindVft(doc, ViewFamily.ThreeDimensional);
                if (vft != null)
                {
                    var v = View3D.CreateIsometric(doc, vft.Id);
                    v.Name = UniqueViewName(doc, $"{vm.Prefix}3D{vm.Suffix}");
                    created.Add(v);
                }
            }
            else if (vm.IsSection)
            {
                var vft = FindVft(doc, ViewFamily.Section);
                var bb = ModelBoundingBox(doc);
                if (vft != null && bb != null)
                {
                    var v = ViewSection.CreateSection(doc, vft.Id, SectionBox(bb));
                    v.Name = UniqueViewName(doc, $"{vm.Prefix}Section{vm.Suffix}");
                    created.Add(v);
                }
            }
            else if (vm.IsElevation)
            {
                created.AddRange(CreateElevations(doc, vm.Prefix, vm.Suffix));
            }

            if (templateId != ElementId.InvalidElementId)
                foreach (var v in created)
                    try { v.ViewTemplateId = templateId; } catch { }

            if (scopeBoxId != ElementId.InvalidElementId)
                foreach (var v in created)
                    try { v.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP)?.Set(scopeBoxId); } catch { }

            int sheets = 0;
            if (vm.CreateSheets)
            {
                int i = 0;
                foreach (var v in created)
                {
                    try
                    {
                        var sheet = ViewSheet.Create(doc, titleBlockId);
                        sheet.SheetNumber = UniqueSheetNumber(doc, $"{vm.BaseSheetNumber}-{(++i):D2}");
                        sheet.Name = v.Name;
                        Viewport.Create(doc, sheet.Id, v.Id, new XYZ(0, 0, 0));
                        sheets++;
                    }
                    catch { }
                }
            }

            tx.Commit();
            ToastHost.Show("View Manager", $"Created {created.Count} view(s), {sheets} sheet(s).", ToastKind.Success);
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

    private static ViewFamilyType? FindVft(Document doc, ViewFamily family)
        => new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == family);

    private static BoundingBoxXYZ? ModelBoundingBox(Document doc)
    {
        XYZ? min = null, max = null;
        foreach (var e in new FilteredElementCollector(doc).WhereElementIsNotElementType()
                     .Where(e => e.Category?.CategoryType == CategoryType.Model))
        {
            var bb = e.get_BoundingBox(null);
            if (bb == null) continue;
            min = min == null ? bb.Min : new XYZ(Math.Min(min.X, bb.Min.X), Math.Min(min.Y, bb.Min.Y), Math.Min(min.Z, bb.Min.Z));
            max = max == null ? bb.Max : new XYZ(Math.Max(max.X, bb.Max.X), Math.Max(max.Y, bb.Max.Y), Math.Max(max.Z, bb.Max.Z));
        }
        return (min == null || max == null) ? null : new BoundingBoxXYZ { Min = min, Max = max };
    }

    private static BoundingBoxXYZ SectionBox(BoundingBoxXYZ model)
    {
        var min = model.Min; var max = model.Max;
        var center = (min + max) / 2;
        var t = Transform.Identity;
        t.Origin = center;
        t.BasisX = XYZ.BasisX;       // ancho de la sección
        t.BasisY = XYZ.BasisZ;       // alto
        t.BasisZ = -XYZ.BasisY;      // dirección de mirada (hacia el norte)
        double w = (max.X - min.X) / 2 + 1;
        double h = (max.Z - min.Z) / 2 + 1;
        double d = (max.Y - min.Y) / 2 + 1;
        return new BoundingBoxXYZ
        {
            Transform = t,
            Min = new XYZ(-w, -h, -d),
            Max = new XYZ(w, h, d)
        };
    }

    private static IEnumerable<View> CreateElevations(Document doc, string prefix, string suffix)
    {
        var result = new List<View>();
        var vft = FindVft(doc, ViewFamily.Elevation);
        var plan = doc.ActiveView as ViewPlan
                   ?? new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                       .FirstOrDefault(v => !v.IsTemplate);
        var bb = ModelBoundingBox(doc);
        if (vft == null || plan == null || bb == null) return result;

        var center = (bb.Min + bb.Max) / 2;
        var marker = ElevationMarker.CreateElevationMarker(doc, vft.Id, center, 100);
        string[] dirs = { "North", "East", "South", "West" };
        for (int i = 0; i < 4; i++)
        {
            try
            {
                var v = marker.CreateElevation(doc, plan.Id, i);
                v.Name = UniqueViewName(doc, $"{prefix}{dirs[i]}{suffix}");
                result.Add(v);
            }
            catch { }
        }
        return result;
    }

    private static string UniqueViewName(Document doc, string baseName)
    {
        var used = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
            .Select(v => v.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseName)) return baseName;
        for (int i = 2; i < 1000; i++)
            if (!used.Contains($"{baseName} ({i})")) return $"{baseName} ({i})";
        return $"{baseName} {Guid.NewGuid():N}".Substring(0, baseName.Length + 5);
    }

    private static string UniqueSheetNumber(Document doc, string baseNumber)
    {
        var used = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
            .Select(s => s.SheetNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseNumber)) return baseNumber;
        for (int i = 1; i < 1000; i++)
            if (!used.Contains($"{baseNumber}-{i}")) return $"{baseNumber}-{i}";
        return baseNumber + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
    }
}

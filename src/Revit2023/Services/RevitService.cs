using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.Models;
using AllO.Services;

namespace AllO.Revit2023.Services;

/// <summary>
/// Implementacion de IRevitService para Revit 2023.
/// Usa ElementId.IntegerValue (int) en lugar de .Value (long).
/// </summary>
public class RevitService : IRevitService
{
    private readonly UIApplication _uiApp;
    private Document? Doc => _uiApp.ActiveUIDocument?.Document;

    public RevitService(UIApplication uiApp)
    {
        _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
    }

    public bool IsRevitSessionActive
    {
        get { try { return Doc != null; } catch { return false; } }
    }

    public string GetDocumentName()
    {
        try { return Doc?.Title ?? "No document"; }
        catch { return "No document"; }
    }

    public List<SheetInfo> GetAllSheets()
    {
        if (Doc == null) return new List<SheetInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .Select(sheet =>
            {
                string tbName = "No title block";
                int tbTypeId = -1;
                var tbInst = new FilteredElementCollector(Doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();
                if (tbInst != null) { tbName = tbInst.Name; tbTypeId = tbInst.GetTypeId().IntegerValue; }

                string approvedBy = "";
                var approvedParam = sheet.get_Parameter(BuiltInParameter.SHEET_APPROVED_BY);
                if (approvedParam != null) approvedBy = approvedParam.AsString() ?? "";

                string designedBy = "";
                var designedParam = sheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY);
                if (designedParam != null) designedBy = designedParam.AsString() ?? "";

                string issueDate = "";
                var issueDateParam = sheet.get_Parameter(BuiltInParameter.SHEET_ISSUE_DATE);
                if (issueDateParam != null) issueDate = issueDateParam.AsString() ?? "";

                return new SheetInfo
                {
                    ElementId = sheet.Id.IntegerValue,
                    SheetNumber = sheet.SheetNumber,
                    OriginalName = sheet.Name,
                    PreviewName = sheet.Name,
                    TitleBlockName = tbName,
                    TitleBlockTypeId = tbTypeId,
                    ApprovedBy = approvedBy,
                    DesignedBy = designedBy,
                    SheetIssueDate = issueDate
                };
            })
            .OrderBy(s => s.SheetNumber).ToList();
    }

    public int RenameSheets(Dictionary<int, string> renames)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Rename Sheets");
        tx.Start();
        try
        {
            foreach (var kvp in renames)
            {
                var sheet = Doc.GetElement(new ElementId(kvp.Key)) as ViewSheet;
                var param = sheet?.get_Parameter(BuiltInParameter.SHEET_NAME);
                if (param != null && !param.IsReadOnly) { param.Set(kvp.Value); count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public int RenumberSheets(Dictionary<int, string> renumbers)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Renumber Sheets");
        tx.Start();
        try
        {
            foreach (var kvp in renumbers)
            {
                var sheet = Doc.GetElement(new ElementId(kvp.Key)) as ViewSheet;
                var param = sheet?.get_Parameter(BuiltInParameter.SHEET_NUMBER);
                if (param != null && !param.IsReadOnly) { param.Set(kvp.Value); count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public int DeleteSheets(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Delete Sheets");
        tx.Start();
        try
        {
            foreach (var id in elementIds)
            {
                var eid = new ElementId(id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public List<int> CreateSheets(int titleBlockTypeId, List<SheetCreateRequest> requests)
    {
        if (Doc == null) return new List<int>();
        var created = new List<int>();
        using var tx = new Transaction(Doc, "AllO - Create Sheets");
        tx.Start();
        try
        {
            var tbTypeId = new ElementId(titleBlockTypeId);
            foreach (var req in requests)
            {
                var sheet = ViewSheet.Create(Doc, tbTypeId);
                sheet.SheetNumber = req.Number;
                sheet.Name = req.Name;
                created.Add(sheet.Id.IntegerValue);
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return created;
    }

    public int DuplicateSheets(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Duplicate Sheets");
        tx.Start();
        try
        {
            foreach (var id in elementIds)
            {
                var original = Doc.GetElement(new ElementId(id)) as ViewSheet;
                if (original == null) continue;
                var tbInstance = new FilteredElementCollector(Doc, original.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();
                ElementId tbTypeId = tbInstance?.GetTypeId() ?? ElementId.InvalidElementId;
                if (tbTypeId == ElementId.InvalidElementId) continue;
                var newSheet = ViewSheet.Create(Doc, tbTypeId);
                newSheet.SheetNumber = original.SheetNumber + " - Copy";
                newSheet.Name = original.Name + " (Copy)";
                count++;
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public int ChangeTitleBlock(int sheetElementId, int newTitleBlockTypeId)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Change Title Block");
        tx.Start();
        try
        {
            var sheet = Doc.GetElement(new ElementId(sheetElementId)) as ViewSheet;
            if (sheet == null) { tx.RollBack(); return 0; }
            var tbInstance = new FilteredElementCollector(Doc, sheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();
            if (tbInstance == null) { tx.RollBack(); return 0; }
            tbInstance.ChangeTypeId(new ElementId(newTitleBlockTypeId));
            tx.Commit();
            return 1;
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); return 0; }
    }

    public List<TitleBlockInfo> GetTitleBlocks()
    {
        if (Doc == null) return new List<TitleBlockInfo>();
        return new FilteredElementCollector(Doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsElementType()
            .Cast<FamilySymbol>()
            .Select(fs => new TitleBlockInfo
            {
                TypeId = fs.Id.IntegerValue,
                FamilyName = fs.FamilyName,
                TypeName = fs.Name
            })
            .OrderBy(t => t.FamilyName).ThenBy(t => t.TypeName)
            .ToList();
    }

    // ── Views ─────────────────────────────────────────────────

    public List<ViewInfo> GetAllViews()
    {
        if (Doc == null) return new List<ViewInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => !v.IsTemplate && v.CanBePrinted)
            .Select(v =>
            {
                string sheetNum = "";
                var sheetParam = v.get_Parameter(BuiltInParameter.VIEWPORT_SHEET_NUMBER);
                if (sheetParam != null) sheetNum = sheetParam.AsString() ?? "";

                string discipline = "";
                try { discipline = v.Discipline.ToString(); } catch { }

                string scale = "";
                try
                {
                    var scaleParam = v.get_Parameter(BuiltInParameter.VIEW_SCALE);
                    if (scaleParam != null) scale = "1 : " + scaleParam.AsInteger();
                }
                catch { }

                string viewTemplateName = "";
                try
                {
                    var templateId = v.ViewTemplateId;
                    if (templateId != ElementId.InvalidElementId)
                    {
                        var template = Doc.GetElement(templateId) as View;
                        if (template != null) viewTemplateName = template.Name;
                    }
                }
                catch { }

                return new ViewInfo
                {
                    ElementId = v.Id.IntegerValue,
                    Name = v.Name,
                    ViewType = v.ViewType.ToString(),
                    Discipline = discipline,
                    Scale = scale,
                    ViewTemplateName = viewTemplateName,
                    SheetNumber = sheetNum
                };
            })
            .OrderBy(v => v.ViewType).ThenBy(v => v.Name)
            .ToList();
    }

    public int DeleteViews(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Delete Views");
        tx.Start();
        try
        {
            foreach (var id in elementIds)
            {
                var eid = new ElementId(id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public int RenameViews(Dictionary<int, string> renames)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Rename Views");
        tx.Start();
        try
        {
            foreach (var kvp in renames)
            {
                var view = Doc.GetElement(new ElementId(kvp.Key)) as View;
                if (view != null) { view.Name = kvp.Value; count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    // ── Revisions ─────────────────────────────────────────────

    public List<RevisionInfo> GetAllRevisions()
    {
        if (Doc == null) return new List<RevisionInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(Revision))
            .Cast<Revision>()
            .Select(r => new RevisionInfo
            {
                ElementId = r.Id.IntegerValue,
                Sequence = r.SequenceNumber,
                Date = r.RevisionDate,
                Description = r.Description,
                IssuedBy = r.IssuedBy,
                IssuedTo = r.IssuedTo,
                Status = r.Issued ? "Issued" : "Not Issued"
            })
            .OrderBy(r => r.Sequence)
            .ToList();
    }

    public int CreateRevision(string date, string description, string issuedBy, string issuedTo)
    {
        if (Doc == null) return -1;
        using var tx = new Transaction(Doc, "AllO - Create Revision");
        tx.Start();
        try
        {
            var rev = Revision.Create(Doc);
            rev.RevisionDate = date;
            rev.Description = description;
            rev.IssuedBy = issuedBy;
            rev.IssuedTo = issuedTo;
            tx.Commit();
            return rev.Id.IntegerValue;
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
    }

    public int DeleteRevisions(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using var tx = new Transaction(Doc, "AllO - Delete Revisions");
        tx.Start();
        try
        {
            foreach (var id in elementIds)
            {
                var eid = new ElementId(id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); throw; }
        return count;
    }

    public int UpdateRevision(int elementId, string date, string description, string issuedBy, string issuedTo)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Update Revision");
        tx.Start();
        try
        {
            var rev = Doc.GetElement(new ElementId(elementId)) as Revision;
            if (rev == null) { tx.RollBack(); return 0; }
            rev.RevisionDate = date;
            rev.Description = description;
            rev.IssuedBy = issuedBy;
            rev.IssuedTo = issuedTo;
            tx.Commit();
            return 1;
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); return 0; }
    }

    public int ToggleRevisionIssued(int elementId)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Toggle Revision Issued");
        tx.Start();
        try
        {
            var rev = Doc.GetElement(new ElementId(elementId)) as Revision;
            if (rev == null) { tx.RollBack(); return 0; }
            rev.Issued = !rev.Issued;
            tx.Commit();
            return 1;
        }
        catch { if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack(); return 0; }
    }

    // ── Publishing / Export ───────────────────────────────────

    public List<PublishSheetItem> GetSheetsForPublish()
    {
        if (Doc == null) return new List<PublishSheetItem>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .Select(s => new PublishSheetItem
            {
                ElementId = s.Id.IntegerValue,
                SheetNumber = s.SheetNumber,
                SheetName = s.Name,
                IsSelected = true,
                Status = "Pending"
            })
            .OrderBy(s => s.SheetNumber)
            .ToList();
    }

    public bool ExportSingleToPdf(int sheetElementId, string outputFolder, string namingPattern, bool combinePdf = false)
    {
        if (Doc == null) return false;
        try
        {
            var viewId = new ElementId(sheetElementId);
            var sheet = Doc.GetElement(viewId) as ViewSheet;
            if (sheet == null) return false;

            var singleList = new List<ElementId> { viewId };
            string fileName = namingPattern
                .Replace("{number}", sheet.SheetNumber)
                .Replace("{name}", sheet.Name)
                .Replace("{Number}", sheet.SheetNumber)
                .Replace("{Name}", sheet.Name);

            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            var options = new PDFExportOptions();
            options.FileName = fileName;
            options.Combine = false;

            return Doc.Export(outputFolder, singleList, options);
        }
        catch { return false; }
    }

    public bool ExportSingleToDwg(int sheetElementId, string outputFolder, string namingPattern)
    {
        if (Doc == null) return false;
        try
        {
            var viewId = new ElementId(sheetElementId);
            var sheet = Doc.GetElement(viewId) as ViewSheet;
            if (sheet == null) return false;

            var singleList = new List<ElementId> { viewId };
            string fileName = namingPattern
                .Replace("{number}", sheet.SheetNumber)
                .Replace("{name}", sheet.Name)
                .Replace("{Number}", sheet.SheetNumber)
                .Replace("{Name}", sheet.Name);

            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            var dwgOptions = new DWGExportOptions();
            return Doc.Export(outputFolder, fileName, singleList, dwgOptions);
        }
        catch { return false; }
    }

    // ── CopyCrop ──────────────────────────────────────────────

    public List<CropViewInfo> GetCroppableViews()
    {
        if (Doc == null) return new List<CropViewInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => !v.IsTemplate && v.ViewType != ViewType.Schedule
                        && v.ViewType != ViewType.DrawingSheet
                        && v.ViewType != ViewType.Legend
                        && v.ViewType != ViewType.Internal)
            .Select(v => new CropViewInfo
            {
                ElementId = v.Id.IntegerValue,
                Name = v.Name,
                ViewType = v.ViewType.ToString(),
                HasCropRegion = v.CropBoxActive || v.CropBoxVisible,
                IsCropActive = v.CropBoxActive
            })
            .OrderBy(v => v.Name)
            .ToList();
    }

    public int CopyCropRegion(int sourceViewId, List<int> targetViewIds)
    {
        if (Doc == null) return 0;
        var sourceView = Doc.GetElement(new ElementId(sourceViewId)) as View;
        if (sourceView == null || !sourceView.CropBoxActive) return 0;

        var sourceCrop = sourceView.CropBox;
        int count = 0;

        using (var tx = new Transaction(Doc, "AllO: Copy Crop Region"))
        {
            tx.Start();
            try
            {
                foreach (var id in targetViewIds)
                {
                    var target = Doc.GetElement(new ElementId(id)) as View;
                    if (target == null) continue;
                    try
                    {
                        target.CropBoxActive = true;
                        target.CropBox = sourceCrop;
                        target.CropBoxVisible = sourceView.CropBoxVisible;
                        count++;
                    }
                    catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    // ── Families ──────────────────────────────────────────────

    public List<FamilyInfo> GetAllFamilies()
    {
        if (Doc == null) return new List<FamilyInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .Select(f =>
            {
                int instanceCount = 0;
                try
                {
                    foreach (var typeId in f.GetFamilySymbolIds())
                    {
                        instanceCount += new FilteredElementCollector(Doc)
                            .WherePasses(new FamilyInstanceFilter(Doc, typeId))
                            .GetElementCount();
                    }
                }
                catch { }

                return new FamilyInfo
                {
                    ElementId = f.Id.IntegerValue,
                    FamilyName = f.Name,
                    Category = f.FamilyCategory?.Name ?? "Unknown",
                    TypeCount = f.GetFamilySymbolIds().Count,
                    InstanceCount = instanceCount,
                    IsInPlace = f.IsInPlace
                };
            })
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FamilyName)
            .ToList();
    }

    public int DeleteFamilies(List<int> familyIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Delete Families"))
        {
            tx.Start();
            try
            {
                foreach (var id in familyIds)
                {
                    try { Doc.Delete(new ElementId(id)); count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public int GetFamilyInstanceCount(int familyId) => 0;

    // ── Grids ─────────────────────────────────────────────────

    public List<GridInfo> GetAllGrids()
    {
        if (Doc == null) return new List<GridInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .Select(g =>
            {
                var curve = g.Curve;
                double length = curve != null ? curve.Length : 0;
                string orientation = "Other";
                if (curve is Line line)
                {
                    var dir = line.Direction;
                    if (Math.Abs(dir.X) > Math.Abs(dir.Y)) orientation = "Horizontal";
                    else if (Math.Abs(dir.Y) > Math.Abs(dir.X)) orientation = "Vertical";
                }
                return new GridInfo
                {
                    ElementId = g.Id.IntegerValue,
                    Name = g.Name,
                    NewName = g.Name,
                    Orientation = orientation,
                    Length = Math.Round(length * 0.3048, 2)
                };
            })
            .OrderBy(g => g.Name)
            .ToList();
    }

    public int RenameGrids(Dictionary<int, string> renames)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Rename Grids"))
        {
            tx.Start();
            try
            {
                foreach (var kvp in renames)
                {
                    var grid = Doc.GetElement(new ElementId(kvp.Key)) as Grid;
                    if (grid == null) continue;
                    try { grid.Name = kvp.Value; count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public int DeleteGrids(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Delete Grids"))
        {
            tx.Start();
            try
            {
                foreach (var id in elementIds)
                {
                    try { Doc.Delete(new ElementId(id)); count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    // ── Levels ────────────────────────────────────────────────

    public List<LevelInfo> GetAllLevels()
    {
        if (Doc == null) return new List<LevelInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(l => new LevelInfo
            {
                ElementId = l.Id.IntegerValue,
                Name = l.Name,
                NewName = l.Name,
                Elevation = Math.Round(l.Elevation * 0.3048, 3),
                NewElevation = Math.Round(l.Elevation * 0.3048, 3),
                IsStructural = l.get_Parameter(BuiltInParameter.LEVEL_IS_STRUCTURAL)?.AsInteger() == 1
            })
            .OrderBy(l => l.Elevation)
            .ToList();
    }

    public int RenameLevels(Dictionary<int, string> renames)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Rename Levels"))
        {
            tx.Start();
            try
            {
                foreach (var kvp in renames)
                {
                    var level = Doc.GetElement(new ElementId(kvp.Key)) as Level;
                    if (level == null) continue;
                    try { level.Name = kvp.Value; count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public int MoveLevels(Dictionary<int, double> newElevations)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Move Levels"))
        {
            tx.Start();
            try
            {
                foreach (var kvp in newElevations)
                {
                    var level = Doc.GetElement(new ElementId(kvp.Key)) as Level;
                    if (level == null) continue;
                    try { level.Elevation = kvp.Value / 0.3048; count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public int DeleteLevels(List<int> elementIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Delete Levels"))
        {
            tx.Start();
            try
            {
                foreach (var id in elementIds)
                {
                    try { Doc.Delete(new ElementId(id)); count++; } catch { }
                }
                tx.Commit();
            }
            catch
            {
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    // ── Align ─────────────────────────────────────────────────

    public List<AlignableElementInfo> GetSelectedElements()
    {
        if (Doc == null || _uiApp.ActiveUIDocument == null) return new List<AlignableElementInfo>();
        var result = new List<AlignableElementInfo>();
        var selection = _uiApp.ActiveUIDocument.Selection.GetElementIds();
        foreach (var id in selection)
        {
            var el = Doc.GetElement(id);
            if (el == null) continue;
            double x = 0, y = 0, z = 0;
            if (el.Location is LocationPoint lp) { x = lp.Point.X; y = lp.Point.Y; z = lp.Point.Z; }
            else if (el.Location is LocationCurve lc) { var mid = lc.Curve.Evaluate(0.5, true); x = mid.X; y = mid.Y; z = mid.Z; }
            result.Add(new AlignableElementInfo
            {
                ElementId = el.Id.IntegerValue,
                Name = el.Name ?? "Unknown",
                Category = el.Category?.Name ?? "None",
                X = Math.Round(x, 4), Y = Math.Round(y, 4), Z = Math.Round(z, 4)
            });
        }
        return result;
    }

    public int AlignElements(List<int> elementIds, string alignmentMode)
    {
        if (Doc == null || elementIds.Count < 2) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, $"AllO Align {alignmentMode}"))
        {
            tx.Start();
            try
            {
                var elements = elementIds.Select(id => Doc.GetElement(new ElementId(id))).Where(e => e?.Location != null).ToList();
                if (elements.Count < 2) { tx.RollBack(); return 0; }
                var positions = new List<(Element el, XYZ pos)>();
                foreach (var el in elements)
                {
                    if (el.Location is LocationPoint lp) positions.Add((el, lp.Point));
                    else if (el.Location is LocationCurve lc) positions.Add((el, lc.Curve.Evaluate(0.5, true)));
                }
                if (positions.Count < 2) { tx.RollBack(); return 0; }
                double targetX = 0, targetY = 0;
                switch (alignmentMode)
                {
                    case "Left": targetX = positions.Min(p => p.pos.X); break;
                    case "Right": targetX = positions.Max(p => p.pos.X); break;
                    case "Center": targetX = positions.Average(p => p.pos.X); break;
                    case "Top": targetY = positions.Max(p => p.pos.Y); break;
                    case "Bottom": targetY = positions.Min(p => p.pos.Y); break;
                    case "Middle": targetY = positions.Average(p => p.pos.Y); break;
                }
                foreach (var (el, pos) in positions)
                {
                    XYZ move = XYZ.Zero;
                    switch (alignmentMode)
                    {
                        case "Left": case "Right": case "Center": move = new XYZ(targetX - pos.X, 0, 0); break;
                        case "Top": case "Bottom": case "Middle": move = new XYZ(0, targetY - pos.Y, 0); break;
                    }
                    if (move.GetLength() > 0.001)
                    { try { ElementTransformUtils.MoveElement(Doc, el.Id, move); count++; } catch { } }
                }
                tx.Commit();
            }
            catch { tx.RollBack(); }
        }
        return count;
    }

    public int AlignToGrid(List<int> elementIds, int gridId)
    {
        if (Doc == null) return 0;
        var grid = Doc.GetElement(new ElementId(gridId)) as Grid;
        if (grid == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO Align to Grid"))
        {
            tx.Start();
            try
            {
                var gridCurve = grid.Curve;
                foreach (var id in elementIds)
                {
                    var el = Doc.GetElement(new ElementId(id));
                    if (el?.Location == null) continue;
                    XYZ pos = el.Location is LocationPoint lp ? lp.Point : (el.Location is LocationCurve lc ? lc.Curve.Evaluate(0.5, true) : XYZ.Zero);
                    var closest = gridCurve.Project(pos).XYZPoint;
                    var move = new XYZ(closest.X - pos.X, closest.Y - pos.Y, 0);
                    if (move.GetLength() > 0.001)
                    { try { ElementTransformUtils.MoveElement(Doc, el.Id, move); count++; } catch { } }
                }
                tx.Commit();
            }
            catch { tx.RollBack(); }
        }
        return count;
    }

    public int AlignToLevel(List<int> elementIds, int levelId)
    {
        if (Doc == null) return 0;
        var level = Doc.GetElement(new ElementId(levelId)) as Level;
        if (level == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO Align to Level"))
        {
            tx.Start();
            try
            {
                double targetZ = level.Elevation;
                foreach (var id in elementIds)
                {
                    var el = Doc.GetElement(new ElementId(id));
                    if (el?.Location is LocationPoint lp)
                    {
                        var move = new XYZ(0, 0, targetZ - lp.Point.Z);
                        if (Math.Abs(move.Z) > 0.001)
                        { try { ElementTransformUtils.MoveElement(Doc, el.Id, move); count++; } catch { } }
                    }
                }
                tx.Commit();
            }
            catch { tx.RollBack(); }
        }
        return count;
    }

    public int DistributeElements(List<int> elementIds, string direction)
    {
        if (Doc == null || elementIds.Count < 3) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, $"AllO Distribute {direction}"))
        {
            tx.Start();
            try
            {
                var items = new List<(Element el, XYZ pos)>();
                foreach (var id in elementIds)
                {
                    var el = Doc.GetElement(new ElementId(id));
                    if (el?.Location == null) continue;
                    XYZ pos = el.Location is LocationPoint lp ? lp.Point : (el.Location is LocationCurve lc ? lc.Curve.Evaluate(0.5, true) : XYZ.Zero);
                    items.Add((el, pos));
                }
                if (items.Count < 3) { tx.RollBack(); return 0; }
                items = direction == "Horizontal" ? items.OrderBy(i => i.pos.X).ToList() : items.OrderBy(i => i.pos.Y).ToList();
                double start = direction == "Horizontal" ? items.First().pos.X : items.First().pos.Y;
                double end = direction == "Horizontal" ? items.Last().pos.X : items.Last().pos.Y;
                double step = (end - start) / (items.Count - 1);
                for (int i = 1; i < items.Count - 1; i++)
                {
                    double target = start + step * i;
                    double current = direction == "Horizontal" ? items[i].pos.X : items[i].pos.Y;
                    double delta = target - current;
                    if (Math.Abs(delta) > 0.001)
                    {
                        XYZ move = direction == "Horizontal" ? new XYZ(delta, 0, 0) : new XYZ(0, delta, 0);
                        try { ElementTransformUtils.MoveElement(Doc, items[i].el.Id, move); count++; } catch { }
                    }
                }
                tx.Commit();
            }
            catch { tx.RollBack(); }
        }
        return count;
    }

    // ── Connector (MEP) ───────────────────────────────────────

    public List<DisconnectedConnectorInfo> FindDisconnectedElements()
    {
        if (Doc == null) return new List<DisconnectedConnectorInfo>();
        var result = new List<DisconnectedConnectorInfo>();
        foreach (var el in new FilteredElementCollector(Doc).WhereElementIsNotElementType())
        {
            ConnectorManager cm = null;
            string category = "";
            if (el is Autodesk.Revit.DB.MEPCurve mep) { cm = mep.ConnectorManager; category = el.Category?.Name ?? "MEP"; }
            else if (el is Autodesk.Revit.DB.FamilyInstance fi && fi.MEPModel?.ConnectorManager != null) { cm = fi.MEPModel.ConnectorManager; category = el.Category?.Name ?? "MEP"; }
            if (cm == null) continue;
            int disconnected = 0;
            foreach (Connector conn in cm.Connectors) { if (!conn.IsConnected) disconnected++; }
            if (disconnected > 0)
            {
                XYZ pos = XYZ.Zero;
                if (el.Location is LocationPoint lp) pos = lp.Point;
                else if (el.Location is LocationCurve lc) pos = lc.Curve.Evaluate(0.5, true);
                string systemType = "";
                try { foreach (Connector conn in cm.Connectors) { if (conn.MEPSystem != null) { systemType = conn.MEPSystem.Name; break; } } } catch { }
                result.Add(new DisconnectedConnectorInfo
                {
                    ElementId = el.Id.IntegerValue, ElementName = el.Name ?? "Unknown", Category = category,
                    SystemType = systemType, DisconnectedCount = disconnected,
                    X = Math.Round(pos.X, 2), Y = Math.Round(pos.Y, 2), Z = Math.Round(pos.Z, 2)
                });
            }
        }
        return result.OrderBy(r => r.Category).ThenBy(r => r.ElementName).ToList();
    }

    public int AutoConnectNearby(double toleranceFeet)
    {
        if (Doc == null) return 0;
        int count = 0;
        using (var tx = new Transaction(Doc, "AllO Auto Connect"))
        {
            tx.Start();
            try
            {
                var openConnectors = new List<Autodesk.Revit.DB.Connector>();
                foreach (var el in new FilteredElementCollector(Doc).WhereElementIsNotElementType())
                {
                    ConnectorManager cm = null;
                    if (el is Autodesk.Revit.DB.MEPCurve mep) cm = mep.ConnectorManager;
                    else if (el is Autodesk.Revit.DB.FamilyInstance fi) cm = fi.MEPModel?.ConnectorManager;
                    if (cm == null) continue;
                    foreach (Connector conn in cm.Connectors) { if (!conn.IsConnected) openConnectors.Add(conn); }
                }
                var used = new HashSet<int>();
                for (int i = 0; i < openConnectors.Count; i++)
                {
                    if (used.Contains(i)) continue;
                    for (int j = i + 1; j < openConnectors.Count; j++)
                    {
                        if (used.Contains(j)) continue;
                        if (openConnectors[i].Owner.Id == openConnectors[j].Owner.Id) continue;
                        if (openConnectors[i].Origin.DistanceTo(openConnectors[j].Origin) <= toleranceFeet)
                        { try { openConnectors[i].ConnectTo(openConnectors[j]); used.Add(i); used.Add(j); count++; break; } catch { } }
                    }
                }
                tx.Commit();
            }
            catch { tx.RollBack(); }
        }
        return count;
    }

    public int ConnectElements(int elementId1, int elementId2)
    {
        if (Doc == null) return 0;
        using (var tx = new Transaction(Doc, "AllO Connect"))
        {
            tx.Start();
            try
            {
                var el1 = Doc.GetElement(new ElementId(elementId1));
                var el2 = Doc.GetElement(new ElementId(elementId2));
                if (el1 == null || el2 == null) { tx.RollBack(); return 0; }
                ConnectorManager cm1 = null, cm2 = null;
                if (el1 is Autodesk.Revit.DB.MEPCurve m1) cm1 = m1.ConnectorManager;
                else if (el1 is Autodesk.Revit.DB.FamilyInstance f1) cm1 = f1.MEPModel?.ConnectorManager;
                if (el2 is Autodesk.Revit.DB.MEPCurve m2) cm2 = m2.ConnectorManager;
                else if (el2 is Autodesk.Revit.DB.FamilyInstance f2) cm2 = f2.MEPModel?.ConnectorManager;
                if (cm1 == null || cm2 == null) { tx.RollBack(); return 0; }
                Connector best1 = null, best2 = null;
                double bestDist = double.MaxValue;
                foreach (Connector c1 in cm1.Connectors) { if (c1.IsConnected) continue; foreach (Connector c2 in cm2.Connectors) { if (c2.IsConnected) continue; double d = c1.Origin.DistanceTo(c2.Origin); if (d < bestDist) { bestDist = d; best1 = c1; best2 = c2; } } }
                if (best1 != null && best2 != null) { best1.ConnectTo(best2); tx.Commit(); return 1; }
                tx.RollBack();
            }
            catch { tx.RollBack(); }
        }
        return 0;
    }

    public int HighlightElement(int elementId)
    {
        if (_uiApp.ActiveUIDocument == null) return 0;
        try
        {
            var id = new ElementId(elementId);
            _uiApp.ActiveUIDocument.Selection.SetElementIds(new List<ElementId> { id });
            _uiApp.ActiveUIDocument.ShowElements(id);
            return 1;
        }
        catch { return 0; }
    }

    // ── QuickSearch ───────────────────────────────────────────

    public List<SearchResultInfo> SearchElements(string query, bool byName, bool byId, bool byCategory, bool byFamily, bool byParameter)
    {
        if (Doc == null || string.IsNullOrWhiteSpace(query)) return new List<SearchResultInfo>();
        var results = new List<SearchResultInfo>();
        var collector = new FilteredElementCollector(Doc)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent();

        foreach (var el in collector)
        {
            if (el.Category == null) continue;
            string name = el.Name ?? "";
            string catName = el.Category.Name ?? "";
            string familyType = "";
            string levelName = "";
            string matchedOn = "";

            if (el is FamilyInstance fi)
            {
                var symbol = fi.Symbol;
                familyType = symbol != null ? $"{symbol.FamilyName}: {symbol.Name}" : "";
            }

            // Get level
            try
            {
                var levelParam = el.get_Parameter(BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
                if (levelParam == null) levelParam = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                if (levelParam != null) levelName = levelParam.AsValueString() ?? "";
            }
            catch { }

            // Check matches
            bool matched = false;
            if (byName && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            { matched = true; matchedOn = "Name"; }
            else if (byId && el.Id.IntegerValue.ToString().Contains(query))
            { matched = true; matchedOn = "ID"; }
            else if (byCategory && catName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            { matched = true; matchedOn = "Category"; }
            else if (byFamily && familyType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            { matched = true; matchedOn = "Family"; }
            else if (byParameter)
            {
                foreach (Parameter p in el.Parameters)
                {
                    try
                    {
                        string val = p.AsValueString() ?? p.AsString() ?? "";
                        if (!string.IsNullOrEmpty(val) && val.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        { matched = true; matchedOn = $"Param:{p.Definition.Name}"; break; }
                    }
                    catch { }
                }
            }

            if (matched)
            {
                results.Add(new SearchResultInfo
                {
                    ElementId = el.Id.IntegerValue,
                    Name = name,
                    Category = catName,
                    FamilyType = familyType,
                    LevelName = levelName,
                    MatchedOn = matchedOn
                });
            }

            if (results.Count >= 500) break; // Limit results
        }
        return results;
    }

    public int SelectElements(List<int> elementIds)
    {
        if (_uiApp.ActiveUIDocument == null) return 0;
        try
        {
            var ids = elementIds.Select(id => new ElementId(id)).ToList();
            _uiApp.ActiveUIDocument.Selection.SetElementIds(ids);
            return ids.Count;
        }
        catch { return 0; }
    }

    public int IsolateElements(List<int> elementIds)
    {
        if (Doc == null || _uiApp.ActiveUIDocument == null) return 0;
        try
        {
            var view = Doc.ActiveView;
            if (view == null) return 0;
            var ids = elementIds.Select(id => new ElementId(id)).ToList();
            using (var tx = new Transaction(Doc, "AllO Isolate"))
            {
                tx.Start();
                view.IsolateElementsTemporary(ids);
                tx.Commit();
            }
            return ids.Count;
        }
        catch { return 0; }
    }

    // ── TableGen (Excel → Revit) ──────────────────────────────────

    public List<ExistingTableViewInfo> GetExistingTableViews()
    {
        if (Doc == null) return new List<ExistingTableViewInfo>();
        var result = new List<ExistingTableViewInfo>();
        foreach (var v in new FilteredElementCollector(Doc).OfClass(typeof(View)).Cast<View>())
        {
            if (v.IsTemplate) continue;
            try
            {
                var param = v.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                string desc = param?.AsString() ?? "";
                if (!string.IsNullOrEmpty(desc) && desc.Contains("EXCEL_DATA|"))
                {
                    var parts = desc.Split('|');
                    result.Add(new ExistingTableViewInfo
                    {
                        ElementId = v.Id.IntegerValue,
                        Name = v.Name,
                        ExcelPath = parts.Length > 1 ? parts[1] : "",
                        SheetName = parts.Length > 2 ? parts[2] : "",
                        Range = parts.Length > 3 ? parts[3] : ""
                    });
                }
            }
            catch { }
        }
        return result;
    }

    public int ImportExcelAsTable(ExcelTableData data, string viewName, string viewType)
    {
        if (Doc == null) return 0;
        try
        {
            using (var tx = new Transaction(Doc, "AllO Import Excel Table"))
            {
                tx.Start();

                View? newView = null;
                if (viewType == "Legend")
                    newView = CreateLegendView();
                if (newView == null)
                    newView = CreateDraftingView();
                if (newView == null) { tx.RollBack(); return 0; }

                try { newView.Scale = 1; } catch { }

                var metaParts = viewName.Split('|');
                string filePath = metaParts.Length > 0 ? metaParts[0] : viewName;
                string sheetName = metaParts.Length > 1 ? metaParts[1] : "";
                string baseName = $"{System.IO.Path.GetFileNameWithoutExtension(filePath)} - {sheetName}";
                int counter = 1;
                while (true)
                {
                    try
                    {
                        newView.Name = counter == 1 ? baseName : $"{baseName} ({counter})";
                        break;
                    }
                    catch { if (++counter > 20) break; }
                }

                DrawTableOnView(newView, data);

                try
                {
                    var param = newView.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                    if (param != null) param.Set($"EXCEL_DATA|{viewName}");
                }
                catch { }

                tx.Commit();

                try { _uiApp.ActiveUIDocument.ActiveView = newView; } catch { }
                return newView.Id.IntegerValue;
            }
        }
        catch { return 0; }
    }

    public int ReloadTableView(int viewId, ExcelTableData data)
    {
        if (Doc == null) return 0;
        try
        {
            var view = Doc.GetElement(new ElementId(viewId)) as View;
            if (view == null) return 0;
            using (var tx = new Transaction(Doc, "AllO Reload Table"))
            {
                tx.Start();
                var collector = new FilteredElementCollector(Doc, view.Id);
                var idsToDelete = collector
                    .Where(el => el is CurveElement || el is TextNote)
                    .Select(el => el.Id).ToList();
                foreach (var id in idsToDelete) { try { Doc.Delete(id); } catch { } }
                DrawTableOnView(view, data);
                tx.Commit();
            }
            return 1;
        }
        catch { return 0; }
    }

    public int DeleteTableViews(List<int> viewIds)
    {
        if (Doc == null) return 0;
        int count = 0;
        try
        {
            var activeId = _uiApp.ActiveUIDocument?.ActiveView?.Id;
            using (var tx = new Transaction(Doc, "AllO Delete Table Views"))
            {
                tx.Start();
                foreach (int id in viewIds)
                {
                    var eid = new ElementId(id);
                    if (activeId != null && eid == activeId) continue;
                    try { Doc.Delete(eid); count++; } catch { }
                }
                tx.Commit();
            }
        }
        catch { }
        return count;
    }

    private View? CreateDraftingView()
    {
        try
        {
            var vTypeId = Doc!.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeDrafting);
            if (vTypeId == null || vTypeId == ElementId.InvalidElementId)
            {
                foreach (var vft in new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>())
                    if (vft.ViewFamily == ViewFamily.Drafting) { vTypeId = vft.Id; break; }
            }
            if (vTypeId == null || vTypeId == ElementId.InvalidElementId) return null;
            return ViewDrafting.Create(Doc, vTypeId);
        }
        catch { return null; }
    }

    private View? CreateLegendView()
    {
        try
        {
            ElementId? legendTypeId = null;
            foreach (var vft in new FilteredElementCollector(Doc!).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>())
                if (vft.ViewFamily == ViewFamily.Legend) { legendTypeId = vft.Id; break; }
            if (legendTypeId == null) return null;

            var viewLegendType = typeof(View).Assembly.GetType("Autodesk.Revit.DB.ViewLegend");
            if (viewLegendType != null)
            {
                var createMethod = viewLegendType.GetMethod("Create",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (createMethod != null)
                    return createMethod.Invoke(null, new object[] { Doc, legendTypeId }) as View;
            }
        }
        catch { }

        // Fallback: duplicate existing legend
        try
        {
            foreach (var v in new FilteredElementCollector(Doc!).OfClass(typeof(View)).Cast<View>())
            {
                if (!v.IsTemplate && v.ViewType == ViewType.Legend)
                {
                    var newId = v.Duplicate(ViewDuplicateOption.Duplicate);
                    return Doc.GetElement(newId) as View;
                }
            }
        }
        catch { }
        return null;
    }

    private ElementId GetTextNoteType()
    {
        string targetName = "Texto Tablas 2mm";
        foreach (var tt in new FilteredElementCollector(Doc!).OfClass(typeof(TextNoteType)).Cast<TextNoteType>())
        {
            try
            {
                if (tt.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    var pSize = tt.get_Parameter(BuiltInParameter.TEXT_SIZE);
                    if (pSize != null) pSize.Set(2.0 / 304.8);
                    var pWidth = tt.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE);
                    if (pWidth != null) pWidth.Set(0.8);
                    return tt.Id;
                }
            }
            catch { }
        }
        try
        {
            var baseId = Doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            var baseType = Doc.GetElement(baseId) as TextNoteType;
            if (baseType == null)
                baseType = new FilteredElementCollector(Doc).OfClass(typeof(TextNoteType)).FirstElement() as TextNoteType;
            if (baseType != null)
            {
                var newType = baseType.Duplicate(targetName) as TextNoteType;
                if (newType != null)
                {
                    var pSize = newType.get_Parameter(BuiltInParameter.TEXT_SIZE);
                    if (pSize != null) pSize.Set(2.0 / 304.8);
                    var pWidth = newType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE);
                    if (pWidth != null) pWidth.Set(0.8);
                    return newType.Id;
                }
            }
        }
        catch { }
        return Doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
    }

    private void DrawTableOnView(View view, ExcelTableData data)
    {
        double ScaleFactor = 1.0;
        double PtsToFt(double pts) => (pts / 864.0) * ScaleFactor;

        var txtTypeId = GetTextNoteType();

        var xCoords = new List<double> { 0.0 };
        double cx = 0;
        foreach (var w in data.ColWidths) { cx += PtsToFt(w); xCoords.Add(cx); }

        var yCoords = new List<double> { 0.0 };
        double cy = 0;
        foreach (var h in data.RowHeights) { cy -= PtsToFt(h); yCoords.Add(cy); }

        var hLines = new Dictionary<int, List<(int start, int end)>>();
        var vLines = new Dictionary<int, List<(int start, int end)>>();

        foreach (var cell in data.Cells)
        {
            int r = cell.Row, c = cell.Col;
            int rSpan = cell.RowSpan, cSpan = cell.ColSpan;

            if (cell.BorderTop) { if (!hLines.ContainsKey(r)) hLines[r] = new(); hLines[r].Add((c, c + cSpan)); }
            if (cell.BorderBottom) { int k = r + rSpan; if (!hLines.ContainsKey(k)) hLines[k] = new(); hLines[k].Add((c, c + cSpan)); }
            if (cell.BorderLeft) { if (!vLines.ContainsKey(c)) vLines[c] = new(); vLines[c].Add((r, r + rSpan)); }
            if (cell.BorderRight) { int k = c + cSpan; if (!vLines.ContainsKey(k)) vLines[k] = new(); vLines[k].Add((r, r + rSpan)); }

            if (!string.IsNullOrWhiteSpace(cell.Text))
            {
                double x1 = xCoords[c], x2 = xCoords[c + cSpan];
                double y1 = yCoords[r], y2 = yCoords[r + rSpan];
                double boxW = x2 - x1, boxH = y1 - y2;
                double xIns = x1 + boxW * 0.02, yIns = y1 - boxH * 0.05;
                var hAlignRevit = HorizontalTextAlignment.Left;
                if (cell.HAlign == -4108) { xIns = x1 + boxW / 2.0; hAlignRevit = HorizontalTextAlignment.Center; }
                else if (cell.HAlign == -4152) { xIns = x2 - boxW * 0.02; hAlignRevit = HorizontalTextAlignment.Right; }
                if (cell.VAlign == -4108) yIns = y1 - boxH / 2.0;
                else if (cell.VAlign == -4107) yIns = y2 + boxH * 0.05;
                try
                {
                    var tn = TextNote.Create(Doc!, view.Id, new XYZ(xIns, yIns, 0), cell.Text, txtTypeId);
                    tn.HorizontalAlignment = hAlignRevit;
                }
                catch { }
            }
        }

        foreach (var kvp in hLines)
        {
            double y = yCoords[kvp.Key];
            foreach (var seg in MergeIntervals(kvp.Value))
            {
                try { Doc!.Create.NewDetailCurve(view, Line.CreateBound(new XYZ(xCoords[seg.start], y, 0), new XYZ(xCoords[seg.end], y, 0))); } catch { }
            }
        }
        foreach (var kvp in vLines)
        {
            double x = xCoords[kvp.Key];
            foreach (var seg in MergeIntervals(kvp.Value))
            {
                try { Doc!.Create.NewDetailCurve(view, Line.CreateBound(new XYZ(x, yCoords[seg.start], 0), new XYZ(x, yCoords[seg.end], 0))); } catch { }
            }
        }
    }

    private static List<(int start, int end)> MergeIntervals(List<(int start, int end)> intervals)
    {
        if (intervals.Count == 0) return intervals;
        var sorted = intervals.OrderBy(i => i.start).ToList();
        var merged = new List<(int start, int end)>();
        var current = sorted[0];
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].start <= current.end)
                current = (current.start, Math.Max(current.end, sorted[i].end));
            else { merged.Add(current); current = sorted[i]; }
        }
        merged.Add(current);
        return merged;
    }
}

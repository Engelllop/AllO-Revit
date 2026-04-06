using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.Models;
using AllO.Services;
using AllO.Helpers;

namespace AllO.Revit2025.Services;

/// <summary>
/// Implementacion de IRevitService para Revit 2025.
/// Usa ElementId.Value (long) - API moderna sin deprecated warnings.
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
                long tbTypeId = -1;
                var tbInst = new FilteredElementCollector(Doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();
                if (tbInst != null) { tbName = tbInst.Name; tbTypeId = tbInst.GetTypeId().Value; }

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
                    ElementId = (int)sheet.Id.Value,
                    SheetNumber = sheet.SheetNumber,
                    OriginalName = sheet.Name,
                    PreviewName = sheet.Name,
                    TitleBlockName = tbName,
                    TitleBlockTypeId = (int)tbTypeId,
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
            Logging.Debug($"Renamed {count} sheets");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to rename sheets", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
            Logging.Debug($"Renumbered {count} sheets");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to renumber sheets", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
                var eid = new ElementId((long)id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
            Logging.Debug($"Deleted {count} sheets");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to delete sheets", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
            var tbTypeId = new ElementId((long)titleBlockTypeId);
            foreach (var req in requests)
            {
                var sheet = ViewSheet.Create(Doc, tbTypeId);
                sheet.SheetNumber = req.Number;
                sheet.Name = req.Name;
                created.Add((int)sheet.Id.Value);
            }
            tx.Commit();
            Logging.Debug($"Created {created.Count} sheets");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to create sheets", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
                var original = Doc.GetElement(new ElementId((long)id)) as ViewSheet;
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
            Logging.Debug($"Duplicated {count} sheets");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to duplicate sheets", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
        return count;
    }

    public int ChangeTitleBlock(int sheetElementId, int newTitleBlockTypeId)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Change Title Block");
        tx.Start();
        try
        {
            var sheet = Doc.GetElement(new ElementId((long)sheetElementId)) as ViewSheet;
            if (sheet == null) { tx.RollBack(); return 0; }
            var tbInstance = new FilteredElementCollector(Doc, sheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks).FirstOrDefault();
            if (tbInstance == null) { tx.RollBack(); return 0; }
            tbInstance.ChangeTypeId(new ElementId((long)newTitleBlockTypeId));
            tx.Commit();
            Logging.Debug($"Changed title block for sheet {sheetElementId}");
            return 1;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to change title block", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            return 0;
        }
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
                TypeId = (int)fs.Id.Value,
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
                    ElementId = (int)v.Id.Value,
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
                var eid = new ElementId((long)id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
            Logging.Debug($"Deleted {count} views");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to delete views", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
            Logging.Debug($"Renamed {count} views");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to rename views", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
                ElementId = (int)r.Id.Value,
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
            Logging.Debug($"Created revision: {description}");
            return (int)rev.Id.Value;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to create revision", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
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
                var eid = new ElementId((long)id);
                if (Doc.GetElement(eid) != null) { Doc.Delete(eid); count++; }
            }
            tx.Commit();
            Logging.Debug($"Deleted {count} revisions");
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to delete revisions", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            throw;
        }
        return count;
    }

    public int UpdateRevision(int elementId, string date, string description, string issuedBy, string issuedTo)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Update Revision");
        tx.Start();
        try
        {
            var rev = Doc.GetElement(new ElementId((long)elementId)) as Revision;
            if (rev == null) { tx.RollBack(); return 0; }
            rev.RevisionDate = date;
            rev.Description = description;
            rev.IssuedBy = issuedBy;
            rev.IssuedTo = issuedTo;
            tx.Commit();
            Logging.Debug($"Updated revision {elementId}");
            return 1;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to update revision", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            return 0;
        }
    }

    public int ToggleRevisionIssued(int elementId)
    {
        if (Doc == null) return 0;
        using var tx = new Transaction(Doc, "AllO - Toggle Revision Issued");
        tx.Start();
        try
        {
            var rev = Doc.GetElement(new ElementId((long)elementId)) as Revision;
            if (rev == null) { tx.RollBack(); return 0; }
            rev.Issued = !rev.Issued;
            tx.Commit();
            Logging.Debug($"Toggled revision {elementId} issued status to {rev.Issued}");
            return 1;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to toggle revision issued status", ex);
            if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
            return 0;
        }
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
                ElementId = (int)s.Id.Value,
                SheetNumber = s.SheetNumber,
                SheetName = s.Name,
                IsSelected = true,
                Status = "Pending",
                ParameterValues = ElementParameterHelper.CollectParameters(s)
            })
            .OrderBy(s => s.SheetNumber)
            .ToList();
    }

    public bool ExportSingleToPdf(int sheetElementId, string outputFolder, string namingPattern, bool combinePdf = false)
    {
        if (Doc == null) return false;
        try
        {
            var viewId = new ElementId((long)sheetElementId);
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

            var result = Doc.Export(outputFolder, singleList, options);
            if (result) Logging.Debug($"Exported sheet {sheetElementId} to PDF");
            else Logging.Warning($"Export to PDF failed for sheet {sheetElementId}");
            return result;
        }
        catch (Exception ex)
        {
            Logging.Error($"Export to PDF failed for sheet {sheetElementId}", ex);
            return false;
        }
    }

    public bool ExportSingleToDwg(int sheetElementId, string outputFolder, string namingPattern)
    {
        if (Doc == null) return false;
        try
        {
            var viewId = new ElementId((long)sheetElementId);
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
            var result = Doc.Export(outputFolder, fileName, singleList, dwgOptions);
            if (result) Logging.Debug($"Exported sheet {sheetElementId} to DWG");
            else Logging.Warning($"Export to DWG failed for sheet {sheetElementId}");
            return result;
        }
        catch (Exception ex)
        {
            Logging.Error($"Export to DWG failed for sheet {sheetElementId}", ex);
            return false;
        }
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
                ElementId = (int)v.Id.Value,
                Name = v.Name,
                ViewType = v.ViewType.ToString(),
                HasCropRegion = v.CropBoxActive || v.CropBoxVisible,
                IsCropActive = v.CropBoxActive
            })
            .OrderBy(v => v.Name)
            .ToList();
    }

    public bool CopyCropToSingleView(int sourceViewId, int targetViewId)
    {
        if (Doc == null) return false;
        var sourceView = Doc.GetElement(new ElementId((long)sourceViewId)) as View;
        if (sourceView == null || !sourceView.CropBoxActive) return false;

        var target = Doc.GetElement(new ElementId((long)targetViewId)) as View;
        if (target == null) return false;

        var sourceCrop = sourceView.CropBox;

        using (var tx = new Transaction(Doc, "AllO: Copy Crop Region"))
        {
            tx.Start();
            try
            {
                target.CropBoxActive = true;
                target.CropBox = sourceCrop;
                target.CropBoxVisible = sourceView.CropBoxVisible;
                tx.Commit();
                Logging.Debug($"Copied crop region to view {targetViewId}");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to copy crop region", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                return false;
            }
        }
    }

    // ── Link family transfer (copy from link → host) ───────────

    public List<LinkDocumentInfo> GetLinkedDocuments()
    {
        if (Doc == null) return new List<LinkDocumentInfo>();
        return new FilteredElementCollector(Doc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Select(li =>
            {
                Document? ld = li.GetLinkDocument();
                return new LinkDocumentInfo
                {
                    LinkInstanceId = (int)li.Id.Value,
                    Name = ld != null ? (string.IsNullOrEmpty(ld.Title) ? li.Name : ld.Title) : li.Name,
                    Path = ld?.PathName ?? string.Empty,
                    IsLoaded = ld != null
                };
            })
            .OrderBy(x => x.Name)
            .ToList();
    }

    public List<LinkedCategoryInfo> GetCategoriesInLink(int linkInstanceId)
    {
        if (Doc == null) return new List<LinkedCategoryInfo>();
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return new List<LinkedCategoryInfo>();
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return new List<LinkedCategoryInfo>();

        var map = new Dictionary<long, string>();
        foreach (Family f in new FilteredElementCollector(linkDoc).OfClass(typeof(Family)).Cast<Family>())
        {
            if (f.IsInPlace) continue;
            Category? cat = f.FamilyCategory;
            if (cat == null) continue;
            long cid = cat.Id.Value;
            if (!map.ContainsKey(cid))
                map[cid] = cat.Name;
        }

        return map.OrderBy(k => k.Value)
            .Select(k => new LinkedCategoryInfo { CategoryId = k.Key, Name = k.Value })
            .ToList();
    }

    public List<LinkFamilyTypeInfo> GetFamilyTypesInLinkCategory(int linkInstanceId, long categoryId)
    {
        if (Doc == null) return new List<LinkFamilyTypeInfo>();
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return new List<LinkFamilyTypeInfo>();
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return new List<LinkFamilyTypeInfo>();

        var list = new List<LinkFamilyTypeInfo>();
        foreach (Family f in new FilteredElementCollector(linkDoc).OfClass(typeof(Family)).Cast<Family>())
        {
            if (f.IsInPlace) continue;
            if (f.FamilyCategory?.Id.Value != categoryId) continue;
            foreach (ElementId fsId in f.GetFamilySymbolIds())
            {
                if (linkDoc.GetElement(fsId) is not FamilySymbol sym) continue;
                list.Add(new LinkFamilyTypeInfo
                {
                    FamilySymbolId = sym.Id.Value,
                    DisplayName = $"{f.Name} : {sym.Name}"
                });
            }
        }

        return list.OrderBy(x => x.DisplayName).ToList();
    }

    public int CopyFamilyInstancesFromLinkToHost(int linkInstanceId, long familySymbolId)
    {
        if (Doc == null) return 0;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return 0;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return 0;

        if (linkDoc.GetElement(new ElementId(familySymbolId)) is not FamilySymbol sym) return 0;

        var ids = new FilteredElementCollector(linkDoc)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Where(fi => fi.GetTypeId() == sym.Id)
            .Select(fi => fi.Id)
            .ToList();

        if (ids.Count == 0)
        {
            Logging.Warning("No instances of the selected type in the linked model.");
            return 0;
        }

        using var tx = new Transaction(Doc, "AllO: Copy family instances from link");
        tx.Start();
        try
        {
            var opts = new CopyPasteOptions();
            Transform t = link.GetTotalTransform();
            ICollection<ElementId> copied = ElementTransformUtils.CopyElements(linkDoc, ids, Doc, t, opts);
            tx.Commit();
            int n = copied?.Count ?? 0;
            Logging.Debug($"Copied {n} instance(s) from link to host");
            return n;
        }
        catch (Exception ex)
        {
            Logging.Error("CopyFamilyInstancesFromLinkToHost failed", ex);
            if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            throw;
        }
    }

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
                    ElementId = (int)g.Id.Value,
                    Name = g.Name,
                    NewName = g.Name,
                    Orientation = orientation,
                    Length = UnitConverter.ToMeters(length)
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
                Logging.Debug($"Renamed {count} grids");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to rename grids", ex);
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
                    try { Doc.Delete(new ElementId((long)id)); count++; } catch { }
                }
                tx.Commit();
                Logging.Debug($"Deleted {count} grids");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to delete grids", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public void PopulateGridSyncWarnings(int linkInstanceId, List<GridInfo> hostGrids)
    {
        if (Doc == null) return;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return;
        Transform t = link.GetTotalTransform();
        var linkGrids = new FilteredElementCollector(linkDoc).OfClass(typeof(Grid)).Cast<Grid>()
            .Where(g => g.Curve != null).GroupBy(g => g.Name).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        const double tolFeet = 0.03;
        foreach (var row in hostGrids)
        {
            row.SyncWarning = null;
            row.HasSyncMismatch = false;
            if (!linkGrids.TryGetValue(row.Name, out var lg) || lg.Curve == null) continue;
            var hostEl = Doc.GetElement(new ElementId((long)row.ElementId)) as Grid;
            if (hostEl?.Curve == null) continue;
            Curve? cLink = lg.Curve.CreateTransformed(t);
            if (!CurvesMatch(hostEl.Curve, cLink, tolFeet))
            {
                row.HasSyncMismatch = true;
                row.SyncWarning = "Axis offset from reference link";
            }
        }
    }

    public int CopyGridsFromLink(int linkInstanceId, bool onlyNewNames)
    {
        if (Doc == null) return 0;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return 0;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return 0;

        var linkGrids = new FilteredElementCollector(linkDoc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
        var hostNames = new HashSet<string>(
            new FilteredElementCollector(Doc).OfClass(typeof(Grid)).Cast<Grid>().Select(g => g.Name),
            StringComparer.Ordinal);

        ICollection<ElementId> ids = onlyNewNames
            ? linkGrids.Where(g => !hostNames.Contains(g.Name)).Select(g => g.Id).ToList()
            : linkGrids.Select(g => g.Id).ToList();

        if (ids.Count == 0) return 0;

        using (var tx = new Transaction(Doc, "AllO: Copy grids from link"))
        {
            tx.Start();
            try
            {
                var opts = new CopyPasteOptions();
                var copied = ElementTransformUtils.CopyElements(linkDoc, ids, Doc, link.GetTotalTransform(), opts);
                tx.Commit();
                return copied?.Count ?? 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Copy grids from link failed", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                return 0;
            }
        }
    }

    public int SyncGridsFromLink(int linkInstanceId)
    {
        if (Doc == null) return 0;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return 0;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return 0;

        Transform t = link.GetTotalTransform();
        var linkGrids = new FilteredElementCollector(linkDoc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
        var hostByName = new FilteredElementCollector(Doc).OfClass(typeof(Grid)).Cast<Grid>()
            .GroupBy(g => g.Name).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        const double tolFeet = 0.03;
        int count = 0;

        using (var tx = new Transaction(Doc, "AllO: Sync grids from link"))
        {
            tx.Start();
            try
            {
                var opts = new CopyPasteOptions();
                foreach (var lg in linkGrids)
                {
                    if (lg.Curve == null) continue;
                    if (!hostByName.TryGetValue(lg.Name, out var hg)) continue;
                    if (hg.Curve == null) continue;
                    Curve cLink = lg.Curve.CreateTransformed(t);
                    if (CurvesMatch(hg.Curve, cLink, tolFeet)) continue;

                    Doc.Delete(hg.Id);
                    ElementTransformUtils.CopyElements(linkDoc, new List<ElementId> { lg.Id }, Doc, t, opts);
                    count++;
                }
                tx.Commit();
                Logging.Debug($"Synced {count} grid(s) from link");
            }
            catch (Exception ex)
            {
                Logging.Error("Sync grids from link failed", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                return 0;
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
                ElementId = (int)l.Id.Value,
                Name = l.Name,
                NewName = l.Name,
                Elevation = UnitConverter.ToMeters(l.Elevation),
                NewElevation = UnitConverter.ToMeters(l.Elevation),
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
                Logging.Debug($"Renamed {count} levels");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to rename levels", ex);
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
                    try { 
                        level.Elevation = UnitConverter.ToFeet(kvp.Value);
                        count++; 
                    } catch { }
                }
                tx.Commit();
                Logging.Debug($"Moved {count} levels");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to move levels", ex);
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
                    try { Doc.Delete(new ElementId((long)id)); count++; } catch { }
                }
                tx.Commit();
                Logging.Debug($"Deleted {count} levels");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to delete levels", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
            }
        }
        return count;
    }

    public void PopulateLevelSyncWarnings(int linkInstanceId, List<LevelInfo> hostLevels)
    {
        if (Doc == null) return;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return;
        Transform t = link.GetTotalTransform();
        var linkLevels = new FilteredElementCollector(linkDoc).OfClass(typeof(Level)).Cast<Level>()
            .GroupBy(l => l.Name).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        const double tolFeet = 0.01;
        foreach (var row in hostLevels)
        {
            row.SyncWarning = null;
            row.HasSyncMismatch = false;
            if (!linkLevels.TryGetValue(row.Name, out var ll)) continue;
            var hostEl = Doc.GetElement(new ElementId((long)row.ElementId)) as Level;
            if (hostEl == null) continue;
            double expectedZ = t.OfPoint(new XYZ(0, 0, ll.Elevation)).Z;
            if (Math.Abs(hostEl.Elevation - expectedZ) > tolFeet)
            {
                row.HasSyncMismatch = true;
                row.SyncWarning = "Elevation differs from reference link";
            }
        }
    }

    public int CopyLevelsFromLink(int linkInstanceId, bool onlyNewNames)
    {
        if (Doc == null) return 0;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return 0;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return 0;

        var linkLevels = new FilteredElementCollector(linkDoc).OfClass(typeof(Level)).Cast<Level>().ToList();
        var hostNames = new HashSet<string>(
            new FilteredElementCollector(Doc).OfClass(typeof(Level)).Cast<Level>().Select(l => l.Name),
            StringComparer.Ordinal);

        ICollection<ElementId> ids = onlyNewNames
            ? linkLevels.Where(l => !hostNames.Contains(l.Name)).Select(l => l.Id).ToList()
            : linkLevels.Select(l => l.Id).ToList();

        if (ids.Count == 0) return 0;

        using (var tx = new Transaction(Doc, "AllO: Copy levels from link"))
        {
            tx.Start();
            try
            {
                var opts = new CopyPasteOptions();
                var copied = ElementTransformUtils.CopyElements(linkDoc, ids, Doc, link.GetTotalTransform(), opts);
                tx.Commit();
                return copied?.Count ?? 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Copy levels from link failed", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                return 0;
            }
        }
    }

    public int SyncLevelsFromLink(int linkInstanceId)
    {
        if (Doc == null) return 0;
        var link = Doc.GetElement(new ElementId((long)linkInstanceId)) as RevitLinkInstance;
        if (link == null) return 0;
        Document? linkDoc = link.GetLinkDocument();
        if (linkDoc == null) return 0;

        Transform t = link.GetTotalTransform();
        var linkLevels = new FilteredElementCollector(linkDoc).OfClass(typeof(Level)).Cast<Level>()
            .GroupBy(l => l.Name).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        int count = 0;
        using (var tx = new Transaction(Doc, "AllO: Sync levels from link"))
        {
            tx.Start();
            try
            {
                foreach (var host in new FilteredElementCollector(Doc).OfClass(typeof(Level)).Cast<Level>())
                {
                    if (!linkLevels.TryGetValue(host.Name, out var ll)) continue;
                    double expectedZ = t.OfPoint(new XYZ(0, 0, ll.Elevation)).Z;
                    if (Math.Abs(host.Elevation - expectedZ) < 0.01) continue;
                    host.Elevation = expectedZ;
                    count++;
                }
                tx.Commit();
                Logging.Debug($"Synced {count} level(s) from link");
            }
            catch (Exception ex)
            {
                Logging.Error("Sync levels from link failed", ex);
                if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                return 0;
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
                ElementId = (int)el.Id.Value,
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
                var elements = elementIds.Select(id => Doc.GetElement(new ElementId((long)id))).Where(e => e?.Location != null).ToList();
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
                Logging.Debug($"Aligned {count} elements using {alignmentMode} mode");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to align elements", ex);
                tx.RollBack();
            }
        }
        return count;
    }

    public int AlignToGrid(List<int> elementIds, int gridId)
    {
        if (Doc == null) return 0;
        var grid = Doc.GetElement(new ElementId((long)gridId)) as Grid;
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
                    var el = Doc.GetElement(new ElementId((long)id));
                    if (el?.Location == null) continue;
                    XYZ pos = el.Location is LocationPoint lp ? lp.Point : (el.Location is LocationCurve lc ? lc.Curve.Evaluate(0.5, true) : XYZ.Zero);
                    var closest = gridCurve.Project(pos).XYZPoint;
                    var move = new XYZ(closest.X - pos.X, closest.Y - pos.Y, 0);
                    if (move.GetLength() > 0.001)
                    { try { ElementTransformUtils.MoveElement(Doc, el.Id, move); count++; } catch { } }
                }
                tx.Commit();
                Logging.Debug($"Aligned {count} elements to grid {gridId}");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to align elements to grid", ex);
                tx.RollBack();
            }
        }
        return count;
    }

    public int AlignToLevel(List<int> elementIds, int levelId)
    {
        if (Doc == null) return 0;
        var level = Doc.GetElement(new ElementId((long)levelId)) as Level;
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
                    var el = Doc.GetElement(new ElementId((long)id));
                    if (el?.Location is LocationPoint lp)
                    {
                        var move = new XYZ(0, 0, targetZ - lp.Point.Z);
                        if (Math.Abs(move.Z) > 0.001)
                        { try { ElementTransformUtils.MoveElement(Doc, el.Id, move); count++; } catch { } }
                    }
                }
                tx.Commit();
                Logging.Debug($"Aligned {count} elements to level {levelId}");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to align elements to level", ex);
                tx.RollBack();
            }
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
                    var el = Doc.GetElement(new ElementId((long)id));
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
                Logging.Debug($"Distributed {count} elements {direction}");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to distribute elements", ex);
                tx.RollBack();
            }
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
                    ElementId = (int)el.Id.Value,
                    ElementName = el.Name ?? "Unknown",
                    Category = category,
                    SystemType = systemType,
                    DisconnectedCount = disconnected,
                    X = Math.Round(pos.X, 2),
                    Y = Math.Round(pos.Y, 2),
                    Z = Math.Round(pos.Z, 2)
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
                Logging.Debug($"Auto-connected {count} MEP connectors");
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to auto-connect MEP elements", ex);
                tx.RollBack();
            }
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
                var el1 = Doc.GetElement(new ElementId((long)elementId1));
                var el2 = Doc.GetElement(new ElementId((long)elementId2));
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
                if (best1 != null && best2 != null) { best1.ConnectTo(best2); tx.Commit(); Logging.Debug("Connected 2 MEP elements"); return 1; }
                tx.RollBack();
            }
            catch (Exception ex)
            {
                Logging.Error("Failed to connect MEP elements", ex);
                tx.RollBack();
            }
        }
        return 0;
    }

    public int HighlightElement(int elementId)
    {
        if (_uiApp.ActiveUIDocument == null) return 0;
        try
        {
            var id = new ElementId((long)elementId);
            _uiApp.ActiveUIDocument.Selection.SetElementIds(new List<ElementId> { id });
            _uiApp.ActiveUIDocument.ShowElements(id);
            Logging.Debug($"Highlighted element {elementId}");
            return 1;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to highlight element", ex);
            return 0;
        }
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
                        ElementId = (int)v.Id.Value,
                        Name = v.Name,
                        ExcelPath = parts.Length > 1 ? parts[1] : "",
                        SheetName = parts.Length > 2 ? parts[2] : "",
                        Range = parts.Length > 3 ? parts[3] : "",
                        ViewKind = v is ViewSchedule ? "Schedule" : "Drawing"
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
        if (string.Equals(viewType, TableGenConstants.OutputKeySchedule, StringComparison.OrdinalIgnoreCase))
            return ImportExcelAsKeySchedule(data, viewName);
        try
        {
            using (var tx = new Transaction(Doc, "AllO Import Excel Table"))
            {
                tx.Start();

                View? newView = null;
                if (string.Equals(viewType, TableGenConstants.OutputLegend, StringComparison.OrdinalIgnoreCase))
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
                Logging.Debug($"Imported Excel table as view: {newView.Name}");
                return (int)newView.Id.Value;
            }
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to import Excel table", ex);
            return 0;
        }
    }

    public int ReloadTableView(int viewId, ExcelTableData data)
    {
        if (Doc == null) return 0;
        try
        {
            var view = Doc.GetElement(new ElementId((long)viewId)) as View;
            if (view == null) return 0;
            using (var tx = new Transaction(Doc, "AllO Reload Table"))
            {
                tx.Start();
                if (view is ViewSchedule vs)
                {
                    var grid = TableGenGridConverter.BuildGrid(data);
                    FillKeyScheduleBody(vs, grid);
                }
                else
                {
                    var collector = new FilteredElementCollector(Doc, view.Id);
                    var idsToDelete = collector
                        .Where(el => el is CurveElement || el is TextNote)
                        .Select(el => el.Id).ToList();
                    foreach (var id in idsToDelete) { try { Doc.Delete(id); } catch { } }
                    DrawTableOnView(view, data);
                }
                tx.Commit();
                Logging.Debug($"Reloaded table view {viewId}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to reload table view", ex);
            return 0;
        }
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
                    var eid = new ElementId((long)id);
                    if (activeId != null && eid == activeId) continue;
                    try { Doc.Delete(eid); count++; } catch { }
                }
                tx.Commit();
                Logging.Debug($"Deleted {count} table views");
            }
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to delete table views", ex);
        }
        return count;
    }

    private int ImportExcelAsKeySchedule(ExcelTableData data, string viewName)
    {
        if (Doc == null) return 0;
        try
        {
            using (var tx = new Transaction(Doc, "AllO Import Excel Key Schedule"))
            {
                tx.Start();
                var catId = new ElementId((long)BuiltInCategory.OST_GenericModel);
                ViewSchedule? schedule = ViewSchedule.CreateKeySchedule(Doc, catId);
                if (schedule == null) { tx.RollBack(); return 0; }

                var metaParts = viewName.Split('|');
                string filePath = metaParts.Length > 0 ? metaParts[0] : viewName;
                string sheetName = metaParts.Length > 1 ? metaParts[1] : "";
                string baseName = $"{System.IO.Path.GetFileNameWithoutExtension(filePath)} - {sheetName} (Schedule)";
                int counter = 1;
                while (true)
                {
                    try
                    {
                        schedule.Name = counter == 1 ? baseName : $"{baseName} ({counter})";
                        break;
                    }
                    catch { if (++counter > 20) break; }
                }

                ConfigureKeyScheduleFields(schedule);
                Doc.Regenerate();
                var grid = TableGenGridConverter.BuildGrid(data);
                FillKeyScheduleBody(schedule, grid);

                try
                {
                    var param = schedule.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                    if (param != null) param.Set($"EXCEL_DATA|{viewName}");
                }
                catch { }

                tx.Commit();
                try { _uiApp.ActiveUIDocument.ActiveView = schedule; } catch { }
                Logging.Debug($"Imported Excel as key schedule: {schedule.Name}");
                return (int)schedule.Id.Value;
            }
        }
        catch (Exception ex)
        {
            Logging.Error("Failed to import Excel key schedule", ex);
            return 0;
        }
    }

    private static void ConfigureKeyScheduleFields(ViewSchedule schedule)
    {
        ScheduleDefinition def = schedule.Definition;
        while (def.GetFieldCount() > 0)
        {
            ScheduleField f = def.GetField(0);
            def.RemoveField(f.FieldId);
        }

        BuiltInParameter[] order =
        {
            BuiltInParameter.ALL_MODEL_MARK,
            BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
            BuiltInParameter.ALL_MODEL_TYPE_COMMENTS,
            BuiltInParameter.ALL_MODEL_MANUFACTURER,
            BuiltInParameter.ALL_MODEL_MODEL
        };

        IList<SchedulableField> schedulable = def.GetSchedulableFields();
        foreach (var bp in order)
        {
            ElementId pid = new ElementId((long)bp);
            foreach (SchedulableField sf in schedulable)
            {
                if (sf.ParameterId.Equals(pid))
                {
                    try { def.AddField(sf); } catch { }
                    break;
                }
            }
        }
    }

    private static void ClearBodyRows(TableSectionData body)
    {
        while (body.NumberOfRows > 0)
        {
            try { body.RemoveRow(body.FirstRowNumber); }
            catch { break; }
        }
    }

    private static void FillKeyScheduleBody(ViewSchedule schedule, string[,] grid)
    {
        int fieldCount = schedule.Definition.GetFieldCount();
        if (fieldCount == 0 || grid.GetLength(0) == 0) return;

        TableData td = schedule.GetTableData();
        TableSectionData body = td.GetSectionData(SectionType.Body);
        ClearBodyRows(body);

        int rows = grid.GetLength(0);
        for (int r = 0; r < rows; r++)
            body.InsertRow(body.FirstRowNumber + body.NumberOfRows);

        for (int r = 0; r < rows; r++)
        {
            string[] rowVals = TableGenGridConverter.GetRowForSchedule(grid, r, fieldCount);
            for (int c = 0; c < rowVals.Length && c < fieldCount; c++)
            {
                try
                {
                    body.SetCellText(body.FirstRowNumber + r, body.FirstColumnNumber + c, rowVals[c]);
                }
                catch { }
            }
        }
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
        catch (Exception ex)
        {
            Logging.Error("Failed to create drafting view", ex);
            return null;
        }
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
        catch (Exception ex)
        {
            Logging.Error("Failed to create legend view", ex);
        }

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
        catch (Exception ex)
        {
            Logging.Error("Failed to duplicate legend view", ex);
        }
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
        catch (Exception ex)
        {
            Logging.Error("Failed to get/create text note type", ex);
        }
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
                
                // Use ExcelConstants for alignment
                if (cell.HAlign == (int)ExcelHAlign.Center) { xIns = x1 + boxW / 2.0; hAlignRevit = HorizontalTextAlignment.Center; }
                else if (cell.HAlign == (int)ExcelHAlign.Right) { xIns = x2 - boxW * 0.02; hAlignRevit = HorizontalTextAlignment.Right; }
                
                if (cell.VAlign == (int)ExcelVAlign.Center) yIns = y1 - boxH / 2.0;
                else if (cell.VAlign == (int)ExcelVAlign.Bottom) yIns = y2 + boxH * 0.05;
                
                try
                {
                    var tn = TextNote.Create(Doc!, view.Id, new XYZ(xIns, yIns, 0), cell.Text, txtTypeId);
                    tn.HorizontalAlignment = hAlignRevit;
                }
                catch (Exception ex)
                {
                    Logging.Warning($"Failed to create text note: {ex.Message}");
                }
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

    private static bool CurvesMatch(Curve a, Curve b, double tolFeet)
    {
        var p0a = a.GetEndPoint(0);
        var p1a = a.GetEndPoint(1);
        var p0b = b.GetEndPoint(0);
        var p1b = b.GetEndPoint(1);
        double d1 = p0a.DistanceTo(p0b) + p1a.DistanceTo(p1b);
        double d2 = p0a.DistanceTo(p1b) + p1a.DistanceTo(p0b);
        return Math.Min(d1, d2) < tolFeet * 2;
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


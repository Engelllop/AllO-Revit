using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SyncViewsCommand : IExternalCommand
{
    private static bool _isSyncing = false;
    private static ElementId? _masterViewId = null;
    private static ElementId? _slaveViewId = null;
    private static EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs>? _idlingHandler = null;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            if (_isSyncing)
            {
                StopSync(commandData.Application);
                TaskDialog.Show("Sync Views", "View synchronization stopped.");
                return Result.Succeeded;
            }

            // Start syncing — pick master/slave from the project's views
            var viewNames = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate)
                .Select(v => v.Name)
                .OrderBy(n => n)
                .ToList();

            var syncWin = new AllO.UI.Views.SyncViewsWindow(viewNames, doc.ActiveView?.Name);
            if (syncWin.ShowDialog() != true) return Result.Cancelled;
            var masterRef = syncWin.SelectedMaster;
            var slaveRef = syncWin.SelectedSlave;
            if (string.IsNullOrWhiteSpace(masterRef) || string.IsNullOrWhiteSpace(slaveRef)) return Result.Cancelled;

            var masterView = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(v => v.Name.Equals(masterRef, StringComparison.OrdinalIgnoreCase));

            var slaveView = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(v => v.Name.Equals(slaveRef, StringComparison.OrdinalIgnoreCase));

            if (masterView == null || slaveView == null)
            {
                TaskDialog.Show("Sync Views", "One or both views not found. Make sure views are named correctly and exist.");
                return Result.Failed;
            }

            _masterViewId = masterView.Id;
            _slaveViewId = slaveView.Id;
            _isSyncing = true;

            // Store the exact delegate so we can unregister it later
            _idlingHandler = OnIdling;
            commandData.Application.Idling += _idlingHandler;

            TaskDialog.Show("Sync Views", $"Syncing started:\nMaster: {masterView.Name}\nSlave: {slaveView.Name}\n\nNavigate in the master view and the slave will follow.\nRun the command again to stop.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }

    private static void StopSync(UIApplication app)
    {
        _isSyncing = false;
        _masterViewId = null;
        _slaveViewId = null;

        if (_idlingHandler != null)
        {
            try { app.Idling -= _idlingHandler; }
            catch { }
            _idlingHandler = null;
        }
    }

    private static void OnIdling(object? sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
        try
        {
            if (!_isSyncing || _masterViewId == null || _slaveViewId == null) return;

            var app = sender as UIApplication;
            if (app == null) return;

            var uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            var doc = uiDoc.Document;
            if (doc == null) return;

            // Safety: verify the stored view IDs belong to the current document
            var masterView = doc.GetElement(_masterViewId) as View;
            var slaveView = doc.GetElement(_slaveViewId) as View;
            if (masterView == null || slaveView == null)
            {
                // Views don't exist in this document; auto-stop to prevent crash
                StopSync(app);
                return;
            }

            // Only sync if master view is active
            if (uiDoc.ActiveView == null || uiDoc.ActiveView.Id != _masterViewId) return;

            var masterUiView = uiDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId == _masterViewId);
            var slaveUiView = uiDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId == _slaveViewId);

            if (masterUiView == null || slaveUiView == null) return;

            var masterCorner = masterUiView.GetZoomCorners();
            if (masterCorner == null || masterCorner.Count < 2) return;

            var slaveCorner = slaveUiView.GetZoomCorners();
            if (slaveCorner == null || slaveCorner.Count < 2) return;

            // Calculate centers
            var masterCenter = (masterCorner[0] + masterCorner[1]) / 2;
            var slaveCenter = (slaveCorner[0] + slaveCorner[1]) / 2;

            // Only update if master moved significantly
            var delta = masterCenter - slaveCenter;
            if (delta.GetLength() > 0.1)
            {
                var masterWidth = (masterCorner[1] - masterCorner[0]).GetLength();
                var slaveWidth = (slaveCorner[1] - slaveCorner[0]).GetLength();

                if (masterWidth > 0 && slaveWidth > 0)
                {
                    slaveUiView.ZoomAndCenterRectangle(masterCorner[0], masterCorner[1]);
                }
            }
        }
        catch
        {
            // Swallow everything during idle to never crash Revit
        }
    }
}

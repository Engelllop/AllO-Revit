using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AllO.Revit2023;

public class App : IExternalApplication
{
    private const string TabName = "AllO";
    private const string PanelName = "Documentation";

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Services.RevitServiceFactory.Creator = uiApp =>
                new AllO.Revit2023.Services.RevitService(uiApp);

            AllO.Services.ColorCoderOverlayHost.Register(application);

            application.CreateRibbonTab(TabName);
            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

            string thisDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string sharedAssemblyPath = Path.Combine(thisDir, "AllO.Shared.dll");

            var sheetBtn = new PushButtonData(
                "SheetManager", "Sheet\nManager",
                sharedAssemblyPath, "AllO.Commands.MainCommand");
            if (panel.AddItem(sheetBtn) is PushButton sb)
                sb.ToolTip = "Manage sheets, views and revisions.";

            var publishBtn = new PushButtonData(
                "Publish", "Publish",
                sharedAssemblyPath, "AllO.Commands.PublishCommand");
            if (panel.AddItem(publishBtn) is PushButton pb)
                pb.ToolTip = "Export sheets to PDF and/or DWG.";

            var toolkitBtn = new PushButtonData(
                "Toolkit", "Toolkit",
                sharedAssemblyPath, "AllO.Commands.SuperCommand");
            if (panel.AddItem(toolkitBtn) is PushButton tb)
                tb.ToolTip = "Copy Crop, Families, Grids, Levels — all in one.";

            var tableBtn = new PushButtonData(
                "TableGen", "Table\nGen",
                sharedAssemblyPath, "AllO.Commands.TableGenCommand");
            if (panel.AddItem(tableBtn) is PushButton tg)
                tg.ToolTip = "Generate and manage schedule tables.";

            // Productivity panel (stacked buttons — one column)
            RibbonPanel prodPanel = application.CreateRibbonPanel(TabName, "Productivity");

            var colorBtn = new PushButtonData(
                "ColorCoder", "Color\nCoder",
                sharedAssemblyPath, "AllO.Commands.ColorCoderCommand");
            var alignBtn = new PushButtonData(
                "LevelAlign", "Level\nAlign",
                sharedAssemblyPath, "AllO.Commands.AlignCommand");
            var connBtn = new PushButtonData(
                "Connector", "Connector",
                sharedAssemblyPath, "AllO.Commands.ConnectorCommand");

            IList<RibbonItem> prodStack = prodPanel.AddStackedItems(colorBtn, alignBtn, connBtn);
            if (prodStack.Count >= 1 && prodStack[0] is PushButton cb)
                cb.ToolTip =
                    "1st click: tint each open model’s views. 2nd click: settings. Reset in settings turns off.";
            if (prodStack.Count >= 2 && prodStack[1] is PushButton ab)
                ab.ToolTip = "Align MEP element elevation to a reference element.";
            if (prodStack.Count >= 3 && prodStack[2] is PushButton cn)
                cn.ToolTip = "Select 2 MEP elements to align and connect them.";

            var linkFamilyBtn = new PushButtonData(
                "LinkFamily", "Link\nFamily",
                sharedAssemblyPath, "AllO.Commands.FamilyTransferCommand");
            if (prodPanel.AddItem(linkFamilyBtn) is PushButton lfb)
                lfb.ToolTip =
                    "Copy a family type from a linked Revit model into the host (all instances, same positions).";

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("AllO - Error", $"Failed to start:\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        AllO.Services.ColorCoderOverlayHost.Shutdown();
        return Result.Succeeded;
    }
}

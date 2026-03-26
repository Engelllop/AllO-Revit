using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AllO.Revit2025;

public class App : IExternalApplication
{
    private const string TabName = "AllO";
    private const string PanelName = "Documentation";

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Services.RevitServiceFactory.Creator = uiApp =>
                new AllO.Revit2025.Services.RevitService(uiApp);

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

            // Productivity panel
            RibbonPanel prodPanel = application.CreateRibbonPanel(TabName, "Productivity");

            var colorBtn = new PushButtonData(
                "ColorCoder", "Color\nCoder",
                sharedAssemblyPath, "AllO.Commands.ColorCoderCommand");
            if (prodPanel.AddItem(colorBtn) is PushButton cb)
                cb.ToolTip = "Color-code views by document for multi-model workflows.";

            var alignBtn = new PushButtonData(
                "LevelAlign", "Level\nAlign",
                sharedAssemblyPath, "AllO.Commands.AlignCommand");
            if (prodPanel.AddItem(alignBtn) is PushButton ab)
                ab.ToolTip = "Align MEP element elevation to a reference element.";

            var connBtn = new PushButtonData(
                "Connector", "Connector",
                sharedAssemblyPath, "AllO.Commands.ConnectorCommand");
            if (prodPanel.AddItem(connBtn) is PushButton cn)
                cn.ToolTip = "Select 2 MEP elements to align and connect them.";

            var searchBtn = new PushButtonData(
                "QuickSearch", "Quick\nSearch",
                sharedAssemblyPath, "AllO.Commands.QuickSearchCommand");
            if (prodPanel.AddItem(searchBtn) is PushButton qb)
                qb.ToolTip = "Search elements by name, ID, category, family or parameter.";

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("AllO - Error", $"Failed to start:\n{ex.Message}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}

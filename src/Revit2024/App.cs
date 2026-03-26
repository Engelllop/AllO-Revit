using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AllO.Revit2024;

public class App : IExternalApplication
{
    private const string TabName = "AllO";
    private const string PanelName = "Documentation";

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Services.RevitServiceFactory.Creator = uiApp =>
                new AllO.Revit2024.Services.RevitService(uiApp);

            application.CreateRibbonTab(TabName);
            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

            string thisDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string sharedAssemblyPath = Path.Combine(thisDir, "AllO.Shared.dll");

            // Sheet Manager button
            var sheetBtn = new PushButtonData(
                "SheetManager", "Sheet\nManager",
                sharedAssemblyPath, "AllO.Commands.MainCommand");
            if (panel.AddItem(sheetBtn) is PushButton sb)
            {
                sb.ToolTip = "Manage sheets, views and revisions.";
                sb.LongDescription = "AllO Sheet Manager. Professional tool for managing sheets with search, filters and batch operations.";
            }

            // Publish button
            var publishBtn = new PushButtonData(
                "Publish", "Publish",
                sharedAssemblyPath, "AllO.Commands.PublishCommand");
            if (panel.AddItem(publishBtn) is PushButton pb)
            {
                pb.ToolTip = "Export sheets to PDF and/or DWG.";
                pb.LongDescription = "AllO Publish. Batch export selected sheets to PDF and DWG formats with naming rules and output folder selection.";
            }

            // Toolkit button
            var toolkitBtn = new PushButtonData(
                "Toolkit", "Toolkit",
                sharedAssemblyPath, "AllO.Commands.SuperCommand");
            if (panel.AddItem(toolkitBtn) is PushButton tb)
            {
                tb.ToolTip = "Copy Crop, Families, Grids, Levels — all in one.";
                tb.LongDescription = "AllO Toolkit. Multi-tool for crop regions, family management, grids and levels operations.";
            }

            // Table Gen button
            var tableBtn = new PushButtonData(
                "TableGen", "Table\nGen",
                sharedAssemblyPath, "AllO.Commands.TableGenCommand");
            if (panel.AddItem(tableBtn) is PushButton tg)
            {
                tg.ToolTip = "Generate and manage schedule tables.";
                tg.LongDescription = "AllO Table Gen. Create schedule templates, manage existing schedules, and place them on sheets.";
            }

            // Productivity panel
            RibbonPanel prodPanel = application.CreateRibbonPanel(TabName, "Productivity");

            // Color Coder button
            var colorBtn = new PushButtonData(
                "ColorCoder", "Color\nCoder",
                sharedAssemblyPath, "AllO.Commands.ColorCoderCommand");
            if (prodPanel.AddItem(colorBtn) is PushButton cb)
            {
                cb.ToolTip = "Color-code views by document for multi-model workflows.";
                cb.LongDescription = "AllO Color Coder. Assign colors to open documents to visually identify which model each view belongs to.";
            }

            // Align button
            var alignBtn = new PushButtonData(
                "LevelAlign", "Level\nAlign",
                sharedAssemblyPath, "AllO.Commands.AlignCommand");
            if (prodPanel.AddItem(alignBtn) is PushButton ab)
                ab.ToolTip = "Align MEP element elevation to a reference element.";

            // Connector button
            var connBtn = new PushButtonData(
                "Connector", "Connector",
                sharedAssemblyPath, "AllO.Commands.ConnectorCommand");
            if (prodPanel.AddItem(connBtn) is PushButton cn)
                cn.ToolTip = "Select 2 MEP elements to align and connect them.";

            // Quick Search button
            var searchBtn = new PushButtonData(
                "QuickSearch", "Quick\nSearch",
                sharedAssemblyPath, "AllO.Commands.QuickSearchCommand");
            if (prodPanel.AddItem(searchBtn) is PushButton qb)
            {
                qb.ToolTip = "Search elements by name, ID, category, family or parameter.";
                qb.LongDescription = "AllO Quick Search. Spotlight-like search to find, select and isolate elements instantly.";
            }

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("AllO - Error",
                $"Failed to initialize AllO Add-in.\n\n{ex.Message}\n\n{ex.StackTrace}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}

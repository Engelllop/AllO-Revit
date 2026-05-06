using Autodesk.Revit.UI;
using AllO.Helpers;
using AllO.Services;
using AllO.UI.Styles;

namespace AllO.Core;

/// <summary>
/// Inicialización compartida entre Revit2023/2024/2025. Cada App.OnStartup
/// registra su RevitServiceFactory y delega aquí.
///
/// Construye el ribbon con paneles agrupados (pulldowns) y configura DI,
/// settings y tema.
/// </summary>
public static class AppBootstrap
{
    private const string TabName = "AllO";

    public static void Initialize(UIControlledApplication application)
    {
        // Settings + tema
        ThemeManager.Apply();
        ServiceLocator.RegisterSingleton(AllOSettings.Current);

        // RevitAsyncHelper debe crearse desde OnStartup (registra ExternalEvent).
        var asyncHelper = new AllO.Services.RevitAsyncHelper();
        ServiceLocator.RegisterSingleton(asyncHelper);

        // Color Coder hooks
        ColorCoderOverlayHost.Register(application);

        // Ribbon
        application.CreateRibbonTab(TabName);
        BuildDocumentationPanel(application);
        BuildProductivityPanel(application);
    }

    public static void Shutdown(UIControlledApplication application)
    {
        ColorCoderOverlayHost.Shutdown();
    }

    private static void BuildDocumentationPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Documentation");

        // PulldownButton "Sheets" agrupando Sheet Manager + Publish
        var sheetsPulldown = new PulldownButtonData("SheetsGroup", "Sheets");
        if (panel.AddItem(sheetsPulldown) is PulldownButton pd)
        {
            RibbonBuilder.Configure(pd, "", "Sheet management tools");

            var smBtn = RibbonBuilder.Button("SheetManager", "Sheet Manager",
                "AllO.Commands.MainCommand");
            if (pd.AddPushButton(smBtn) is PushButton sm)
                RibbonBuilder.Configure(sm, "",
                    "Manage sheets, views and revisions.",
                    "AllO Sheet Manager. Search, filter and batch ops on sheets.");

            var pubBtn = RibbonBuilder.Button("Publish", "Publish",
                "AllO.Commands.PublishCommand");
            if (pd.AddPushButton(pubBtn) is PushButton pub)
                RibbonBuilder.Configure(pub, "",
                    "Export sheets to PDF and/or DWG.",
                    "AllO Publish. Batch export with naming rules.");
        }

        var toolkitBtn = RibbonBuilder.Button("Toolkit", "Toolkit",
            "AllO.Commands.SuperCommand");
        if (panel.AddItem(toolkitBtn) is PushButton tb)
            RibbonBuilder.Configure(tb, "",
                "Crop, Families, Grids, Levels — all in one.",
                "AllO Toolkit. Multi-tool for crops, families, grids and levels.");

        var tableBtn = RibbonBuilder.Button("TableGen", "Table\nGen",
            "AllO.Commands.TableGenCommand");
        if (panel.AddItem(tableBtn) is PushButton tg)
            RibbonBuilder.Configure(tg, "",
                "Generate and manage schedule tables.",
                "AllO Table Gen. Schedules, templates and sheet placement.");
    }

    private static void BuildProductivityPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Productivity");

        var colorBtn = RibbonBuilder.Button("ColorCoder", "Color\nCoder",
            "AllO.Commands.ColorCoderCommand");
        var alignBtn = RibbonBuilder.Button("LevelAlign", "Level\nAlign",
            "AllO.Commands.AlignCommand");
        var connBtn  = RibbonBuilder.Button("Connector", "Connector",
            "AllO.Commands.ConnectorCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(colorBtn, alignBtn, connBtn);

        if (stack.Count >= 1 && stack[0] is PushButton cb)
            RibbonBuilder.Configure(cb, "",
                "1st click: tint views per model. 2nd click: settings.",
                "AllO Color Coder. Per-document tint on view windows.");

        if (stack.Count >= 2 && stack[1] is PushButton ab)
            RibbonBuilder.Configure(ab, "",
                "Align MEP element elevation to a reference.",
                "AllO Align. MEP elevation alignment to a reference element.");

        if (stack.Count >= 3 && stack[2] is PushButton cn)
            RibbonBuilder.Configure(cn, "",
                "Select 2 MEP elements and connect them.",
                "AllO Connector. Aligns and connects MEP elements.");

        var linkFamilyBtn = RibbonBuilder.Button("LinkFamily", "Link\nFamily",
            "AllO.Commands.FamilyTransferCommand");
        if (panel.AddItem(linkFamilyBtn) is PushButton lfb)
            RibbonBuilder.Configure(lfb, "",
                "Copy a family type from a linked model into the host.",
                "AllO Link Family. Copies instances with link transform.");

        var linkVisBtn = RibbonBuilder.Button("LinkVisibility", "Link\nVisibility",
            "AllO.Commands.LinkVisibilityCommand");
        if (panel.AddItem(linkVisBtn) is PushButton lvb)
            RibbonBuilder.Configure(lvb, "",
                "Control visibility of linked model categories across multiple views.",
                "AllO Link Visibility. Select a linked Revit model, choose categories to show/hide/halftone, and apply to multiple views at once.");
    }
}

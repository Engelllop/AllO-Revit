using Autodesk.Revit.UI;
using AllO.Helpers;
using AllO.Services;
using AllO.UI.Styles;

namespace AllO.Core;

/// <summary>
/// Inicialización compartida entre Revit2023/2024/2025. Cada App.OnStartup
/// registra su RevitServiceFactory y delega aquí.
///
/// Construye el ribbon con paneles agrupados y botones individuales, y configura DI,
/// settings y tema.
/// </summary>
public static class AppBootstrap
{
    private const string TabName = "AllO";

    public static void Initialize(UIControlledApplication application)
    {
        StartupLog.Clear();
        StartupLog.Write("AppBootstrap.Initialize start");

        // Settings + tema
        StartupLog.Write("  ThemeManager.Apply...");
        ThemeManager.Apply();
        StartupLog.Write("  ThemeManager.Apply done");

        StartupLog.Write("  Register settings...");
        ServiceLocator.RegisterSingleton(AllOSettings.Current);
        StartupLog.Write("  Register settings done");

        // RevitAsyncHelper debe crearse desde OnStartup (registra ExternalEvent).
        StartupLog.Write("  Create RevitAsyncHelper...");
        var asyncHelper = new AllO.Services.RevitAsyncHelper();
        ServiceLocator.RegisterSingleton(asyncHelper);
        StartupLog.Write("  Create RevitAsyncHelper done");

        // Color Coder hooks
        StartupLog.Write("  ColorCoderOverlayHost.Register...");
        ColorCoderOverlayHost.Register(application);
        StartupLog.Write("  ColorCoderOverlayHost.Register done");

        // Ribbon
        StartupLog.Write("  CreateRibbonTab...");
        application.CreateRibbonTab(TabName);
        StartupLog.Write("  CreateRibbonTab done");

        StartupLog.Write("  BuildDocumentationPanel...");
        BuildDocumentationPanel(application);
        StartupLog.Write("  BuildDocumentationPanel done");

        StartupLog.Write("  BuildViewsPanel...");
        BuildViewsPanel(application);
        StartupLog.Write("  BuildViewsPanel done");

        StartupLog.Write("  BuildProductivityPanel...");
        BuildProductivityPanel(application);
        StartupLog.Write("  BuildProductivityPanel done");

        StartupLog.Write("  BuildMepPanel...");
        BuildMepPanel(application);
        StartupLog.Write("  BuildMepPanel done");

        StartupLog.Write("  BuildToolsPanel...");
        BuildToolsPanel(application);
        StartupLog.Write("  BuildToolsPanel done");

        StartupLog.Write("  BuildCoordinationPanel...");
        BuildCoordinationPanel(application);
        StartupLog.Write("  BuildCoordinationPanel done");

        StartupLog.Write("  BuildAuditPanel...");
        BuildAuditPanel(application);
        StartupLog.Write("  BuildAuditPanel done");

        StartupLog.Write("  BuildLinksPanel...");
        BuildLinksPanel(application);
        StartupLog.Write("  BuildLinksPanel done");

        StartupLog.Write("AppBootstrap.Initialize complete");
    }

    public static void Shutdown(UIControlledApplication application)
    {
        ColorCoderOverlayHost.Shutdown();
    }

    private static void BuildDocumentationPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Documentation");

        // Sheet Manager — SplitButton: main click = Sheet List, dropdown = View List / Revisions
        var smSplitData = new SplitButtonData("SheetManager", "Sheet\nManager");
        if (panel.AddItem(smSplitData) is SplitButton smSplit)
        {
            RibbonBuilder.Configure(smSplit, "sheetList", "Manage sheets, views and revisions.");

            var sheetListBtn = smSplit.AddPushButton(
                new PushButtonData("SheetList", "Sheet List", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.SheetListCommand"));
            RibbonBuilder.Configure(sheetListBtn, "sheetList",
                "Open the sheet list panel.",
                "AllO Sheet List. Search, filter and batch operations on sheets.");

            var viewListBtn = smSplit.AddPushButton(
                new PushButtonData("ViewList", "View List", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.SheetViewListCommand"));
            RibbonBuilder.Configure(viewListBtn, "viewList",
                "Open the view list panel.",
                "AllO View List. Manage views and their placement on sheets.");

            var revBtn = smSplit.AddPushButton(
                new PushButtonData("Revisions", "Revisions", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.SheetRevisionsCommand"));
            RibbonBuilder.Configure(revBtn, "revisions",
                "Open the revisions panel.",
                "AllO Revisions. Create, edit and assign revision clouds.");
        }

        // Publish
        var pubBtn = RibbonBuilder.Button("Publish", "Publish",
            "AllO.Commands.PublishCommand");
        if (panel.AddItem(pubBtn) is PushButton pub)
            RibbonBuilder.Configure(pub, "publish",
                "Export sheets to PDF and/or DWG.",
                "AllO Publish. Batch export with naming rules.");

        // TableGen
        var tableBtn = RibbonBuilder.Button("TableGen", "Table\nGen",
            "AllO.Commands.TableGenCommand");
        if (panel.AddItem(tableBtn) is PushButton tg)
            RibbonBuilder.Configure(tg, "tableGen",
                "Generate and manage schedule tables.",
                "AllO Table Gen. Schedules, templates and sheet placement.");
    }

    private static void BuildViewsPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Views");

        var cropBtn = RibbonBuilder.Button("CopyCrop", "Copy\nCrop",
            "AllO.Commands.CropCommand");
        var gridBtn = RibbonBuilder.Button("Grids", "Grids",
            "AllO.Commands.GridCommand");
        var levelBtn = RibbonBuilder.Button("Levels", "Levels",
            "AllO.Commands.LevelCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(cropBtn, gridBtn, levelBtn);

        if (stack.Count >= 1 && stack[0] is PushButton cb)
            RibbonBuilder.Configure(cb, "copyCrop",
                "Copy crop region from a source view to selected targets.",
                "AllO Copy Crop. Batch crop replication across views.");

        if (stack.Count >= 2 && stack[1] is PushButton gb)
            RibbonBuilder.Configure(gb, "grids",
                "Rename, delete, copy and sync grids from linked models.",
                "AllO Grids. Grid management with link sync support.");

        if (stack.Count >= 3 && stack[2] is PushButton lb)
            RibbonBuilder.Configure(lb, "levels",
                "Rename, move, delete, copy and sync levels from linked models.",
                "AllO Levels. Level management with elevation offsets and link sync.");

        var sectionBoxBtn = RibbonBuilder.Button("AutoSectionBox", "Section\nBox",
            "AllO.Commands.AutoSectionBoxCommand");
        if (panel.AddItem(sectionBoxBtn) is PushButton sb)
            RibbonBuilder.Configure(sb, "autoSectionBox",
                "Create a 3D view with a section box fit to the selected elements.",
                "AllO Auto Section Box. Aísla la selección en una vista 3D con section box ajustado.");
    }

    private static void BuildProductivityPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Productivity");

        // Row 1: ColorCoder, MatchElevation, ParameterPush
        var colorBtn = RibbonBuilder.Button("ColorCoder", "Color\nCoder",
            "AllO.Commands.ColorCoderCommand");
        var matchElevBtn = RibbonBuilder.Button("MatchElev", "Match\nElevation",
            "AllO.Commands.MatchElevationCommand");
        var paramPushBtn = RibbonBuilder.Button("ParamPush", "Param\nPush",
            "AllO.Commands.ParameterPushCommand");

        IList<RibbonItem> stack1 = panel.AddStackedItems(colorBtn, matchElevBtn, paramPushBtn);

        if (stack1.Count >= 1 && stack1[0] is PushButton cb)
            RibbonBuilder.Configure(cb, "colorCoder",
                "1st click: tint views per model. 2nd click: settings.",
                "AllO Color Coder. Per-document tint on view windows.");

        if (stack1.Count >= 2 && stack1[1] is PushButton me)
            RibbonBuilder.Configure(me, "matchElev",
                "Match elevation of selected elements to a reference element.",
                "AllO Match Elevation. Snap MEP elements to the same Z height.");

        if (stack1.Count >= 3 && stack1[2] is PushButton pp)
            RibbonBuilder.Configure(pp, "paramPush",
                "Push a parameter value from a source element to multiple targets.",
                "AllO Parameter Push. Mass-copy parameter values across elements.");
    }

    private static void BuildMepPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "MEP");

        // Bloom + RerouteAround stacked
        var bloomBtn = RibbonBuilder.Button("Bloom", "Bloom",
            "AllO.Commands.BloomCommand");
        var rerouteBtn = RibbonBuilder.Button("Reroute", "Reroute\nAround",
            "AllO.Commands.RerouteAroundCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(bloomBtn, rerouteBtn);

        if (stack.Count >= 1 && stack[0] is PushButton bb)
            RibbonBuilder.Configure(bb, "bloom",
                "Auto-create MEP stubs from all connectors on selected equipment.",
                "AllO Bloom. Generate pipes/ducts from equipment connectors instantly.");

        if (stack.Count >= 2 && stack[1] is PushButton rb)
            RibbonBuilder.Configure(rb, "reroute",
                "Reroute a pipe or duct around an obstacle with a directional offset.",
                "AllO Reroute Around. Create directional bypasses on MEP curves.");

        // Connector, MultiConnect, SplitPipe (movidos desde Productivity: son MEP)
        var connBtn = RibbonBuilder.Button("Connector", "Connector",
            "AllO.Commands.ConnectorCommand");
        var multiConnBtn = RibbonBuilder.Button("MultiConnect", "Multi\nConnect",
            "AllO.Commands.MultiConnectCommand");
        var splitBtn = RibbonBuilder.Button("SplitPipe", "Split\nPipe",
            "AllO.Commands.SplitPipeCommand");

        IList<RibbonItem> connStack = panel.AddStackedItems(connBtn, multiConnBtn, splitBtn);

        if (connStack.Count >= 1 && connStack[0] is PushButton c)
            RibbonBuilder.Configure(c, "connector",
                "Select 2 MEP elements and connect them.",
                "AllO Connector. Aligns and connects MEP elements.");

        if (connStack.Count >= 2 && connStack[1] is PushButton mc)
            RibbonBuilder.Configure(mc, "multiConnect",
                "Connect multiple terminals to a main pipe or duct at once.",
                "AllO Multi-Connect. Batch-connect MEP terminals to a main branch.");

        if (connStack.Count >= 3 && connStack[2] is PushButton sp)
            RibbonBuilder.Configure(sp, "splitPipe",
                "Split a pipe or duct at a point, optionally with a gap.",
                "AllO Split Pipe. Divide MEP curves with optional expansion gap.");

        // Elbow Direction pulldown
        var elbowData = new PulldownButtonData("ElbowDir", "Elbow\nDir");
        if (panel.AddItem(elbowData) is PulldownButton elbowPull)
        {
            RibbonBuilder.Configure(elbowPull, "elbowDir", "Place an elbow in a specific direction.");

            var left = elbowPull.AddPushButton(
                new PushButtonData("ElbowLeft", "Left", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.ElbowLeftCommand"));
            RibbonBuilder.Configure(left, "elbowDir", "Place elbow pointing Left.", "AllO Elbow Left.");

            var right = elbowPull.AddPushButton(
                new PushButtonData("ElbowRight", "Right", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.ElbowRightCommand"));
            RibbonBuilder.Configure(right, "elbowDir", "Place elbow pointing Right.", "AllO Elbow Right.");

            var up = elbowPull.AddPushButton(
                new PushButtonData("ElbowUp", "Up", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.ElbowUpCommand"));
            RibbonBuilder.Configure(up, "elbowDir", "Place elbow pointing Up.", "AllO Elbow Up.");

            var down = elbowPull.AddPushButton(
                new PushButtonData("ElbowDown", "Down", RibbonBuilder.SharedAssemblyPath(), "AllO.Commands.ElbowDownCommand"));
            RibbonBuilder.Configure(down, "elbowDir", "Place elbow pointing Down.", "AllO Elbow Down.");
        }

        var netTreeBtn = RibbonBuilder.Button("NetworkTree", "Network\nTree",
            "AllO.Commands.NetworkTreeCommand");
        var powerEachBtn = RibbonBuilder.Button("PowerEach", "Power\nEach",
            "AllO.Commands.PowerEachCommand");

        IList<RibbonItem> netStack = panel.AddStackedItems(netTreeBtn, powerEachBtn);

        if (netStack.Count >= 1 && netStack[0] is PushButton nt)
            RibbonBuilder.Configure(nt, "netTree",
                "Analyze a connected MEP network: lengths per run, elbows, tees/wyes, as a tree diagram.",
                "AllO Network Tree. Pick any pipe/duct/conduit element and get total length, longest run, " +
                "fitting counts and a branch-by-branch breakdown. Export to CSV.");

        if (netStack.Count >= 2 && netStack[1] is PushButton pe)
            RibbonBuilder.Configure(pe, "powerEach",
                "Create ONE power circuit PER selected element and assign them all to a panel.",
                "AllO Power Each. Native Revit puts the whole selection into a single circuit; " +
                "this creates an independent circuit for each element on the chosen panel.");
    }

    private static void BuildToolsPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Tools");

        var oneFilterBtn = RibbonBuilder.Button("OneFilter", "One\nFilter",
            "AllO.Commands.OneFilterCommand");
        var reorderBtn = RibbonBuilder.Button("ReOrdering", "Re\nOrdering",
            "AllO.Commands.ReOrderingCommand");
        var familyExpBtn = RibbonBuilder.Button("FamilyExport", "Family\nExport",
            "AllO.Commands.FamilyExportCommand");
        var viewManBtn = RibbonBuilder.Button("ViewManager", "View\nManager",
            "AllO.Commands.ViewManagerCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(oneFilterBtn, reorderBtn, familyExpBtn);

        if (stack.Count >= 1 && stack[0] is PushButton of)
            RibbonBuilder.Configure(of, "oneFilter",
                "Advanced filter: select elements by parameter + operator + value.",
                "AllO OneFilter. Parameter-driven selection with operators.");

        if (stack.Count >= 2 && stack[1] is PushButton ro)
            RibbonBuilder.Configure(ro, "reOrdering",
                "Renumber elements sequentially along a drawn path.",
                "AllO ReOrdering. Sequential numbering with prefix/suffix.");

        if (stack.Count >= 3 && stack[2] is PushButton fe)
            RibbonBuilder.Configure(fe, "familyExport",
                "Export all loaded families to a destination folder.",
                "AllO Family Export. Bulk family library extraction.");

        if (panel.AddItem(viewManBtn) is PushButton vm)
            RibbonBuilder.Configure(vm, "viewManager",
                "Batch-create views and sheets from levels with templates.",
                "AllO View Manager. Mass view/sheet creation.");
    }

    private static void BuildAuditPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Cleanup");

        var wipeBtn = RibbonBuilder.Button("ModelWipe", "Wipe",
            "AllO.Commands.WipeCommand");
        if (panel.AddItem(wipeBtn) is PushButton wb)
            RibbonBuilder.Configure(wb, "wipe",
                "Deep model cleanup: unused views, sheets, templates, filters, imports, patterns.",
                "AllO Wipe. Purge what Revit's native Purge cannot reach.");
    }

    private static void BuildCoordinationPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Coordination");

        var syncBtn = RibbonBuilder.Button("SyncViews", "Sync\nViews",
            "AllO.Commands.SyncViewsCommand");
        var copyStateBtn = RibbonBuilder.Button("CopyState", "Copy\nState",
            "AllO.Commands.CopyStateCommand");
        var pasteStateBtn = RibbonBuilder.Button("PasteState", "Paste\nState",
            "AllO.Commands.PasteStateCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(syncBtn, copyStateBtn, pasteStateBtn);

        if (stack.Count >= 1 && stack[0] is PushButton sv)
            RibbonBuilder.Configure(sv, "syncViews",
                "Synchronize navigation between two open views in real time.",
                "AllO Sync Views. Link pan/zoom of master and slave views.");

        if (stack.Count >= 2 && stack[1] is PushButton cs)
            RibbonBuilder.Configure(cs, "copyState",
                "Copy visibility state (worksets, categories, filters) from current view.",
                "AllO Copy State. Snapshot view visibility configuration.");

        if (stack.Count >= 3 && stack[2] is PushButton ps)
            RibbonBuilder.Configure(ps, "pasteState",
                "Paste copied visibility state into the current view.",
                "AllO Paste State. Apply visibility snapshot without templates.");

        var matchBtn = RibbonBuilder.Button("Match", "Match\nProps",
            "AllO.Commands.MatchCommand");
        if (panel.AddItem(matchBtn) is PushButton mb)
            RibbonBuilder.Configure(mb, "match",
                "Match specific parameters from source to multiple targets.",
                "AllO Match. Selective parameter transfer beyond Revit's native Match.");
    }

    private static void BuildLinksPanel(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel(TabName, "Links");

        var linkFamilyBtn = RibbonBuilder.Button("LinkFamily", "Link\nFamily",
            "AllO.Commands.FamilyTransferCommand");
        var linkVisBtn = RibbonBuilder.Button("LinkVisibility", "Link\nVisibility",
            "AllO.Commands.LinkVisibilityCommand");

        IList<RibbonItem> stack = panel.AddStackedItems(linkFamilyBtn, linkVisBtn);

        if (stack.Count >= 1 && stack[0] is PushButton lfb)
            RibbonBuilder.Configure(lfb, "linkFamily",
                "Copy a family type from a linked model into the host.",
                "AllO Link Family. Copies instances with link transform.");

        if (stack.Count >= 2 && stack[1] is PushButton lvb)
            RibbonBuilder.Configure(lvb, "linkVisibility",
                "Control visibility of linked model categories across multiple views.",
                "AllO Link Visibility. Select a linked Revit model, choose categories to show/hide/halftone, and apply to multiple views at once.");
    }
}

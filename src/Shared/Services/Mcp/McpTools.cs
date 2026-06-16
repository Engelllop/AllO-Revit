using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace AllO.Services.Mcp;

/// <summary>
/// Tools MCP v1 (solo lectura + set_parameter/selección). Cada llamada corre en contexto
/// Revit vía <see cref="McpRevitExecutor"/>. Devuelven grafos dict/list serializables por JsonLite.
/// </summary>
public static class McpTools
{
#if NET48
#pragma warning disable CS0618 // Shared net48 corre también en Revit 2023, que no tiene ElementId.Value/(long)
    private static long IdOf(Element e) => e.Id.IntegerValue;
    private static ElementId MakeId(long v) => new((int)v);
#pragma warning restore CS0618
#else
    private static long IdOf(Element e) => e.Id.Value;
    private static ElementId MakeId(long v) => new(v);
#endif

    private static Dictionary<string, object?> Tool(string name, string description, Dictionary<string, object?> properties, params string[] required)
        => new()
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required.ToList()
            }
        };

    private static Dictionary<string, object?> Prop(string type, string description)
        => new() { ["type"] = type, ["description"] = description };

    public static List<object?> ListToolDefs() => new()
    {
        Tool("get_model_info", "Info del modelo Revit activo: título, ruta, vista activa y conteos básicos.",
            new Dictionary<string, object?>()),
        Tool("list_elements", "Lista instancias de una categoría (p.ej. 'Walls', 'Pipes', 'OST_Doors'). Devuelve id, nombre, tipo y nivel.",
            new Dictionary<string, object?>
            {
                ["category"] = Prop("string", "Nombre de categoría Revit en inglés o BuiltInCategory (con o sin prefijo OST_)."),
                ["limit"] = Prop("number", "Máximo de elementos a devolver (default 50).")
            }, "category"),
        Tool("get_element", "Detalle de un elemento: categoría, tipo y todos sus parámetros con valores.",
            new Dictionary<string, object?>
            {
                ["element_id"] = Prop("number", "ElementId del elemento.")
            }, "element_id"),
        Tool("set_parameter", "Escribe un parámetro de instancia de un elemento (dentro de una transacción).",
            new Dictionary<string, object?>
            {
                ["element_id"] = Prop("number", "ElementId del elemento."),
                ["parameter_name"] = Prop("string", "Nombre visible del parámetro."),
                ["value"] = Prop("string", "Valor nuevo como texto; se convierte según el StorageType.")
            }, "element_id", "parameter_name", "value"),
        Tool("list_views", "Lista las vistas del modelo (sin plantillas): id, nombre y tipo.",
            new Dictionary<string, object?>()),
        Tool("get_selection", "Elementos actualmente seleccionados en Revit.",
            new Dictionary<string, object?>()),
        Tool("get_geometry", "Geometría (en mm) de los elementos seleccionados, o de los element_ids dados. Devuelve por cada uno: línea con extremos {start,end} o punto {point}. Úsalo para leer una línea/detail item que dibujaste y replicarla como tubería/ducto/conduit, o para colocar familias a lo largo de ella.",
            new Dictionary<string, object?>
            {
                ["element_ids"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "number" },
                    ["description"] = "ElementIds (opcional). Si se omite, usa la selección actual."
                }
            }),
        Tool("select_elements", "Selecciona (y resalta) los elementos dados en la UI de Revit.",
            new Dictionary<string, object?>
            {
                ["element_ids"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "number" },
                    ["description"] = "ElementIds a seleccionar."
                }
            }, "element_ids"),
        Tool("rename_element", "Renombra un elemento (vista, lámina, tipo, etc.) vía Element.Name.",
            new Dictionary<string, object?>
            {
                ["element_id"] = Prop("number", "ElementId del elemento."),
                ["name"] = Prop("string", "Nombre nuevo.")
            }, "element_id", "name"),
        Tool("delete_elements", "Elimina elementos del modelo (transacción única; la vista activa se omite por seguridad). Deshacer con Ctrl+Z en Revit.",
            new Dictionary<string, object?>
            {
                ["element_ids"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "number" },
                    ["description"] = "ElementIds a eliminar."
                }
            }, "element_ids"),
        Tool("list_types", "Lista los TIPOS disponibles de una categoría (p.ej. 'Pipes', 'PipingSystem', 'ElectricalFixtures', 'PlumbingFixtures'). Úsalo para descubrir qué pasar a create_pipe / place_family_instance.",
            new Dictionary<string, object?>
            {
                ["category"] = Prop("string", "Categoría Revit en inglés o BuiltInCategory (con o sin OST_). Para tuberías usa 'Pipes'; para sistemas 'PipingSystem'."),
                ["limit"] = Prop("number", "Máximo de tipos a devolver (default 100).")
            }, "category"),
        Tool("create_pipe", "Crea una tubería entre dos puntos (coordenadas en MILÍMETROS). Si no das tipos usa el primero disponible. Devuelve el id de la tubería creada.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."),
                ["y1"] = Prop("number", "Y inicial (mm)."),
                ["z1"] = Prop("number", "Z inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."),
                ["y2"] = Prop("number", "Y final (mm)."),
                ["z2"] = Prop("number", "Z final (mm)."),
                ["diameter"] = Prop("number", "Diámetro nominal en mm (opcional)."),
                ["level"] = Prop("string", "Nombre del nivel (opcional; default = nivel más bajo)."),
                ["pipe_type"] = Prop("string", "Nombre del tipo de tubería (opcional; default = primero)."),
                ["system_type"] = Prop("string", "Nombre del tipo de sistema de tubería (opcional; default = primero).")
            }, "x1", "y1", "z1", "x2", "y2", "z2"),
        Tool("place_family_instance", "Coloca una instancia de familia (cualquier categoría: equipos eléctricos, luminarias, dispositivos, aparatos sanitarios, mobiliario, genéricos…) en un punto (coordenadas en MILÍMETROS). Descubre nombres con list_types.",
            new Dictionary<string, object?>
            {
                ["type_name"] = Prop("string", "Nombre del tipo (FamilySymbol) a colocar."),
                ["family_name"] = Prop("string", "Nombre de la familia (opcional, para desambiguar si hay tipos homónimos)."),
                ["x"] = Prop("number", "X (mm)."),
                ["y"] = Prop("number", "Y (mm)."),
                ["z"] = Prop("number", "Z (mm)."),
                ["level"] = Prop("string", "Nombre del nivel (opcional; default = nivel más bajo).")
            }, "type_name", "x", "y", "z"),
        Tool("place_hosted_family", "Coloca una familia HOSPEDADA (puertas, ventanas, luminarias en muro/techo…) sobre un elemento anfitrión. Punto en MILÍMETROS sobre el host.",
            new Dictionary<string, object?>
            {
                ["type_name"] = Prop("string", "Nombre del tipo (FamilySymbol)."),
                ["family_name"] = Prop("string", "Nombre de la familia (opcional)."),
                ["host_id"] = Prop("number", "ElementId del anfitrión (p.ej. el muro)."),
                ["x"] = Prop("number", "X (mm)."),
                ["y"] = Prop("number", "Y (mm)."),
                ["z"] = Prop("number", "Z (mm).")
            }, "type_name", "host_id", "x", "y", "z"),
        Tool("create_duct", "Crea un conducto (duct) entre dos puntos (mm). Tipos opcionales.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."), ["y1"] = Prop("number", "Y inicial (mm)."), ["z1"] = Prop("number", "Z inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."), ["y2"] = Prop("number", "Y final (mm)."), ["z2"] = Prop("number", "Z final (mm)."),
                ["level"] = Prop("string", "Nivel (opcional)."), ["duct_type"] = Prop("string", "Tipo de conducto (opcional)."), ["system_type"] = Prop("string", "Tipo de sistema mecánico (opcional).")
            }, "x1", "y1", "z1", "x2", "y2", "z2"),
        Tool("create_conduit", "Crea un tubo conduit eléctrico entre dos puntos (mm). Tipo opcional.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."), ["y1"] = Prop("number", "Y inicial (mm)."), ["z1"] = Prop("number", "Z inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."), ["y2"] = Prop("number", "Y final (mm)."), ["z2"] = Prop("number", "Z final (mm)."),
                ["level"] = Prop("string", "Nivel (opcional)."), ["conduit_type"] = Prop("string", "Tipo de conduit (opcional).")
            }, "x1", "y1", "z1", "x2", "y2", "z2"),
        Tool("create_cable_tray", "Crea una bandeja de cables (cable tray) entre dos puntos (mm). Tipo opcional.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."), ["y1"] = Prop("number", "Y inicial (mm)."), ["z1"] = Prop("number", "Z inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."), ["y2"] = Prop("number", "Y final (mm)."), ["z2"] = Prop("number", "Z final (mm)."),
                ["level"] = Prop("string", "Nivel (opcional)."), ["tray_type"] = Prop("string", "Tipo de bandeja (opcional).")
            }, "x1", "y1", "z1", "x2", "y2", "z2"),
        Tool("create_wall", "Crea un muro recto entre dos puntos (mm) con altura. Tipo y nivel opcionales.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."), ["y1"] = Prop("number", "Y inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."), ["y2"] = Prop("number", "Y final (mm)."),
                ["height"] = Prop("number", "Altura del muro en mm."),
                ["base_offset"] = Prop("number", "Desfase de base en mm (opcional)."),
                ["level"] = Prop("string", "Nivel base (opcional)."), ["wall_type"] = Prop("string", "Tipo de muro (opcional).")
            }, "x1", "y1", "x2", "y2", "height"),
        Tool("create_floor", "Crea un piso a partir de un contorno cerrado de puntos (mm) en planta. Tipo y nivel opcionales.",
            new Dictionary<string, object?>
            {
                ["points"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "object" },
                    ["description"] = "Lista de vértices {x, y} en mm, en orden, contorno cerrado (mín. 3)."
                },
                ["level"] = Prop("string", "Nivel (opcional)."), ["floor_type"] = Prop("string", "Tipo de piso (opcional).")
            }, "points"),
        Tool("create_grid", "Crea un eje (grid) recto entre dos puntos (mm). Nombre opcional.",
            new Dictionary<string, object?>
            {
                ["x1"] = Prop("number", "X inicial (mm)."), ["y1"] = Prop("number", "Y inicial (mm)."),
                ["x2"] = Prop("number", "X final (mm)."), ["y2"] = Prop("number", "Y final (mm)."),
                ["name"] = Prop("string", "Nombre del eje (opcional).")
            }, "x1", "y1", "x2", "y2"),
        Tool("create_level", "Crea un nivel a una elevación dada (mm). Nombre opcional.",
            new Dictionary<string, object?>
            {
                ["elevation"] = Prop("number", "Elevación en mm."),
                ["name"] = Prop("string", "Nombre del nivel (opcional).")
            }, "elevation"),
        Tool("create_room", "Crea una habitación (room) en un punto (mm) sobre un nivel. Nombre opcional.",
            new Dictionary<string, object?>
            {
                ["x"] = Prop("number", "X (mm)."), ["y"] = Prop("number", "Y (mm)."),
                ["level"] = Prop("string", "Nivel (opcional)."), ["name"] = Prop("string", "Nombre del room (opcional).")
            }, "x", "y"),
        Tool("create_view", "Crea una vista. view_type: floor_plan | ceiling_plan | 3d | drafting. floor/ceiling requieren nivel.",
            new Dictionary<string, object?>
            {
                ["view_type"] = Prop("string", "floor_plan | ceiling_plan | 3d | drafting."),
                ["level"] = Prop("string", "Nivel (para floor_plan/ceiling_plan; opcional, default más bajo)."),
                ["name"] = Prop("string", "Nombre de la vista (opcional).")
            }, "view_type"),
        Tool("duplicate_view", "Duplica una vista existente. with_detailing copia también las anotaciones.",
            new Dictionary<string, object?>
            {
                ["view_id"] = Prop("number", "ElementId de la vista a duplicar."),
                ["with_detailing"] = Prop("boolean", "true = WithDetailing; false = Duplicate (default)."),
                ["name"] = Prop("string", "Nombre de la copia (opcional).")
            }, "view_id"),
        Tool("create_sheet", "Crea una lámina (sheet). Title block opcional (por nombre de tipo).",
            new Dictionary<string, object?>
            {
                ["number"] = Prop("string", "Número de lámina (opcional)."),
                ["name"] = Prop("string", "Nombre de lámina (opcional)."),
                ["title_block"] = Prop("string", "Nombre del tipo de title block (opcional; default = primero o ninguno).")
            }),
        Tool("place_view_on_sheet", "Coloca una vista en una lámina (crea un viewport). Punto centro en mm (opcional).",
            new Dictionary<string, object?>
            {
                ["sheet_id"] = Prop("number", "ElementId de la lámina."),
                ["view_id"] = Prop("number", "ElementId de la vista."),
                ["x"] = Prop("number", "X del centro en mm (opcional)."), ["y"] = Prop("number", "Y del centro en mm (opcional).")
            }, "sheet_id", "view_id"),
        Tool("create_text_note", "Crea un texto en una vista, en un punto (mm).",
            new Dictionary<string, object?>
            {
                ["view_id"] = Prop("number", "ElementId de la vista."),
                ["x"] = Prop("number", "X (mm)."), ["y"] = Prop("number", "Y (mm)."),
                ["text"] = Prop("string", "Contenido del texto.")
            }, "view_id", "x", "y", "text"),
        Tool("move_elements", "Mueve elementos por un vector (mm).",
            new Dictionary<string, object?>
            {
                ["element_ids"] = new Dictionary<string, object?> { ["type"] = "array", ["items"] = new Dictionary<string, object?> { ["type"] = "number" }, ["description"] = "ElementIds a mover." },
                ["dx"] = Prop("number", "Δx (mm)."), ["dy"] = Prop("number", "Δy (mm)."), ["dz"] = Prop("number", "Δz (mm).")
            }, "element_ids"),
        Tool("copy_elements", "Copia elementos por un vector (mm). Devuelve los ids nuevos.",
            new Dictionary<string, object?>
            {
                ["element_ids"] = new Dictionary<string, object?> { ["type"] = "array", ["items"] = new Dictionary<string, object?> { ["type"] = "number" }, ["description"] = "ElementIds a copiar." },
                ["dx"] = Prop("number", "Δx (mm)."), ["dy"] = Prop("number", "Δy (mm)."), ["dz"] = Prop("number", "Δz (mm).")
            }, "element_ids"),
        Tool("rotate_element", "Rota un elemento alrededor de un eje vertical (Z) que pasa por un punto (mm).",
            new Dictionary<string, object?>
            {
                ["element_id"] = Prop("number", "ElementId."),
                ["angle"] = Prop("number", "Ángulo en grados (positivo = antihorario)."),
                ["x"] = Prop("number", "X del centro de rotación en mm (opcional, default = origen)."),
                ["y"] = Prop("number", "Y del centro de rotación en mm (opcional).")
            }, "element_id", "angle"),
        Tool("set_type_parameter", "Escribe un parámetro del TIPO de un elemento (afecta a todas las instancias del tipo).",
            new Dictionary<string, object?>
            {
                ["element_id"] = Prop("number", "ElementId de una instancia (o del tipo)."),
                ["parameter_name"] = Prop("string", "Nombre del parámetro de tipo."),
                ["value"] = Prop("string", "Valor nuevo como texto.")
            }, "element_id", "parameter_name", "value")
    };

    public static object? Call(string name, Dictionary<string, object?> args, UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        var doc = uidoc?.Document;
        if (doc == null) throw new InvalidOperationException("No document is open in Revit.");

        switch (name)
        {
            case "get_model_info": return GetModelInfo(uidoc!, doc);
            case "list_elements": return ListElements(doc, Str(args, "category"), (int)Num(args, "limit", 50));
            case "get_element": return GetElement(doc, (long)Num(args, "element_id"));
            case "set_parameter": return SetParameter(doc, (long)Num(args, "element_id"), Str(args, "parameter_name"), Str(args, "value"));
            case "list_views": return ListViews(doc);
            case "get_selection": return GetSelection(uidoc!, doc);
            case "get_geometry": return GetGeometry(uidoc!, doc, args);
            case "select_elements": return SelectElements(uidoc!, args);
            case "rename_element": return RenameElement(doc, (long)Num(args, "element_id"), Str(args, "name"));
            case "delete_elements": return DeleteElements(uidoc!, doc, args);
            case "list_types": return ListTypes(doc, Str(args, "category"), (int)Num(args, "limit", 100));
            case "create_pipe": return CreatePipe(doc, args);
            case "place_family_instance": return PlaceFamilyInstance(doc, args);
            case "place_hosted_family": return PlaceHostedFamily(doc, args);
            case "create_duct": return CreateDuct(doc, args);
            case "create_conduit": return CreateConduit(doc, args);
            case "create_cable_tray": return CreateCableTray(doc, args);
            case "create_wall": return CreateWall(doc, args);
            case "create_floor": return CreateFloor(doc, args);
            case "create_grid": return CreateGrid(doc, args);
            case "create_level": return CreateLevel(doc, args);
            case "create_room": return CreateRoom(doc, args);
            case "create_view": return CreateView(doc, args);
            case "duplicate_view": return DuplicateView(doc, args);
            case "create_sheet": return CreateSheet(doc, args);
            case "place_view_on_sheet": return PlaceViewOnSheet(doc, args);
            case "create_text_note": return CreateTextNote(doc, args);
            case "move_elements": return MoveElements(doc, args);
            case "copy_elements": return CopyElements(doc, args);
            case "rotate_element": return RotateElement(doc, args);
            case "set_type_parameter": return SetTypeParameter(doc, args);
            default: throw new InvalidOperationException($"Unknown tool '{name}'.");
        }
    }

    private static string Str(Dictionary<string, object?> args, string key)
        => args.TryGetValue(key, out var v) && v != null ? v.ToString()! : "";

    private static double Num(Dictionary<string, object?> args, string key, double fallback = 0)
    {
        if (!args.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is double d) return d;
        return double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : fallback;
    }

    private static object GetModelInfo(UIDocument uidoc, Document doc)
    {
        int sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();
        int views = new FilteredElementCollector(doc).OfClass(typeof(View))
            .Cast<View>().Count(v => !v.IsTemplate);
        return new Dictionary<string, object?>
        {
            ["title"] = doc.Title,
            ["path"] = doc.PathName,
            ["isWorkshared"] = doc.IsWorkshared,
            ["activeView"] = uidoc.ActiveView?.Name,
            ["sheetCount"] = sheets,
            ["viewCount"] = views
        };
    }

    private static BuiltInCategory? ResolveCategory(Document doc, string name)
    {
        string candidate = name.StartsWith("OST_", StringComparison.OrdinalIgnoreCase) ? name : "OST_" + name.Replace(" ", "");
        if (Enum.TryParse(candidate, true, out BuiltInCategory bic)) return bic;

        foreach (Category cat in doc.Settings.Categories)
        {
            if (string.Equals(cat.Name, name, StringComparison.OrdinalIgnoreCase))
                return cat.BuiltInCategory;
        }
        return null;
    }

    private static object ListElements(Document doc, string category, int limit)
    {
        var bic = ResolveCategory(doc, category)
            ?? throw new InvalidOperationException($"Category '{category}' not found. Use English Revit names (e.g. 'Walls') or BuiltInCategory (e.g. 'OST_Walls').");
        if (limit <= 0) limit = 50;

        var result = new List<object?>();
        foreach (var e in new FilteredElementCollector(doc)
                     .OfCategory(bic).WhereElementIsNotElementType())
        {
            string typeName = "";
            try { typeName = (doc.GetElement(e.GetTypeId()) as ElementType)?.Name ?? ""; } catch { }
            string levelName = "";
            try { levelName = (doc.GetElement(e.LevelId) as Level)?.Name ?? ""; } catch { }
            result.Add(new Dictionary<string, object?>
            {
                ["id"] = IdOf(e),
                ["name"] = e.Name,
                ["type"] = typeName,
                ["level"] = levelName
            });
            if (result.Count >= limit) break;
        }
        return new Dictionary<string, object?> { ["category"] = category, ["count"] = result.Count, ["elements"] = result };
    }

    private static object GetElement(Document doc, long id)
    {
        var e = doc.GetElement(MakeId(id))
            ?? throw new InvalidOperationException($"Element {id} not found.");

        var parameters = new List<object?>();
        foreach (Parameter p in e.Parameters)
        {
            string value;
            try { value = p.AsValueString() ?? p.AsString() ?? ""; }
            catch { value = ""; }
            parameters.Add(new Dictionary<string, object?>
            {
                ["name"] = p.Definition?.Name ?? "",
                ["value"] = value,
                ["isReadOnly"] = p.IsReadOnly
            });
        }

        return new Dictionary<string, object?>
        {
            ["id"] = IdOf(e),
            ["name"] = e.Name,
            ["category"] = e.Category?.Name,
            ["type"] = (doc.GetElement(e.GetTypeId()) as ElementType)?.Name,
            ["parameters"] = parameters.OrderBy(p => ((Dictionary<string, object?>)p!)["name"]).ToList()
        };
    }

    private static object SetParameter(Document doc, long id, string paramName, string value)
    {
        var e = doc.GetElement(MakeId(id))
            ?? throw new InvalidOperationException($"Element {id} not found.");
        var p = e.LookupParameter(paramName)
            ?? throw new InvalidOperationException($"Parameter '{paramName}' not found on element {id}.");
        if (p.IsReadOnly) throw new InvalidOperationException($"Parameter '{paramName}' is read-only.");

        using var tx = new Transaction(doc, "AllO MCP: Set Parameter");
        tx.Start();
        bool ok;
        switch (p.StorageType)
        {
            case StorageType.String:
                ok = p.Set(value);
                break;
            case StorageType.Integer:
                ok = int.TryParse(value, out int i) && p.Set(i);
                break;
            case StorageType.Double:
                ok = p.SetValueString(value) ||
                     (double.TryParse(value, System.Globalization.NumberStyles.Any,
                         System.Globalization.CultureInfo.InvariantCulture, out double d) && p.Set(d));
                break;
            default:
                ok = p.SetValueString(value);
                break;
        }
        if (!ok) { tx.RollBack(); throw new InvalidOperationException("Revit rejected the value (wrong format or units?)."); }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["element"] = e.Name, ["parameter"] = paramName, ["value"] = value };
    }

    private static object ListViews(Document doc)
    {
        var result = new List<object?>();
        foreach (var v in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
        {
            if (v.IsTemplate) continue;
            result.Add(new Dictionary<string, object?>
            {
                ["id"] = IdOf(v),
                ["name"] = v.Name,
                ["viewType"] = v.ViewType.ToString()
            });
        }
        return new Dictionary<string, object?> { ["count"] = result.Count, ["views"] = result };
    }

    private static object GetSelection(UIDocument uidoc, Document doc)
    {
        var result = new List<object?>();
        foreach (var eid in uidoc.Selection.GetElementIds())
        {
            var e = doc.GetElement(eid);
            if (e == null) continue;
            result.Add(new Dictionary<string, object?>
            {
                ["id"] = IdOf(e),
                ["name"] = e.Name,
                ["category"] = e.Category?.Name
            });
        }
        return new Dictionary<string, object?> { ["count"] = result.Count, ["elements"] = result };
    }

    private static object RenameElement(Document doc, long id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name must not be empty.");
        var e = doc.GetElement(MakeId(id))
            ?? throw new InvalidOperationException($"Element {id} not found.");

        string oldName = e.Name;
        using var tx = new Transaction(doc, "AllO MCP: Rename");
        tx.Start();
        e.Name = name;
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["oldName"] = oldName, ["newName"] = name };
    }

    private static object DeleteElements(UIDocument uidoc, Document doc, Dictionary<string, object?> args)
    {
        if (args.TryGetValue("element_ids", out var raw) && raw is List<object?> list)
        {
            var activeId = uidoc.ActiveView?.Id;
            var skipped = new List<object?>();
            var toDelete = new List<ElementId>();
            foreach (var item in list)
            {
                if (item == null || !double.TryParse(item.ToString(), out double d)) continue;
                var eid = MakeId((long)d);
                if (activeId != null && eid == activeId) { skipped.Add((long)d); continue; }
                if (doc.GetElement(eid) == null) { skipped.Add((long)d); continue; }
                toDelete.Add(eid);
            }

            int deleted = 0;
            if (toDelete.Count > 0)
            {
                using var tx = new Transaction(doc, "AllO MCP: Delete Elements");
                tx.Start();
                foreach (var eid in toDelete)
                {
                    try { deleted += doc.Delete(eid).Count; }
                    catch (Exception ex) { skipped.Add($"{eid}: {ex.Message}"); }
                }
                tx.Commit();
            }
            return new Dictionary<string, object?>
            {
                ["ok"] = true,
                ["deleted"] = deleted,
                ["requested"] = toDelete.Count,
                ["skipped"] = skipped
            };
        }
        throw new InvalidOperationException("element_ids must be an array of numbers.");
    }

    private static double Mm(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    private static Level ResolveLevel(Document doc, string name)
    {
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
        if (levels.Count == 0) throw new InvalidOperationException("The model has no levels.");
        if (!string.IsNullOrWhiteSpace(name))
        {
            var m = levels.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
            if (m != null) return m;
            throw new InvalidOperationException($"Level '{name}' not found.");
        }
        return levels.OrderBy(l => l.Elevation).First();
    }

    private static object ListTypes(Document doc, string category, int limit)
    {
        var bic = ResolveCategory(doc, category)
            ?? throw new InvalidOperationException($"Category '{category}' not found.");
        if (limit <= 0) limit = 100;

        var result = new List<object?>();
        foreach (var et in new FilteredElementCollector(doc)
                     .OfCategory(bic).WhereElementIsElementType().Cast<ElementType>())
        {
            result.Add(new Dictionary<string, object?>
            {
                ["id"] = IdOf(et),
                ["family"] = et.FamilyName,
                ["type"] = et.Name
            });
            if (result.Count >= limit) break;
        }
        return new Dictionary<string, object?> { ["category"] = category, ["count"] = result.Count, ["types"] = result };
    }

    private static object CreatePipe(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));

        var pipeTypes = new FilteredElementCollector(doc).OfClass(typeof(PipeType)).Cast<PipeType>().ToList();
        if (pipeTypes.Count == 0) throw new InvalidOperationException("The model has no Pipe Types loaded.");
        string ptName = Str(args, "pipe_type");
        var pipeType = string.IsNullOrWhiteSpace(ptName)
            ? pipeTypes[0]
            : pipeTypes.FirstOrDefault(t => string.Equals(t.Name, ptName, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException($"Pipe Type '{ptName}' not found.");

        var sysTypes = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>().ToList();
        if (sysTypes.Count == 0) throw new InvalidOperationException("The model has no Piping System Types.");
        string stName = Str(args, "system_type");
        var sysType = string.IsNullOrWhiteSpace(stName)
            ? sysTypes[0]
            : sysTypes.FirstOrDefault(t => string.Equals(t.Name, stName, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException($"Piping System Type '{stName}' not found.");

        var p1 = new XYZ(Mm(Num(args, "x1")), Mm(Num(args, "y1")), Mm(Num(args, "z1")));
        var p2 = new XYZ(Mm(Num(args, "x2")), Mm(Num(args, "y2")), Mm(Num(args, "z2")));
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");

        double diameter = Num(args, "diameter");

        using var tx = new Transaction(doc, "AllO MCP: Create Pipe");
        tx.Start();
        var pipe = Pipe.Create(doc, sysType.Id, pipeType.Id, level.Id, p1, p2);
        if (diameter > 0)
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(Mm(diameter));
        tx.Commit();

        return new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["id"] = IdOf(pipe),
            ["pipeType"] = pipeType.Name,
            ["systemType"] = sysType.Name,
            ["level"] = level.Name
        };
    }

    private static object PlaceFamilyInstance(Document doc, Dictionary<string, object?> args)
    {
        string typeName = Str(args, "type_name");
        if (string.IsNullOrWhiteSpace(typeName)) throw new InvalidOperationException("type_name is required.");
        string familyName = Str(args, "family_name");

        var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
            .Where(s => string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(familyName) || string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (symbols.Count == 0)
            throw new InvalidOperationException($"Family type '{typeName}' not found. Use list_types to discover names.");
        if (symbols.Count > 1)
            throw new InvalidOperationException($"'{typeName}' is ambiguous ({symbols.Count} matches). Pass family_name to disambiguate.");

        var symbol = symbols[0];
        var level = ResolveLevel(doc, Str(args, "level"));
        var p = new XYZ(Mm(Num(args, "x")), Mm(Num(args, "y")), Mm(Num(args, "z")));

        using var tx = new Transaction(doc, "AllO MCP: Place Family Instance");
        tx.Start();
        if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }
        var inst = doc.Create.NewFamilyInstance(p, symbol, level, StructuralType.NonStructural);
        tx.Commit();

        return new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["id"] = IdOf(inst),
            ["family"] = symbol.FamilyName,
            ["type"] = symbol.Name,
            ["level"] = level.Name
        };
    }

    private static XYZ Pt(Dictionary<string, object?> args, string sx, string sy, string sz)
        => new(Mm(Num(args, sx)), Mm(Num(args, sy)), Mm(Num(args, sz)));

    private static double ToMm(double internalFt) => UnitUtils.ConvertFromInternalUnits(internalFt, UnitTypeId.Millimeters);

    private static Dictionary<string, object?> PointMm(XYZ p)
        => new() { ["x"] = Math.Round(ToMm(p.X), 1), ["y"] = Math.Round(ToMm(p.Y), 1), ["z"] = Math.Round(ToMm(p.Z), 1) };

    private static object GetGeometry(UIDocument uidoc, Document doc, Dictionary<string, object?> args)
    {
        List<ElementId> ids;
        if (args.TryGetValue("element_ids", out var raw) && raw is List<object?> list && list.Count > 0)
        {
            ids = new List<ElementId>();
            foreach (var item in list)
                if (item != null && double.TryParse(item.ToString(), out double d)) ids.Add(MakeId((long)d));
        }
        else ids = uidoc.Selection.GetElementIds().ToList();

        var result = new List<object?>();
        foreach (var eid in ids)
        {
            var e = doc.GetElement(eid);
            if (e == null) continue;

            Curve? curve = (e as CurveElement)?.GeometryCurve;
            if (curve == null && e.Location is LocationCurve lc) curve = lc.Curve;

            var item = new Dictionary<string, object?> { ["id"] = IdOf(e), ["category"] = e.Category?.Name };
            if (curve != null && curve.IsBound)
            {
                var a = curve.GetEndPoint(0);
                var b = curve.GetEndPoint(1);
                item["kind"] = curve is Line ? "line" : "curve";
                item["start"] = PointMm(a);
                item["end"] = PointMm(b);
                item["length"] = Math.Round(ToMm(curve.Length), 1);
            }
            else if (e.Location is LocationPoint lp)
            {
                item["kind"] = "point";
                item["point"] = PointMm(lp.Point);
            }
            else
            {
                var bb = e.get_BoundingBox(null);
                if (bb != null)
                {
                    item["kind"] = "bbox";
                    item["min"] = PointMm(bb.Min);
                    item["max"] = PointMm(bb.Max);
                }
                else item["kind"] = "none";
            }
            result.Add(item);
        }
        return new Dictionary<string, object?> { ["count"] = result.Count, ["elements"] = result };
    }

    private static bool Flag(Dictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v == null) return false;
        if (v is bool b) return b;
        return bool.TryParse(v.ToString(), out var p) && p;
    }

    private static T ResolveType<T>(Document doc, string name, string label) where T : Element
    {
        var all = new FilteredElementCollector(doc).OfClass(typeof(T)).Cast<T>().ToList();
        if (all.Count == 0) throw new InvalidOperationException($"The model has no {label}.");
        if (string.IsNullOrWhiteSpace(name)) return all[0];
        return all.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"{label} '{name}' not found.");
    }

    private static FamilySymbol ResolveSymbol(Document doc, string typeName, string familyName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) throw new InvalidOperationException("type_name is required.");
        var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
            .Where(s => string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(familyName) || string.Equals(s.FamilyName, familyName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (symbols.Count == 0) throw new InvalidOperationException($"Family type '{typeName}' not found. Use list_types to discover names.");
        if (symbols.Count > 1) throw new InvalidOperationException($"'{typeName}' is ambiguous ({symbols.Count} matches). Pass family_name to disambiguate.");
        return symbols[0];
    }

    private static List<ElementId> IdList(Dictionary<string, object?> args, string key)
    {
        var ids = new List<ElementId>();
        if (args.TryGetValue(key, out var raw) && raw is List<object?> list)
            foreach (var item in list)
                if (item != null && double.TryParse(item.ToString(), out double d)) ids.Add(MakeId((long)d));
        if (ids.Count == 0) throw new InvalidOperationException($"{key} must be a non-empty array of numbers.");
        return ids;
    }

    private static object PlaceHostedFamily(Document doc, Dictionary<string, object?> args)
    {
        var symbol = ResolveSymbol(doc, Str(args, "type_name"), Str(args, "family_name"));
        var host = doc.GetElement(MakeId((long)Num(args, "host_id")))
            ?? throw new InvalidOperationException("Host element not found.");
        var p = Pt(args, "x", "y", "z");

        using var tx = new Transaction(doc, "AllO MCP: Place Hosted Family");
        tx.Start();
        if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }
        var inst = doc.Create.NewFamilyInstance(p, symbol, host, StructuralType.NonStructural);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(inst), ["family"] = symbol.FamilyName, ["type"] = symbol.Name };
    }

    private static object CreateDuct(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));
        var ductType = ResolveType<DuctType>(doc, Str(args, "duct_type"), "Duct Types");
        var sysType = ResolveType<MechanicalSystemType>(doc, Str(args, "system_type"), "Mechanical System Types");
        var p1 = Pt(args, "x1", "y1", "z1");
        var p2 = Pt(args, "x2", "y2", "z2");
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");
        using var tx = new Transaction(doc, "AllO MCP: Create Duct");
        tx.Start();
        var duct = Duct.Create(doc, sysType.Id, ductType.Id, level.Id, p1, p2);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(duct), ["ductType"] = ductType.Name, ["level"] = level.Name };
    }

    private static object CreateConduit(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));
        var type = ResolveType<ConduitType>(doc, Str(args, "conduit_type"), "Conduit Types");
        var p1 = Pt(args, "x1", "y1", "z1");
        var p2 = Pt(args, "x2", "y2", "z2");
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");
        using var tx = new Transaction(doc, "AllO MCP: Create Conduit");
        tx.Start();
        var conduit = Conduit.Create(doc, type.Id, p1, p2, level.Id);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(conduit), ["conduitType"] = type.Name, ["level"] = level.Name };
    }

    private static object CreateCableTray(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));
        var type = ResolveType<CableTrayType>(doc, Str(args, "tray_type"), "Cable Tray Types");
        var p1 = Pt(args, "x1", "y1", "z1");
        var p2 = Pt(args, "x2", "y2", "z2");
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");
        using var tx = new Transaction(doc, "AllO MCP: Create Cable Tray");
        tx.Start();
        var tray = CableTray.Create(doc, type.Id, p1, p2, level.Id);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(tray), ["trayType"] = type.Name, ["level"] = level.Name };
    }

    private static object CreateWall(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));
        var wallType = ResolveType<WallType>(doc, Str(args, "wall_type"), "Wall Types");
        var p1 = new XYZ(Mm(Num(args, "x1")), Mm(Num(args, "y1")), 0);
        var p2 = new XYZ(Mm(Num(args, "x2")), Mm(Num(args, "y2")), 0);
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");
        double height = Mm(Num(args, "height"));
        if (height <= 0) throw new InvalidOperationException("height must be > 0.");
        double baseOffset = Mm(Num(args, "base_offset"));
        var line = Line.CreateBound(p1, p2);
        using var tx = new Transaction(doc, "AllO MCP: Create Wall");
        tx.Start();
        var wall = Wall.Create(doc, line, wallType.Id, level.Id, height, baseOffset, false, false);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(wall), ["wallType"] = wallType.Name, ["level"] = level.Name };
    }

    private static object CreateFloor(Document doc, Dictionary<string, object?> args)
    {
        if (!(args.TryGetValue("points", out var raw) && raw is List<object?> pts) || pts.Count < 3)
            throw new InvalidOperationException("points must be an array of at least 3 {x, y} objects.");
        var loop = new CurveLoop();
        var xyz = new List<XYZ>();
        foreach (var p in pts)
        {
            if (p is not Dictionary<string, object?> d) throw new InvalidOperationException("Each point must be an object {x, y}.");
            xyz.Add(new XYZ(Mm(Num(d, "x")), Mm(Num(d, "y")), 0));
        }
        for (int i = 0; i < xyz.Count; i++)
            loop.Append(Line.CreateBound(xyz[i], xyz[(i + 1) % xyz.Count]));

        var level = ResolveLevel(doc, Str(args, "level"));
        var floorType = ResolveType<FloorType>(doc, Str(args, "floor_type"), "Floor Types");
        using var tx = new Transaction(doc, "AllO MCP: Create Floor");
        tx.Start();
        var floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorType.Id, level.Id);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(floor), ["floorType"] = floorType.Name, ["level"] = level.Name };
    }

    private static object CreateGrid(Document doc, Dictionary<string, object?> args)
    {
        var p1 = new XYZ(Mm(Num(args, "x1")), Mm(Num(args, "y1")), 0);
        var p2 = new XYZ(Mm(Num(args, "x2")), Mm(Num(args, "y2")), 0);
        if (p1.IsAlmostEqualTo(p2)) throw new InvalidOperationException("Start and end points are the same.");
        string name = Str(args, "name");
        using var tx = new Transaction(doc, "AllO MCP: Create Grid");
        tx.Start();
        var grid = Grid.Create(doc, Line.CreateBound(p1, p2));
        if (!string.IsNullOrWhiteSpace(name)) try { grid.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(grid), ["name"] = grid.Name };
    }

    private static object CreateLevel(Document doc, Dictionary<string, object?> args)
    {
        double elev = Mm(Num(args, "elevation"));
        string name = Str(args, "name");
        using var tx = new Transaction(doc, "AllO MCP: Create Level");
        tx.Start();
        var level = Level.Create(doc, elev);
        if (!string.IsNullOrWhiteSpace(name)) try { level.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(level), ["name"] = level.Name };
    }

    private static object CreateRoom(Document doc, Dictionary<string, object?> args)
    {
        var level = ResolveLevel(doc, Str(args, "level"));
        var uv = new UV(Mm(Num(args, "x")), Mm(Num(args, "y")));
        string name = Str(args, "name");
        using var tx = new Transaction(doc, "AllO MCP: Create Room");
        tx.Start();
        var room = doc.Create.NewRoom(level, uv)
            ?? throw new InvalidOperationException("Revit could not create the room at that point.");
        if (!string.IsNullOrWhiteSpace(name)) try { room.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(room), ["level"] = level.Name };
    }

    private static ElementId ViewFamilyTypeId(Document doc, ViewFamily family)
        => new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
               .FirstOrDefault(v => v.ViewFamily == family)?.Id
           ?? throw new InvalidOperationException($"No ViewFamilyType for {family}.");

    private static object CreateView(Document doc, Dictionary<string, object?> args)
    {
        string vt = Str(args, "view_type").ToLowerInvariant().Replace("_", "").Replace(" ", "");
        string name = Str(args, "name");
        View view;
        using var tx = new Transaction(doc, "AllO MCP: Create View");
        tx.Start();
        switch (vt)
        {
            case "floorplan":
                view = ViewPlan.Create(doc, ViewFamilyTypeId(doc, ViewFamily.FloorPlan), ResolveLevel(doc, Str(args, "level")).Id);
                break;
            case "ceilingplan":
                view = ViewPlan.Create(doc, ViewFamilyTypeId(doc, ViewFamily.CeilingPlan), ResolveLevel(doc, Str(args, "level")).Id);
                break;
            case "3d":
            case "threed":
                view = View3D.CreateIsometric(doc, ViewFamilyTypeId(doc, ViewFamily.ThreeDimensional));
                break;
            case "drafting":
                view = ViewDrafting.Create(doc, ViewFamilyTypeId(doc, ViewFamily.Drafting));
                break;
            default:
                tx.RollBack();
                throw new InvalidOperationException("view_type must be floor_plan | ceiling_plan | 3d | drafting.");
        }
        if (!string.IsNullOrWhiteSpace(name)) try { view.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(view), ["name"] = view.Name, ["viewType"] = view.ViewType.ToString() };
    }

    private static object DuplicateView(Document doc, Dictionary<string, object?> args)
    {
        var view = doc.GetElement(MakeId((long)Num(args, "view_id"))) as View
            ?? throw new InvalidOperationException("View not found.");
        var mode = Flag(args, "with_detailing") ? ViewDuplicateOption.WithDetailing : ViewDuplicateOption.Duplicate;
        if (!view.CanViewBeDuplicated(mode)) throw new InvalidOperationException("This view cannot be duplicated.");
        string name = Str(args, "name");
        using var tx = new Transaction(doc, "AllO MCP: Duplicate View");
        tx.Start();
        var newId = view.Duplicate(mode);
        var newView = (View)doc.GetElement(newId);
        if (!string.IsNullOrWhiteSpace(name)) try { newView.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(newView), ["name"] = newView.Name };
    }

    private static object CreateSheet(Document doc, Dictionary<string, object?> args)
    {
        var tb = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsElementType().Cast<ElementType>().ToList();
        string tbName = Str(args, "title_block");
        ElementId tbId = ElementId.InvalidElementId;
        if (tb.Count > 0)
            tbId = (string.IsNullOrWhiteSpace(tbName)
                ? tb[0]
                : tb.FirstOrDefault(t => string.Equals(t.Name, tbName, StringComparison.OrdinalIgnoreCase))
                  ?? throw new InvalidOperationException($"Title block '{tbName}' not found.")).Id;

        using var tx = new Transaction(doc, "AllO MCP: Create Sheet");
        tx.Start();
        var sheet = ViewSheet.Create(doc, tbId);
        string number = Str(args, "number");
        string name = Str(args, "name");
        if (!string.IsNullOrWhiteSpace(number)) try { sheet.SheetNumber = number; } catch { }
        if (!string.IsNullOrWhiteSpace(name)) try { sheet.Name = name; } catch { }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(sheet), ["number"] = sheet.SheetNumber, ["name"] = sheet.Name };
    }

    private static object PlaceViewOnSheet(Document doc, Dictionary<string, object?> args)
    {
        var sheet = doc.GetElement(MakeId((long)Num(args, "sheet_id"))) as ViewSheet
            ?? throw new InvalidOperationException("Sheet not found.");
        long viewId = (long)Num(args, "view_id");
        var viewElementId = MakeId(viewId);
        if (!Viewport.CanAddViewToSheet(doc, sheet.Id, viewElementId))
            throw new InvalidOperationException("This view cannot be added to the sheet (already placed, or a plan already on another sheet).");
        var center = new XYZ(Mm(Num(args, "x")), Mm(Num(args, "y")), 0);
        using var tx = new Transaction(doc, "AllO MCP: Place View On Sheet");
        tx.Start();
        var vp = Viewport.Create(doc, sheet.Id, viewElementId, center);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(vp), ["sheet"] = sheet.SheetNumber };
    }

    private static object CreateTextNote(Document doc, Dictionary<string, object?> args)
    {
        var viewId = MakeId((long)Num(args, "view_id"));
        if (doc.GetElement(viewId) is not View) throw new InvalidOperationException("View not found.");
        string text = Str(args, "text");
        if (string.IsNullOrEmpty(text)) throw new InvalidOperationException("text must not be empty.");
        var typeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
        if (typeId == ElementId.InvalidElementId)
            typeId = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).FirstElementId();
        var p = new XYZ(Mm(Num(args, "x")), Mm(Num(args, "y")), 0);
        using var tx = new Transaction(doc, "AllO MCP: Create Text Note");
        tx.Start();
        var note = TextNote.Create(doc, viewId, p, text, typeId);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = IdOf(note) };
    }

    private static object MoveElements(Document doc, Dictionary<string, object?> args)
    {
        var ids = IdList(args, "element_ids");
        var v = Pt(args, "dx", "dy", "dz");
        using var tx = new Transaction(doc, "AllO MCP: Move Elements");
        tx.Start();
        ElementTransformUtils.MoveElements(doc, ids, v);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["moved"] = ids.Count };
    }

    private static object CopyElements(Document doc, Dictionary<string, object?> args)
    {
        var ids = IdList(args, "element_ids");
        var v = Pt(args, "dx", "dy", "dz");
        using var tx = new Transaction(doc, "AllO MCP: Copy Elements");
        tx.Start();
        var copies = ElementTransformUtils.CopyElements(doc, ids, v);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["count"] = copies.Count, ["ids"] = copies.Select(c => (object?)IdOf(doc.GetElement(c))).ToList() };
    }

    private static object RotateElement(Document doc, Dictionary<string, object?> args)
    {
        var id = MakeId((long)Num(args, "element_id"));
        if (doc.GetElement(id) == null) throw new InvalidOperationException("Element not found.");
        double angle = Num(args, "angle") * Math.PI / 180.0;
        var basePt = new XYZ(Mm(Num(args, "x")), Mm(Num(args, "y")), 0);
        var axis = Line.CreateBound(basePt, basePt + XYZ.BasisZ);
        using var tx = new Transaction(doc, "AllO MCP: Rotate Element");
        tx.Start();
        ElementTransformUtils.RotateElement(doc, id, axis, angle);
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["id"] = (long)Num(args, "element_id") };
    }

    private static object SetTypeParameter(Document doc, Dictionary<string, object?> args)
    {
        var e = doc.GetElement(MakeId((long)Num(args, "element_id")))
            ?? throw new InvalidOperationException("Element not found.");
        var typeId = e.GetTypeId();
        var type = (typeId != ElementId.InvalidElementId ? doc.GetElement(typeId) : e)
            ?? throw new InvalidOperationException("Element has no type.");
        string paramName = Str(args, "parameter_name");
        string value = Str(args, "value");
        var p = type.LookupParameter(paramName)
            ?? throw new InvalidOperationException($"Type parameter '{paramName}' not found on {type.Name}.");
        if (p.IsReadOnly) throw new InvalidOperationException($"Parameter '{paramName}' is read-only.");

        using var tx = new Transaction(doc, "AllO MCP: Set Type Parameter");
        tx.Start();
        bool ok = p.StorageType switch
        {
            StorageType.String => p.Set(value),
            StorageType.Integer => int.TryParse(value, out int i) && p.Set(i),
            StorageType.Double => p.SetValueString(value) || (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d) && p.Set(d)),
            _ => p.SetValueString(value)
        };
        if (!ok) { tx.RollBack(); throw new InvalidOperationException("Revit rejected the value (wrong format or units?)."); }
        tx.Commit();
        return new Dictionary<string, object?> { ["ok"] = true, ["type"] = type.Name, ["parameter"] = paramName, ["value"] = value };
    }

    private static object SelectElements(UIDocument uidoc, Dictionary<string, object?> args)
    {
        if (args.TryGetValue("element_ids", out var raw) && raw is List<object?> list)
        {
            var ids = new List<ElementId>();
            foreach (var item in list)
            {
                if (item == null) continue;
                if (double.TryParse(item.ToString(), out double d))
                    ids.Add(MakeId((long)d));
            }
            uidoc.Selection.SetElementIds(ids);
            return new Dictionary<string, object?> { ["ok"] = true, ["selected"] = ids.Count };
        }
        throw new InvalidOperationException("element_ids must be an array of numbers.");
    }
}

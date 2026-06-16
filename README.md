# AllO — Add-in para Autodesk Revit

Add-in C#/WPF (MVVM) para Revit **2023 / 2024 / 2025** con ~23 comandos de productividad
BIM/MEP organizados en 8 paneles, más integración con Claude vía MCP.

## Estructura

```
src/
├── Shared/        Biblioteca multi-target (net48 + net8.0-windows): Core, Models,
│                  Services, UI (WPF), Commands. Todo el código común vive aquí.
├── Versioned/     App.cs + RevitService.cs únicos, compilados por las 3 versiones
│                  con #if REVIT2023 / REVIT2024 / (else = 2025).
├── Revit2023/     Proyecto net48  → compila contra SDK 2023
├── Revit2024/     Proyecto net48  → compila contra SDK 2024
├── Revit2025/     Proyecto net8.0 → compila contra SDK 2025
└── Tests/         xUnit (net8.0). Solo referencia archivos puros por <Compile Include>
                   (sin dependencia de RevitAPI). `dotnet test src/Tests/AllO.Tests.csproj`
```

## Build y deploy

```powershell
dotnet build AllO.sln -c Release        # compila las 3 versiones y despliega a Addins
$env:CI='true'; dotnet build AllO.sln   # compila SIN desplegar (lo que usa GitHub Actions)
```

El target `CopyToRevitAddins` copia `AllO.dll`, `AllO.Shared.dll`, iconos y `.addin` a
`%APPDATA%\Autodesk\Revit\Addins\{versión}`.

> **Gotcha:** si un Revit está abierto, la copia de ESA versión falla con warning
> MSB3026 silencioso (la DLL está bloqueada) y el ribbon queda viejo. Cerrar Revit
> y recompilar.

Iconos del ribbon: declarativos en `src/Shared/Resources/Icons/generate_icons.py`
(requiere PIL). Añadir función `draw_x` + entrada en `ICONS` y ejecutar el script.

## Paneles y comandos

| Panel | Comandos |
|---|---|
| Documentation | Sheet Manager (Sheet List / View List / Revisions), Publish, **Table Gen** |
| Views | Copy Crop, Grids, Levels, Auto Section Box |
| Productivity | **Color Coder**, Match Elevation, Param Push |
| MEP | Bloom, Reroute Around, Connector, Multi Connect, Split Pipe, Elbow Dir, Network Tree, Power Each |
| Tools | One Filter, ReOrdering, Family Export, View Manager, **A.I. Connector** |
| Cleanup | Wipe |
| Coordination | Sync Views, Copy/Paste State, Match Props |
| Links | Link Family, Link Visibility |

## Features destacadas

### Table Gen (Excel → Revit)

Importa rangos de Excel como **Drafting View / Legend** (líneas + TextNotes con wrapping
al ancho de celda) o como **Schedule** con formato. El lector usa COM interop
(`ExcelReader`, requiere Excel instalado) y lee `Range.Text` (el valor *formateado*:
fechas, monedas) con fallback a `Value2` si la celda muestra `####`. También captura
negrita, tamaño de fuente, color de texto y relleno (colores Excel vienen en BGR →
`BgrToRgb`). Guardas anti-cuelgue: tope 1000×60 celdas + recorte al contenido real con
una lectura bulk de `Value2` (el UsedRange suele venir inflado por formato residual).

**Modo Schedule — técnica "header como lienzo" (la misma que DiRoots):** el body de un
schedule prohíbe `SetCellText` y celdas combinadas, pero la sección **Header**
(`GetTableData().GetSectionData(SectionType.Header)`) permite TODO:

- `InsertRow`/`InsertColumn` + `SetColumnWidth`/`SetRowHeight` (anchos/altos exactos
  del Excel, puntos→pies = /864)
- `MergeCells(TableMergedCell)` → celdas combinadas reales
- `SetCellText` → texto libre en cualquier celda
- `SetCellStyle(TableCellStyle)` → alineación, negrita, tamaño, color de texto,
  relleno y bordes por lado (line style de `OST_Lines`)

Se crea un `ViewSchedule.CreateSchedule` de Generic Models **sin campos**
(`ShowHeaders = false`) y el Excel completo se pinta en su header. El reload hace
reset del header a 1×1 (limpia merges/estilos) y repinta.

> Regla aprendida a golpes: NUNCA hacer `while` sobre `NumberOfRows`/`NumberOfColumns`
> al insertar — no se actualizan hasta el `Regenerate` y el while congela Revit para
> siempre. Siempre conteos fijos con `for`.

Los key schedules creados por versiones anteriores siguen recargándose por su pipeline
legacy (parámetros "AllO Col N" + key elements, detectado con `Definition.IsKeySchedule`).
La metadata (ruta|hoja|rango) se guarda en `VIEW_DESCRIPTION` con prefijo `EXCEL_DATA|`
para el reload posterior.

### Color Coder

Asigna un color por documento abierto. Modos (1er clic activa, 2º abre el panel):

- **View tabs** (default): pinta las pestañas nativas de Revit, estilo NonicaTab/pyRevit.
  Implementado en `DocumentTabColorizer` recorriendo el visual tree del MainWindow
  (busca `DocumentPaneTabPanel` → TabItems) con reflection — sin referenciar Xceed,
  tolera el drift de AvalonDock entre versiones. Pestaña activa a color pleno,
  inactivas al 45%; restaura los brushes originales al desactivar.
- **Top glow / Border / Bottom glow** (legacy): overlays WPF sobre el rectángulo de la vista.

### Iconos animados del ribbon (`RibbonAnimator`)

Al activar el tab AllO, los iconos hacen una ráfaga de ~3 s (50 ms/frame). Cada botón
recibe un estilo en round-robin: **Spin, Bounce, Slide, Fall, Pulse, Wiggle**. La
amplitud sigue una envolvente por ciclo (50% → 100% → 100% → 50% → 25%) para entrada
y salida suaves; Spin pasa a balanceo amortiguado en los ciclos suaves.

Reglas de seguridad (aprendidas a golpes):

- La ráfaga se **aborta** en cuanto se activa cualquier elemento del ribbon
  (`ComponentManager.UIElementActivated`) — animar durante la ejecución de un comando
  causó fatal error.
- Los frames se generan **una vez como PNG físicos** en `%APPDATA%\AllO\AnimFrames` y
  se cargan con `StreamSource`. NUNCA asignar bitmaps en memoria (RenderTargetBitmap /
  UriSource) al ribbon: Revit crashea al serializar el layout en File > New.
- Los SplitButton muestran el icono de su item actual → se animan también los hijos
  (`RibbonListButton.Items`).
- Apagado: `"AnimatedRibbonIcons": false` en settings.

### A.I. Connector (servidor MCP)

Comando en Tools que levanta un **servidor MCP** (Streamable HTTP, JSON-RPC 2.0) en
`http://localhost:48400/mcp` (busca puerto libre 48400-48409). Solo escucha en
localhost y muere con Revit. Permite a Claude leer y modificar el modelo en vivo.

Conexión:

```bash
# Claude Code (una vez):
claude mcp add --transport http revit http://localhost:48400/mcp
# Claude Desktop: Settings → Connectors → Add custom connector → pegar la URL
```

Tools expuestas (`McpTools`):

| Tool | Qué hace |
|---|---|
| `get_model_info` | Título, ruta, vista activa, conteo de láminas/vistas |
| `list_elements` | Instancias por categoría (nombre inglés o `OST_*`), con tipo y nivel |
| `get_element` | Todos los parámetros de un elemento con valores |
| `set_parameter` | Escribe un parámetro (transacción; convierte según StorageType) |
| `rename_element` | Renombra vía `Element.Name` (vistas, láminas, tipos…) |
| `delete_elements` | Borra en lote (omite la vista activa por seguridad) |
| `list_views` | Vistas del modelo sin plantillas |
| `get_selection` / `select_elements` | Lee o establece la selección en la UI |

Arquitectura (`src/Shared/Services/Mcp/`):

- `McpServerHost` — HttpListener + dispatch JSON-RPC (initialize / tools/list / tools/call).
- `McpRevitExecutor` — cola + `ExternalEvent`: las tools llegan en threads HTTP pero la
  Revit API exige contexto API. **Se crea en el comando** (ExternalEvent.Create lo exige).
- `McpTools` — implementación; divergencia de ElementId con `#if NET48` (IntegerValue,
  corre en 2023/24) / `.Value` (net8, 2025).
- `JsonLite` — JSON parser/writer propio (~250 líneas, con tests). ¿Por qué no otra cosa?
  `System.Web.Extensions` rompe el compilador XAML de net48 bajo dotnet SDK y Newtonsoft
  obligaría a desplegar otra DLL.
- `AiConnectorWindow` — **modeless** (`ShowSingleton`): un ShowDialog modal bloquea el
  idle de Revit y los ExternalEvents jamás se procesan (timeout en toda tool).

Escrituras (`set_parameter`, `rename_element`, `delete_elements`) corren en
transacciones propias → **Ctrl+Z en Revit las deshace**.

## Settings (`%APPDATA%\AllO\settings.json`)

| Clave | Default | Efecto |
|---|---|---|
| `AnimatedRibbonIcons` | `true` | Ráfaga de animación al activar el tab |
| `ShowToasts` | `true` | Notificaciones toast de éxito |
| `ThemeMode` | `"Auto"` | Auto / Dark / Light |
| `Logging.EnableDebug` | — | Log de debug a archivo |

## Notas de desarrollo

- CI: GitHub Actions compila el sln con `-warnaserror` + tests (`.github/workflows/build.yml`).
  El deploy a Addins se salta con `CI=true`.
- Convención de versión: `src/Directory.Build.props` (la muestran las ventanas `AllOWindow`).
- Riesgo conocido: `Shared` en net48 compila contra el SDK 2024 pero corre también en
  Revit 2023 → usar solo APIs presentes en 2023 (p.ej. `ElementId.IntegerValue` con
  `#pragma warning disable CS0618`, nunca `.Value` fuera de `#if !NET48`).
- Toda ventana usa el estilo `AllOWindow` (`UI/Styles/AllOTheme.xaml`): chrome propio,
  fade-in, botones con hover animado. WPF no permite `Setter TargetName` sobre
  `ScaleTransform` — animar press con Storyboard.

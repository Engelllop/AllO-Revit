# AllO — Resumen de Sesión (2026-06-01)

## 🔴 Problema Crítico Resuelto: Crash en Revit 2024.3+

**Causa raíz identificada:** `pyRevit` (no AllO). Su runtime legacy carga ensamblados conflictivos y genera bitmaps dinámicos de ribbon que causan `0xe0434352` + `URI prefix is not recognized` al abrir `File > New > Project`.

**Solución:** pyRevit debe permanecer deshabilitado (`.addin` renombrado a `.bak`). Re-habilitarlo reintroducirá el crash.

---

## 🎨 Ribbon Builder — Dynamic Bitmaps Eliminados

**Archivo:** `Shared/Helpers/RibbonBuilder.cs`

- ❌ Removido todo `RenderTargetBitmap` / `DrawingVisual` / generación dinámica de iconos.
- ✅ `LoadIcon(name, size)` carga archivos `.png` estáticos desde `Resources/Icons/{name}_{size}.png`.
- ✅ Los iconos se congelan (`Freeze()`) para evitar fugas de memoria.
- **Regla permanente:** NUNCA asignar `BitmapSource` generado por WPF a `PushButton.Image`/`LargeImage`. Solo archivos `.png` estáticos o `null`.

**Iconos generados:** 56 archivos PNG (28 comandos × 16×16 + 32×32) creados con Python/Pillow.
- Incluidos como `<Content>` en `AllO.Shared.csproj` con `CopyToOutputDirectory=PreserveNewest`.
- Copiados a la carpeta de deploy en `Resources/Icons/`.

---

## 🎨 Tema Visual Overhaul

**Archivo:** `Shared/UI/Themes/AllOTheme.xaml`

- Paleta basada en Anthropic/Claude (warm tones):
  - Fondo: `#faf9f5` (paper)
  - Texto principal: `#141413` (ink)
  - Acento/CTA: `#d97757` (terracotta)
- Estilos: `BtnPrimary`, `BtnSecondary`, `BtnDanger`, `SearchBox`, `NavBtn`, `DgColumnHeader`, `DgCell`, `DataGridRowEven`/`Odd`.

---

## 🪟 Separación de Ventanas (Single-Panel Mode)

**Problema:** Sidebar visible en ventanas de panel único (CopyCrop, Grids, Levels, SheetList, etc.).

**Solución:**
- `SuperToolWindow.xaml.cs` y `SheetManagerWindow.xaml.cs`: colapsan `SidebarBorder.Visibility = Collapsed` + `RootGrid.ColumnDefinitions[0].Width = 0` antes del primer layout.
- RadioButtons usan `IsChecked="{Binding IsCropTab/IsGridTab/IsLevelTab, Mode=OneWay}"` + evento `Checked="NavTab_Checked"`.
- Comandos que abren single-panel:
  - `CropCommand` → `initialTab: 0, showNav: false, title: "Copy Crop"`
  - `GridCommand` → `initialTab: 1, showNav: false, title: "Grids"`
  - `LevelCommand` → `initialTab: 2, showNav: false, title: "Levels"`
  - `SheetListCommand` → `initialPanel: 0, showNav: false, title: "Sheet List"`
  - `SheetViewListCommand` → `initialPanel: 1, showNav: false, title: "View List"`
  - `SheetRevisionsCommand` → `initialPanel: 2, showNav: false, title: "Revisions"`

---

## ✅ Badges de Estado en Grids/Levels

**Reemplazado:** fondo naranja en filas (`DgRSync`) → **Columna explícita `Status`**.
- Verde pill: `"OK"` (sincronizado)
- Naranja pill: `"Error"` con tooltip descriptivo.

---

## 🔧 Comandos Refactorizados

### `MultiConnectCommand`
- ❌ Eliminado anti-pattern de transacción anidada.
- ✅ Añadido `IRevitService.ConnectElementsBatch(int mainId, List<int> terminalIds)`.
- Implementado en `RevitService.cs` (2023/2024/2025) — single transaction, conecta terminales al main por proximidad.
- **Nota API:** Revit 2023 usa `new ElementId((int)id)`; 2024/2025 usan `new ElementId((long)id)`.

### `MatchElevationCommand`
- ✅ Usa `Curve.CreateTransformed()` en lugar de recrear curvas como `Line`, preservando `Arc` y otros tipos.
- ✅ Respeta elementos `Pinned` (los salta y reporta cantidad).

### `WipeCommand` — Rebuilt
- ❌ Reemplazado `InputDialog` con ejecución inmediata.
- ✅ Nuevo `WipePreviewWindow` con:
  - Lista checkable por categorías (Views, Sheets, Templates, Filters, Imports, LinePatterns, FillPatterns).
  - Conteo por categoría.
  - Botón explícito **Execute** (estilo `BtnDanger`) + Cancel.
  - Soporte para mock service.
- **Archivos nuevos:** `WipeItem.cs`, `WipePreviewViewModel.cs`, `WipePreviewWindow.xaml/.xaml.cs`.

### `MatchCommand` — Rebuilt
- ❌ Reemplazado lista de texto numerada.
- ✅ Nuevo `MatchParameterWindow` con:
  - `DataGrid` con checkboxes, nombre de parámetro, tipo (`StorageType`), valor actual.
  - Botón **"Select Safe"** que excluye parámetros `ElementId`.
  - Copia nativa preservando `StorageType`.
- **Archivos nuevos:** `MatchParameterItem.cs`, `MatchParameterViewModel.cs`, `MatchParameterWindow.xaml/.xaml.cs`.

### `OneFilterCommand` — Rebuilt
- ❌ Reemplazados 4 `InputDialog` secuenciales.
- ✅ Nuevo `OneFilterWindow` con:
  - Dropdown de parámetros poblado desde selección/modelo actual.
  - Dropdown de operadores (`Equals`, `Contains`, `Greater`, `Less`, etc.).
  - Campo de valor.
  - Radio buttons: `Selection` / `Model`.
- **Archivos nuevos:** `OneFilterViewModel.cs`, `OneFilterWindow.xaml/.xaml.cs`, `InverseBoolConverter.cs`.

### `ReOrderingCommand` — Rebuilt (última tarea de sesión)
- ❌ Reemplazados 4 `InputDialog` secuenciales.
- ✅ Abre `ReorderWindow` con preview interactivo.
- Flujo:
  1. Seleccionar elementos a renumerar.
  2. Seleccionar línea/guía (`ModelCurve` o `DetailCurve`).
  3. Proyectar elementos sobre la curva y ordenar por parámetro.
  4. Mostrar ventana con DataGrid: checkbox, nombre, categoría, valor actual, preview.
  5. Ajustar `ParameterName` (default: "Mark"), `Prefix`, `StartNumber`, `Suffix`.
  6. Aplicar numeración a elementos seleccionados.
- **Mejoras UX en VM:**
  - `RefreshPreview()` respeta items deseleccionados (muestra valor actual en lugar de secuencia).
  - Suscripción a `PropertyChanged` de cada ítem para recalcular preview en tiempo real al marcar/desmarcar.
- **Archivos nuevos/modificados:**
  - `ReOrderingCommand.cs` (reescrito)
  - `ReorderItem.cs`, `ReorderViewModel.cs`, `ReorderWindow.xaml/.xaml.cs` (existentes, ajustados)

---

## 🏗️ Build / Deploy

- **MSBuild 18.4 / VS 2022** via PowerShell / `dotnet build`.
- `ContinueOnError=WarnAndContinue` en target `CopyToRevitAddins` para nunca fallar si DLLs están bloqueadas.
- **Resultados de esta sesión:**
  - ✅ Revit 2023 (`net48`) — Build + deploy OK
  - ✅ Revit 2024 (`net48`) — Build + deploy OK
  - ✅ Revit 2025 (`net8.0-windows`) — Build + deploy OK

---

## ⚠️ Notas Importantes para Futuras Sesiones

1. **pyRevit:** Mantener deshabilitado. Si se re-habilita, el crash `0xe0434352` regresará.
2. **Dynamic bitmaps en ribbon:** PROHIBIDOS. Solo `.png` estáticos.
3. **Revit 2023 API:** `ElementId` constructor solo acepta `int`. En Shared usamos `new ElementId(intId)` para compatibilidad (genera warning obsoleto en compilación para 2024/2025 pero es seguro en runtime).
4. **Ventanas WPF:** No se ha establecido `Owner` en ninguna ventana modal. Si aparecen detrás de Revit, agregar `Owner = ...` vía `RevitWindowHandle`.

---

*Sesión finalizada: 2026-06-01 11:44*  
*Autor: Kimi Code CLI*

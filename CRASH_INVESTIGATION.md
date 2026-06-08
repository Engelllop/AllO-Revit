# AllO Revit Crash Investigation — Documentación Completa

> **Fecha:** 2026-05-31  
> **Estado:** En progreso — pendiente confirmar versión de prueba (2024 vs 2025)

---

## 1. Síntoma del Problema

Revit crashea al hacer **File → New → Project** (crear proyecto nuevo).  
El crash ocurre con código de excepción `0xe0434352` (CLR exception).

### Pista del Journal
```
DBG_WARN: Exception occurred when loading UI layout file: The URI prefix is not recognized.: line 0 of .
```
Ubicación del journal: `%LocalAppData%\Autodesk\Revit\Autodesk Revit 2024\Journals\`

---

## 2. Historial de Pruebas

| # | Cambio realizado | Resultado |
|---|------------------|-----------|
| 1 | Comentar `BuildAuditPanel`, `BuildCoordinationPanel` y `AutoSectionBox` en `AppBootstrap.cs` | ✅ **Funcionó** — usuario confirmó "Si me abrio todo bien" |
| 2 | Activar solo `BuildAuditPanel` (panel "Audit" + botón "Wipe") | ❌ **Crash** |
| 3 | Renombrar panel a "Cleanup" y botón a "ModelWipe" | ❌ **Crash** |
| 4 | Reestructurar: integrar todos los comandos nuevos en 6 paneles existentes (sin paneles nuevos) | ❌ **Crash** |
| 5 | Desactivar `ToolTipImage` en `RibbonBuilder.Configure()` | ❌ **Crash** |
| 6 | Restaurar estructura original exacta de 6 paneles (sin ningún botón nuevo) | ❌ **Crash** |
| 7 | Eliminar `RevitUILayout.xml` y `UIState.dat` | ❌ **Crash** |

### Hallazgo Crítico
Revit 2025 **siguió abierto durante todas las pruebas** (PID 2672). Los add-ins de Revit no se recargan dinámicamente; se cargan una sola vez al inicio del programa.

**Hipótesis principal:** El usuario pudo haber estado probando en **Revit 2025** (que nunca recibió los hotfixes porque el DLL estaba bloqueado por el proceso en ejecución), o Revit 2024 nunca se cerró completamente entre iteraciones.

---

## 3. Causas Descartadas

| Hipótesis | Estado | Evidencia |
|-----------|--------|-----------|
| Cantidad de paneles (>6) | ❌ Descartada | Reestructuramos a exactamente 6 paneles y siguió crasheando |
| Nombres conflictivos ("Audit", "Wipe") | ❌ Descartada | Cambiamos a "Cleanup" y "ModelWipe" y siguió crasheando |
| `ToolTipImage` de 96×96 | ❌ Descartada | Comentamos `ToolTipImage` globalmente y siguió crasheando |
| `RevitUILayout.xml` corrupto | ⚠️ Parcialmente descartada | Eliminamos el archivo y siguió crasheando, pero puede regenerarse corrupto si la versión del add-in en memoria es la vieja |
| Código de comandos nuevos (Wipe, etc.) | ⚠️ Sin confirmar | Los comandos nunca se ejecutaron; el crash ocurre al abrir proyecto, no al clickear botón |

---

## 4. Estado Actual del Código

### `src/Shared/Core/AppBootstrap.cs`
- Estructura original de **6 paneles** restaurada:
  1. Documentation
  2. Views
  3. Productivity
  4. MEP
  5. Tools
  6. Links
- `BuildAuditPanel` y `BuildCoordinationPanel` existen como métodos pero **no se llaman** en `Initialize()`.
- Sin ningún botón nuevo registrado en el ribbon.

### `src/Shared/Helpers/RibbonBuilder.cs`
- `ToolTipImage` está **comentado** en `Configure(PushButton)`:
  ```csharp
  // ToolTipImage disabled — causes Revit crash when serializing ribbon layout on New Project
  // button.ToolTipImage = CreateGlyphIcon(tooltipImageGlyph ?? glyph, 96, background, foreground);
  ```

### Archivos de Layout Revit
- `RevitUILayout.xml` → **eliminado** (backupeado a `.bak`)
- `UIState.dat` → **eliminado**
- Se regenerarán automáticamente al próximo inicio limpio de Revit 2024.

---

## 5. Pendientes / Próximos Pasos

### Paso 1: Confirmar versión de prueba
**Crítico:** Determinar si el usuario estaba probando en Revit 2024 o Revit 2025.
- Si era **2025**: el DLL nunca se actualizó porque Revit 2025 estaba corriendo. La solución es cerrar Revit 2025, recompilar/desplegar a 2025, y probar.
- Si era **2024**: asegurar que Revit 2024 esté completamente cerrado antes de abrirlo de nuevo.

### Paso 2: Probar inicio limpio
Con Revit completamente cerrado:
1. Recompilar y desplegar a la versión objetivo (2024 o 2025).
2. Abrir Revit fresco.
3. Crear **New Project**.
4. Verificar si abre sin crash.

### Paso 3: Si funciona el inicio limpio
Re-integrar los nuevos comandos **uno por uno** para identificar si alguno específico causa problemas:
1. AutoSectionBox en Views
2. SyncViews en Views
3. CopyState + PasteState en Views
4. MatchProps en Productivity
5. Wipe en Tools

### Paso 4: Si sigue crasheando con inicio limpio
Investigar otras causas:
- Verificar que el `.addin` manifest no tenga duplicados o rutas incorrectas.
- Revisar journals recientes para nueva pista del error exacto.
- Considerar que el problema pueda estar en `ColorCoderOverlayHost` o `ThemeManager.Apply()`.

---

## 6. Comandos Nuevos Implementados (sin registrar en ribbon actualmente)

Los siguientes comandos están compilados y listos en `AllO.Shared.dll`:

| Comando | Archivo | Función |
|---------|---------|---------|
| `WipeCommand` | `Commands/WipeCommand.cs` | Limpieza profunda: vistas, sheets, templates, filters, imports, patterns |
| `AutoSectionBoxCommand` | `Commands/AutoSectionBoxCommand.cs` | Crea vista 3D con section box ajustado a selección |
| `SyncViewsCommand` | `Commands/SyncViewsCommand.cs` | Sincroniza pan/zoom entre dos vistas vía evento Idling |
| `CopyStateCommand` | `Commands/CopyStateCommand.cs` | Copia estado de visibilidad (worksets, categorías, filters) |
| `PasteStateCommand` | `Commands/PasteStateCommand.cs` | Pega estado de visibilidad copiado |
| `MatchCommand` | `Commands/MatchCommand.cs` | Match selectivo de parámetros entre elementos |
| `ViewManagerCommand` | `Commands/ViewManagerCommand.cs` | Creación batch de vistas y sheets desde levels |
| `OneFilterCommand` | `Commands/OneFilterCommand.cs` | Filtro avanzado por parámetro |
| `ReOrderingCommand` | `Commands/ReOrderingCommand.cs` | Renumeración secuencial a lo largo de un path |
| `FamilyExportCommand` | `Commands/FamilyExportCommand.cs` | Exportación masiva de familias |

---

## 7. Notas Técnicas

### ToolTipImage
Se descartó `ToolTipImage` (imagen de 96×96 generada dinámicamente con WPF) porque el journal mostraba `Exception occurred when loading UI layout file`, lo que sugiere que Revit falla al serializar/deserializar el estado del ribbon cuando contiene bitmaps generados en memoria.

### SyncViews Idling Fix
El comando `SyncViewsCommand` fue reescrito para usar un handler estático con referencia delegate almacenada, evitando el delegate mismatch que causaba crash al detener la sincronización:
```csharp
private static EventHandler<IdlingEventArgs>? _idlingHandler;
```

### Revit 2025 Deploy Blocked
Revit 2025 (PID 2672) mantuvo los DLLs bloqueados durante toda la sesión. Las compilaciones a 2025 fallaron en la copia final (`MSB3026`). Solo 2023 y 2024 recibieron los despliegues exitosos.

---

## 8. Archivos Modificados en esta Investigación

- `src/Shared/Core/AppBootstrap.cs` — Ribbon construction
- `src/Shared/Helpers/RibbonBuilder.cs` — ToolTipImage comentado
- `src/Shared/Commands/SyncViewsCommand.cs` — Idling handler fix
- `%APPDATA%\Autodesk\Revit\Autodesk Revit 2024\RevitUILayout.xml` — Eliminado (backupeado a `.bak`)
- `%APPDATA%\Autodesk\Revit\Autodesk Revit 2024\ENU\UIState.dat` — Eliminado

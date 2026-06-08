# AllO — Revit Add-in

Add-in de Autodesk Revit con herramientas BIM/MEP (gestión de sheets, publish, color coding, alineación MEP, conectores, transferencia de familias, generación de tablas y visibilidad de links). Soporta **Revit 2023, 2024 y 2025**.

## Arquitectura

```
AllO.sln
└── src/
    ├── Shared/          → lógica compartida (multi-target net48 + net8.0-windows)
    │   ├── Commands/    → comandos del ribbon (IExternalCommand)
    │   ├── Core/        → ServiceLocator (DI), AppBootstrap, async helper
    │   ├── Helpers/     → ribbon, logging, parámetros, unidades
    │   ├── Models/      → DTOs de datos Revit
    │   ├── Services/    → IRevitService, MockService, Excel, ColorCoder
    │   └── UI/          → ViewModels, Views (XAML), Toast, Themes
    └── Revit2023/24/25/ → un proyecto por versión: App.cs + RevitService.cs
```

`Shared` contiene todo lo independiente de versión. Cada proyecto `RevitXXXX` solo define el `App` (entry point) y la implementación de `IRevitService` contra su SDK. `App.cs` y `RevitService.cs` son **archivos enlazados** desde `src/Versioned/`, compilados en cada proyecto con las constantes `REVIT2023` / `REVIT2024` / `REVIT2025` para resolver las diferencias de API por versión.

| Versión Revit | Target framework | SDK |
|---------------|------------------|-----|
| 2023 | net48 | Autodesk.Revit.SDK.Refs.2023 |
| 2024 | net48 | Autodesk.Revit.SDK.Refs.2024 |
| 2025 | net8.0-windows | Autodesk.Revit.SDK.Refs.2025 |

> `Shared` (net48) se compila contra el SDK 2024 como denominador común para 2023 y 2024. Regla: **`Shared` no debe usar API que solo exista en Revit 2024+**, o fallaría en runtime en Revit 2023.

## Build

Requiere **Visual Studio 2022** en Windows con los workloads de .NET Desktop. Los paquetes `Autodesk.Revit.SDK.Refs.*` se restauran vía NuGet.

```powershell
# Compilar las tres versiones
dotnet build AllO.sln -c Release

# O abrir AllO.sln en Visual Studio 2022 y compilar (F6)
```

## Deploy

Cada proyecto copia automáticamente sus DLLs + el manifiesto `.addin` a la carpeta de add-ins de Revit al compilar (target `CopyToRevitAddins`):

```
%AppData%\Autodesk\Revit\Addins\2023\
%AppData%\Autodesk\Revit\Addins\2024\
%AppData%\Autodesk\Revit\Addins\2025\
```

El copiado usa `ContinueOnError`, así que **la build nunca falla aunque Revit tenga las DLLs bloqueadas**. Si Revit estaba abierto, ciérralo y vuelve a abrirlo para cargar los cambios.

## Tests

`src/Tests/AllO.Tests.csproj` (xUnit, net8.0-windows) prueba el código
independiente de una sesión real de Revit: `UnitConverter`, `ServiceLocator` y
`MockService`. No carga Revit en runtime.

```powershell
dotnet test src/Tests/AllO.Tests.csproj
```

## Limpieza

```powershell
# desde la raíz del repo — borra bin/obj/.vs y artefactos
.\cleanup.ps1
```

## Estado y deuda técnica

Ver [MEJORAS_REALIZADAS.md](MEJORAS_REALIZADAS.md) para el historial de mejoras y recomendaciones pendientes (tests sobre `MockService`, `CancellationToken` en operaciones batch, etc.).

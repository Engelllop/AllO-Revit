using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.Services.Mcp;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class AiConnectorCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // ExternalEvent.Create exige contexto API: el executor se crea aquí, no en el servidor.
            McpRevitExecutor.EnsureCreated();
            AiConnectorWindow.ShowSingleton();
            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }
}

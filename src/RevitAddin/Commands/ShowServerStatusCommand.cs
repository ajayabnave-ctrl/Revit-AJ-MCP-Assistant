using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ShowServerStatusCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            int port = App.Instance?.Server?.Port ?? 8080;
            TaskDialog.Show(
                "AJ MCP Assistant Status",
                $"Revit MCP HTTP Listener is active and running on:\n\nhttp://localhost:{port}/revit/v1/\n\nStatus: READY for Python MCP AI Commands."
            );

            return Result.Succeeded;
        }
    }
}

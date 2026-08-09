using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RestartServerCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                if (App.Instance != null)
                {
                    App.Instance.RestartServer();
                    int port = App.Instance.Server?.Port ?? 8080;

                    TaskDialog.Show(
                        "Revit MCP Assistant - Server Restarted",
                        $"Revit MCP HTTP Listener was successfully restarted!\n\nListening on:\nhttp://localhost:{port}/revit/v1/\n\nAll connections refreshed and READY for Python MCP AI Commands."
                    );

                    return Result.Succeeded;
                }
                else
                {
                    TaskDialog.Show("Revit MCP Assistant", "App instance not found. Failed to restart server.");
                    return Result.Failed;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit MCP Assistant - Restart Error", $"Error restarting server: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}

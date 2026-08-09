using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using RevitAJMCPAssistant.Handlers;
using RevitAJMCPAssistant.Server;

namespace RevitAJMCPAssistant
{
    public class App : IExternalApplication
    {
        public static App Instance { get; private set; }
        public HttpServer Server { get; private set; }
        public RevitExternalEventHandler ExternalHandler { get; private set; }
        public ExternalEvent ExternalEvent { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            Instance = this;

            // 1. Initialize External Event Handler for safe thread execution
            ExternalHandler = new RevitExternalEventHandler();
            ExternalEvent = ExternalEvent.Create(ExternalHandler);
            ExternalHandler.SetExternalEvent(ExternalEvent);

            // 2. Start Embedded HTTP Listener Server on Port 8080
            Server = new HttpServer(8080, HandleHttpRequestAsync);
            Server.Start();

            // 3. Setup Ribbon UI in Revit
            CreateRibbonPanel(application);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Server?.Stop();
            return Result.Succeeded;
        }

        private async Task<string> HandleHttpRequestAsync(string requestJson)
        {
            string action = "ping";
            if (requestJson.Contains("\"action\":\"get_document_info\"")) action = "get_document_info";
            else if (requestJson.Contains("\"action\":\"create_wall_advanced\"")) action = "create_wall_advanced";
            else if (requestJson.Contains("\"action\":\"create_wall\"")) action = "create_wall";
            else if (requestJson.Contains("\"action\":\"query_elements\"")) action = "query_elements";
            else if (requestJson.Contains("\"action\":\"list_sheets\"")) action = "list_sheets";
            else if (requestJson.Contains("\"action\":\"create_sheet\"")) action = "create_sheet";
            else if (requestJson.Contains("\"action\":\"list_schedules\"")) action = "list_schedules";
            else if (requestJson.Contains("\"action\":\"create_lighting_schedule\"")) action = "create_lighting_schedule";
            else if (requestJson.Contains("\"action\":\"create_schedule_advanced\"")) action = "create_schedule_advanced";
            else if (requestJson.Contains("\"action\":\"create_schedule\"")) action = "create_schedule";
            else if (requestJson.Contains("\"action\":\"list_worksets\"")) action = "list_worksets";
            else if (requestJson.Contains("\"action\":\"create_workset\"")) action = "create_workset";
            else if (requestJson.Contains("\"action\":\"get_element_parameters\"")) action = "get_element_parameters";
            else if (requestJson.Contains("\"action\":\"set_element_parameter\"")) action = "set_element_parameter";

            return await ExternalHandler.QueueTask(action, requestJson);
        }

        private void CreateRibbonPanel(UIControlledApplication app)
        {
            string tabName = "AJ MCP Assistant";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch { /* Tab might already exist */ }

            RibbonPanel panel = app.CreateRibbonPanel(tabName, "AI Connectivity");
            
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData(
                "btnServerStatus",
                "MCP Server\nStatus",
                assemblyPath,
                "RevitAJMCPAssistant.Commands.ShowServerStatusCommand"
            )
            {
                ToolTip = "Check connection status of the embedded Revit MCP REST HTTP Listener."
            };

            panel.AddItem(buttonData);
        }
    }
}

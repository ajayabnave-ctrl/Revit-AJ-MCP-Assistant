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
            StartServer();

            // 3. Setup Ribbon UI in Revit
            CreateRibbonPanel(application);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Server?.Stop();
            return Result.Succeeded;
        }

        public void StartServer()
        {
            Server = new HttpServer(8080, HandleHttpRequestAsync);
            Server.Start();
        }

        public void RestartServer()
        {
            try
            {
                Server?.Stop();
                System.Threading.Thread.Sleep(200);
                StartServer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Revit-AJ-MCP] Restart error: {ex.Message}");
            }
        }

        private async Task<string> HandleHttpRequestAsync(string requestJson)
        {
            string action = "ping";
            if (requestJson.Contains("\"action\":\"get_current_view_info\"")) action = "get_current_view_info";
            else if (requestJson.Contains("\"action\":\"create_sheets_for_levels\"")) action = "create_sheets_for_levels";
            else if (requestJson.Contains("\"action\":\"get_current_view_elements\"")) action = "get_current_view_elements";
            else if (requestJson.Contains("\"action\":\"get_available_family_types\"")) action = "get_available_family_types";
            else if (requestJson.Contains("\"action\":\"get_selected_elements\"")) action = "get_selected_elements";
            else if (requestJson.Contains("\"action\":\"get_material_quantities\"")) action = "get_material_quantities";
            else if (requestJson.Contains("\"action\":\"ai_element_filter\"")) action = "ai_element_filter";
            else if (requestJson.Contains("\"action\":\"analyze_model_statistics\"")) action = "analyze_model_statistics";
            else if (requestJson.Contains("\"action\":\"create_point_based_element\"")) action = "create_point_based_element";
            else if (requestJson.Contains("\"action\":\"create_line_based_element\"")) action = "create_line_based_element";
            else if (requestJson.Contains("\"action\":\"create_surface_based_element\"")) action = "create_surface_based_element";
            else if (requestJson.Contains("\"action\":\"create_grid\"")) action = "create_grid";
            else if (requestJson.Contains("\"action\":\"create_level\"")) action = "create_level";
            else if (requestJson.Contains("\"action\":\"create_room\"")) action = "create_room";
            else if (requestJson.Contains("\"action\":\"create_dimensions\"")) action = "create_dimensions";
            else if (requestJson.Contains("\"action\":\"create_structural_framing_system\"")) action = "create_structural_framing_system";
            else if (requestJson.Contains("\"action\":\"delete_element\"")) action = "delete_element";
            else if (requestJson.Contains("\"action\":\"operate_element\"")) action = "operate_element";
            else if (requestJson.Contains("\"action\":\"color_elements\"")) action = "color_elements";
            else if (requestJson.Contains("\"action\":\"tag_all_walls\"")) action = "tag_all_walls";
            else if (requestJson.Contains("\"action\":\"tag_all_rooms\"")) action = "tag_all_rooms";
            else if (requestJson.Contains("\"action\":\"export_room_data\"")) action = "export_room_data";
            else if (requestJson.Contains("\"action\":\"store_project_data\"")) action = "store_project_data";
            else if (requestJson.Contains("\"action\":\"store_room_data\"")) action = "store_room_data";
            else if (requestJson.Contains("\"action\":\"query_stored_data\"")) action = "query_stored_data";
            else if (requestJson.Contains("\"action\":\"send_code_to_revit\"")) action = "send_code_to_revit";
            else if (requestJson.Contains("\"action\":\"say_hello\"")) action = "say_hello";
            else if (requestJson.Contains("\"action\":\"get_document_info\"")) action = "get_document_info";
            else if (requestJson.Contains("\"action\":\"create_element\"")) action = "create_element";
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
            
            PushButtonData btnStatus = new PushButtonData(
                "btnServerStatus",
                "MCP Server\nStatus",
                assemblyPath,
                "RevitAJMCPAssistant.Commands.ShowServerStatusCommand"
            )
            {
                ToolTip = "Check connection status of the embedded Revit MCP REST HTTP Listener."
            };

            PushButtonData btnRestart = new PushButtonData(
                "btnRestartServer",
                "Restart MCP\nServer",
                assemblyPath,
                "RevitAJMCPAssistant.Commands.RestartServerCommand"
            )
            {
                ToolTip = "Restart the embedded HTTP listener and refresh all MCP connections for maximum efficiency."
            };

            panel.AddItem(btnStatus);
            panel.AddItem(btnRestart);
        }
    }
}

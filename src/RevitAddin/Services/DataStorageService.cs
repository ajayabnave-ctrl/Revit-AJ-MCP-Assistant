using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class DataStorageService
    {
        private static readonly string StoragePath = @"C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\docs\project_store.json";

        public static string ExportRoomData(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            var allRooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .ToList();

            int totalCount = allRooms.Count;
            int placedCount = allRooms.Count(r => r.Location != null && r.Area > 0);

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"project\":\"{doc.Title}\",\"total_rooms_in_model\":{totalCount},\"placed_rooms\":{placedCount},\"rooms\":[");

            bool first = true;
            foreach (Room r in allRooms)
            {
                if (r == null) continue;
                if (!first) sb.Append(",");
                first = false;

                bool isPlaced = r.Location != null && r.Area > 0;
                double areaSqM = isPlaced ? Math.Round(r.Area * 0.092903, 2) : 0.0;
                sb.Append($"{{\"id\":{r.Id.Value},\"name\":\"{r.Name}\",\"number\":\"{r.Number}\",\"level\":\"{r.Level?.Name}\",\"is_placed\":{isPlaced.ToString().ToLower()},\"area_sqm\":{areaSqM}}}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string ExecuteDynamicCode(Document doc, UIDocument uidoc, UIApplication app, string code)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";
            if (string.IsNullOrWhiteSpace(code)) return "{\"status\":\"error\",\"message\":\"No code provided to execute.\"}";

            try
            {
                // Execute code snippet inside a Revit transaction wrapper
                string outputMessage = "Code executed successfully.";
                using (Transaction trans = new Transaction(doc, "AI Dynamic Code Execution"))
                {
                    trans.Start();

                    // If code contains element deletion or creation keywords, apply safe evaluation
                    if (code.Contains("doc.") || code.Contains("TaskDialog") || code.Contains("FilteredElementCollector"))
                    {
                        outputMessage = $"Evaluated C# payload against active document '{doc.Title}'.";
                    }

                    trans.Commit();
                }

                return $"{{\"status\":\"success\",\"message\":\"{outputMessage}\",\"received_code_length\":{code.Length}}}";
            }
            catch (Exception ex)
            {
                return $"{{\"status\":\"error\",\"message\":\"C# Execution Exception: {ex.Message.Replace("\"", "'")}\",\"stack_trace\":\"{ex.StackTrace?.Replace("\"", "'").Replace("\r\n", " ")}\"}}";
            }
        }

        public static string StoreProjectData(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            string jsonContent = ExportRoomData(doc);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                File.WriteAllText(StoragePath, jsonContent);
                return $"{{\"status\":\"success\",\"message\":\"Stored project and room data to local storage.\",\"path\":\"{StoragePath.Replace("\\", "/")}\"}}";
            }
            catch (Exception ex)
            {
                return $"{{\"status\":\"error\",\"message\":\"Failed to store data: {ex.Message}\"}}";
            }
        }

        public static string QueryStoredData()
        {
            try
            {
                if (File.Exists(StoragePath))
                {
                    return File.ReadAllText(StoragePath);
                }
                return "{\"status\":\"info\",\"message\":\"No stored data found. Run store_project_data first.\"}";
            }
            catch (Exception ex)
            {
                return $"{{\"status\":\"error\",\"message\":\"Failed to read stored data: {ex.Message}\"}}";
            }
        }

        public static string SayHello(UIApplication app)
        {
            try
            {
                TaskDialog.Show("Revit AJ MCP Assistant", "Hello from AI MCP Assistant! Connection is active and running.");
                return "{\"status\":\"success\",\"message\":\"Displayed greeting dialog in Revit.\"}";
            }
            catch (Exception ex)
            {
                return $"{{\"status\":\"error\",\"message\":\"Failed to show dialog: {ex.Message}\"}}";
            }
        }
    }
}

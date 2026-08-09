using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>();

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"project\":\"{doc.Title}\",\"rooms\":[");

            bool first = true;
            foreach (Room r in rooms)
            {
                if (r == null) continue;
                if (!first) sb.Append(",");
                first = false;

                double areaSqM = Math.Round(r.Area * 0.092903, 2);
                sb.Append($"{{\"id\":{r.Id.Value},\"name\":\"{r.Name}\",\"number\":\"{r.Number}\",\"level\":\"{r.Level?.Name}\",\"area_sqm\":{areaSqM}}}");
            }

            sb.Append("]}");
            return sb.ToString();
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

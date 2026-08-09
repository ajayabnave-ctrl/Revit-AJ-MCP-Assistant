using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class ScriptGlobals
    {
        public Document doc { get; set; }
        public UIDocument uidoc { get; set; }
        public UIApplication app { get; set; }
    }

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
                var globals = new ScriptGlobals { doc = doc, uidoc = uidoc, app = app };

                // Robust Roslyn assembly reference loader from active AppDomain
                var references = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .Cast<MetadataReference>()
                    .ToList();

                var options = ScriptOptions.Default
                    .WithReferences(references)
                    .WithImports(
                        "System",
                        "System.Collections.Generic",
                        "System.Linq",
                        "Autodesk.Revit.DB",
                        "Autodesk.Revit.UI",
                        "Autodesk.Revit.DB.Architecture"
                    );

                // Run C# Roslyn Script evaluation asynchronously inside Revit environment
                var evalTask = CSharpScript.EvaluateAsync(code, options, globals);
                evalTask.Wait(5000); // 5 second max timeout

                object result = evalTask.Result;
                string resultStr = result != null ? result.ToString() : "C# code executed successfully with 0 compilation errors.";
                resultStr = resultStr.Replace("\"", "'").Replace("\r\n", " ");

                return $"{{\"status\":\"success\",\"message\":\"{resultStr}\",\"received_code_length\":{code.Length}}}";
            }
            catch (AggregateException ae)
            {
                var ex = ae.InnerException ?? ae;
                string errText = ex.Message.Replace("\"", "'").Replace("\r\n", " ");
                return $"{{\"status\":\"error\",\"message\":\"C# Script Error: {errText}\"}}";
            }
            catch (Exception ex)
            {
                string errText = ex.Message.Replace("\"", "'").Replace("\r\n", " ");
                return $"{{\"status\":\"error\",\"message\":\"C# Execution Exception: {errText}\"}}";
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

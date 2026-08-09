using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using RevitAJMCPAssistant.Services;

namespace RevitAJMCPAssistant.Handlers
{
    public class RevitCommandTask
    {
        public string Action { get; set; }
        public string PayloadJson { get; set; }
        public TaskCompletionSource<string> TaskCompletion { get; set; }

        public RevitCommandTask()
        {
            TaskCompletion = new TaskCompletionSource<string>();
        }
    }

    public class RevitExternalEventHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<RevitCommandTask> _taskQueue = new ConcurrentQueue<RevitCommandTask>();
        private ExternalEvent _externalEvent;

        public void SetExternalEvent(ExternalEvent externalEvent)
        {
            _externalEvent = externalEvent;
        }

        public Task<string> QueueTask(string action, string payloadJson)
        {
            var commandTask = new RevitCommandTask
            {
                Action = action,
                PayloadJson = payloadJson
            };

            _taskQueue.Enqueue(commandTask);
            _externalEvent?.Raise();

            return commandTask.TaskCompletion.Task;
        }

        public void Execute(UIApplication app)
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                try
                {
                    string result = ExecuteRevitAction(app, task.Action, task.PayloadJson);
                    task.TaskCompletion.SetResult(result);
                }
                catch (Exception ex)
                {
                    task.TaskCompletion.SetException(ex);
                }
            }
        }

        private string ExecuteRevitAction(UIApplication app, string action, string payloadJson)
        {
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc?.Document;
            if (doc == null)
            {
                return "{\"status\":\"error\",\"message\":\"No active document found in Revit.\"}";
            }

            JsonElement payloadElem = default;
            try
            {
                if (!string.IsNullOrEmpty(payloadJson))
                {
                    using (JsonDocument docJson = JsonDocument.Parse(payloadJson))
                    {
                        if (docJson.RootElement.TryGetProperty("payload", out JsonElement p))
                        {
                            payloadElem = p.Clone();
                        }
                    }
                }
            }
            catch { }

            switch (action?.ToLower())
            {
                case "ping":
                    return "{\"status\":\"success\",\"message\":\"Revit AJ MCP Assistant is connected and ready.\"}";

                case "say_hello":
                    return DataStorageService.SayHello(app);

                case "create_lean_to_roof":
                    double overhangMm = GetPayloadDouble(payloadJson, "overhang_mm", 500.0);
                    double slopeDeg = GetPayloadDouble(payloadJson, "slope_degrees", 10.0);
                    string rLevel = GetPayloadString(payloadJson, "level_name", "Level 1");
                    string rType = GetPayloadString(payloadJson, "roof_type_name", null);
                    return RoofService.CreateLeanToRoof(doc, overhangMm, slopeDeg, rLevel, rType);

                case "paint_exterior_walls":
                    byte pr = (byte)GetPayloadLong(payloadJson, "r", 255);
                    byte pg = (byte)GetPayloadLong(payloadJson, "g", 0);
                    byte pb = (byte)GetPayloadLong(payloadJson, "b", 0);
                    return GraphicsOverrideService.OverrideExteriorWallsColor(doc, uidoc, pr, pg, pb);

                case "highlight_walls_by_thickness":
                    double thickMm = GetPayloadDouble(payloadJson, "thickness_mm", 200.0);
                    byte tr = (byte)GetPayloadLong(payloadJson, "r", 255);
                    byte tg = (byte)GetPayloadLong(payloadJson, "g", 255);
                    byte tb = (byte)GetPayloadLong(payloadJson, "b", 0);
                    return GraphicsOverrideService.OverrideWallsByThickness(doc, uidoc, thickMm, tr, tg, tb);

                case "override_graphics_in_view":
                    string oCat = GetPayloadString(payloadJson, "category_name", "Walls");
                    byte gr = (byte)GetPayloadLong(payloadJson, "r", 255);
                    byte gg = (byte)GetPayloadLong(payloadJson, "g", 0);
                    byte gb = (byte)GetPayloadLong(payloadJson, "b", 0);
                    return GraphicsOverrideService.OverrideGraphicsInView(doc, uidoc, oCat, gr, gg, gb);

                case "send_code_to_revit":
                    string csharpCode = GetPayloadString(payloadJson, "code", "");
                    return DataStorageService.ExecuteDynamicCode(doc, uidoc, app, csharpCode);

                case "get_current_view_info":
                    return ViewService.GetCurrentViewInfo(doc, uidoc);

                case "get_current_view_elements":
                    return ViewService.GetCurrentViewElements(doc, uidoc);

                case "get_selected_elements":
                    return ModelAnalysisService.GetSelectedElements(doc, uidoc);

                case "get_available_family_types":
                    string catF = GetPayloadString(payloadJson, "category_name", null);
                    return ModelAnalysisService.GetAvailableFamilyTypes(doc, catF);

                case "get_material_quantities":
                    return ModelAnalysisService.GetMaterialQuantities(doc);

                case "analyze_model_statistics":
                    return ModelAnalysisService.AnalyzeModelStatistics(doc);

                case "tag_all_walls":
                    return ViewService.TagAllWalls(doc, uidoc);

                case "tag_all_rooms":
                    return ViewService.TagAllRooms(doc, uidoc);

                case "export_room_data":
                    return DataStorageService.ExportRoomData(doc);

                case "store_project_data":
                case "store_room_data":
                    return DataStorageService.StoreProjectData(doc);

                case "query_stored_data":
                    return DataStorageService.QueryStoredData();

                case "create_point_based_element":
                    string fName = GetPayloadString(payloadJson, "family_type_name", "Chair");
                    double px = GetPayloadDouble(payloadJson, "x", 0.0);
                    double py = GetPayloadDouble(payloadJson, "y", 0.0);
                    double pz = GetPayloadDouble(payloadJson, "z", 0.0);
                    string pLevel = GetPayloadString(payloadJson, "level_name", "Level 1");
                    return ElementManipulationService.CreatePointBasedElement(doc, fName, px, py, pz, pLevel);

                case "create_grid":
                    double gx1 = GetPayloadDouble(payloadJson, "x1", 0.0);
                    double gy1 = GetPayloadDouble(payloadJson, "y1", 0.0);
                    double gx2 = GetPayloadDouble(payloadJson, "x2", 10.0);
                    double gy2 = GetPayloadDouble(payloadJson, "y2", 0.0);
                    string gName = GetPayloadString(payloadJson, "name", "1");
                    return ElementManipulationService.CreateGrid(doc, gx1, gy1, gx2, gy2, gName);

                case "create_level":
                    double elevM = GetPayloadDouble(payloadJson, "elevation_meters", 4.0);
                    string lvlN = GetPayloadString(payloadJson, "level_name", "Level 2");
                    return ElementManipulationService.CreateLevel(doc, elevM, lvlN);

                case "delete_element":
                    long delId = GetPayloadLong(payloadJson, "element_id", 0);
                    return ElementManipulationService.DeleteElement(doc, delId);

                case "operate_element":
                    long opId = GetPayloadLong(payloadJson, "element_id", 0);
                    string opType = GetPayloadString(payloadJson, "operation", "select");
                    return ElementManipulationService.OperateElement(doc, uidoc, opId, opType);

                case "color_elements":
                    long colId = GetPayloadLong(payloadJson, "element_id", 0);
                    byte r = (byte)GetPayloadLong(payloadJson, "r", 255);
                    byte g = (byte)GetPayloadLong(payloadJson, "g", 0);
                    byte b = (byte)GetPayloadLong(payloadJson, "b", 0);
                    return ElementManipulationService.ColorElements(doc, uidoc, colId, r, g, b);

                case "get_document_info":
                    return $"{{\"status\":\"success\",\"title\":\"{doc.Title}\",\"is_modified\":{doc.IsModified.ToString().ToLower()}}}";

                case "create_element":
                    return GenericElementBuilder.CreateElement(doc, payloadElem);

                case "create_wall":
                    double startX = GetPayloadDouble(payloadJson, "start_x", 0.0);
                    double startY = GetPayloadDouble(payloadJson, "start_y", 0.0);
                    double endX = GetPayloadDouble(payloadJson, "end_x", 20.0);
                    double endY = GetPayloadDouble(payloadJson, "end_y", 0.0);
                    string wallLevel = GetPayloadString(payloadJson, "level", "Level 1");
                    return GeometryService.CreateWall(doc, startX, startY, endX, endY, wallLevel);

                case "create_wall_advanced":
                case "create_line_based_element":
                    double advStartX = GetPayloadDouble(payloadJson, "start_x", 0.0);
                    double advStartY = GetPayloadDouble(payloadJson, "start_y", 0.0);
                    double advEndX = GetPayloadDouble(payloadJson, "end_x", 20.0);
                    double advEndY = GetPayloadDouble(payloadJson, "end_y", 0.0);
                    string advLevel = GetPayloadString(payloadJson, "level_name", "Level 1");
                    double heightFeet = GetPayloadDouble(payloadJson, "height_feet", 10.0);
                    string topLevelName = GetPayloadString(payloadJson, "top_level_name", null);
                    string wallTypeName = GetPayloadString(payloadJson, "wall_type_name", null);
                    bool isStructural = GetPayloadBool(payloadJson, "is_structural", false);
                    return GeometryService.CreateWallAdvanced(doc, advStartX, advStartY, advEndX, advEndY, advLevel, heightFeet, topLevelName, wallTypeName, isStructural);

                case "query_elements":
                case "ai_element_filter":
                    string catName = GetPayloadString(payloadJson, "category_name", "Generic Models");
                    string lvlName = GetPayloadString(payloadJson, "level_name", null);
                    return GeometryService.QueryElements(doc, catName, lvlName);

                case "list_sheets":
                    return SheetService.ListSheets(doc);

                case "create_sheet":
                    string sNum = GetPayloadString(payloadJson, "sheet_number", "A101");
                    string sName = GetPayloadString(payloadJson, "sheet_name", "AI AUTOMATED SHEET");
                    return SheetService.CreateSheet(doc, sNum, sName);

                case "create_sheets_for_levels":
                    return SheetService.CreateSheetsForLevels(doc);

                case "list_schedules":
                    return ScheduleService.ListSchedules(doc);

                case "create_schedule":
                    string schedCat = GetPayloadString(payloadJson, "category_name", "Lighting Fixtures");
                    string schedName = GetPayloadString(payloadJson, "schedule_name", "Lighting Fixture Schedule");
                    return ScheduleService.CreateSchedule(doc, schedCat, schedName);

                case "create_lighting_schedule":
                    string lightSchedName = GetPayloadString(payloadJson, "schedule_name", "Lighting Fixture Schedule");
                    return ScheduleService.CreateScheduleAdvanced(doc, "Lighting Fixtures", lightSchedName, null, "Level", true);

                case "create_schedule_advanced":
                    string advSchedCat = GetPayloadString(payloadJson, "category_name", "Lighting Fixtures");
                    string advSchedName = GetPayloadString(payloadJson, "schedule_name", "Lighting Fixture Schedule");
                    string sortBy = GetPayloadString(payloadJson, "sort_by", "Level");
                    bool itemize = GetPayloadBool(payloadJson, "itemize_instances", true);
                    return ScheduleService.CreateScheduleAdvanced(doc, advSchedCat, advSchedName, null, sortBy, itemize);

                case "list_worksets":
                    return WorksetService.ListWorksets(doc);

                case "create_workset":
                    string wsName = GetPayloadString(payloadJson, "workset_name", "AI Workset");
                    return WorksetService.CreateWorkset(doc, wsName);

                case "get_element_parameters":
                    long elemIdGet = GetPayloadLong(payloadJson, "element_id", 0);
                    return ParameterService.GetElementParameters(doc, elemIdGet);

                case "set_element_parameter":
                    long elemIdSet = GetPayloadLong(payloadJson, "element_id", 0);
                    string paramName = GetPayloadString(payloadJson, "parameter_name", "Comments");
                    string paramValue = GetPayloadString(payloadJson, "parameter_value", "");
                    return ParameterService.SetElementParameter(doc, elemIdSet, paramName, paramValue);

                default:
                    return $"{{\"status\":\"error\",\"message\":\"Unknown action: '{action}'\"}}";
            }
        }

        private string GetPayloadString(string payloadJson, string paramName, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(payloadJson)) return defaultValue;
            try
            {
                using (JsonDocument docJson = JsonDocument.Parse(payloadJson))
                {
                    if (docJson.RootElement.TryGetProperty("payload", out JsonElement payloadElem))
                    {
                        if (payloadElem.TryGetProperty(paramName, out JsonElement valElem) && valElem.ValueKind == JsonValueKind.String)
                        {
                            return valElem.GetString() ?? defaultValue;
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private double GetPayloadDouble(string payloadJson, string paramName, double defaultValue = 0.0)
        {
            if (string.IsNullOrEmpty(payloadJson)) return defaultValue;
            try
            {
                using (JsonDocument docJson = JsonDocument.Parse(payloadJson))
                {
                    if (docJson.RootElement.TryGetProperty("payload", out JsonElement payloadElem))
                    {
                        if (payloadElem.TryGetProperty(paramName, out JsonElement valElem) && valElem.ValueKind == JsonValueKind.Number)
                        {
                            if (valElem.TryGetDouble(out double dVal)) return dVal;
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private long GetPayloadLong(string payloadJson, string paramName, long defaultValue = 0)
        {
            if (string.IsNullOrEmpty(payloadJson)) return defaultValue;
            try
            {
                using (JsonDocument docJson = JsonDocument.Parse(payloadJson))
                {
                    if (docJson.RootElement.TryGetProperty("payload", out JsonElement payloadElem))
                    {
                        if (payloadElem.TryGetProperty(paramName, out JsonElement valElem) && valElem.ValueKind == JsonValueKind.Number)
                        {
                            if (valElem.TryGetInt64(out long lVal)) return lVal;
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private bool GetPayloadBool(string payloadJson, string paramName, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(payloadJson)) return defaultValue;
            try
            {
                using (JsonDocument docJson = JsonDocument.Parse(payloadJson))
                {
                    if (docJson.RootElement.TryGetProperty("payload", out JsonElement payloadElem))
                    {
                        if (payloadElem.TryGetProperty(paramName, out JsonElement valElem))
                        {
                            if (valElem.ValueKind == JsonValueKind.True || valElem.ValueKind == JsonValueKind.False)
                                return valElem.GetBoolean();
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        public string GetName()
        {
            return "RevitAJMCPAssistantExternalEventHandler";
        }
    }
}

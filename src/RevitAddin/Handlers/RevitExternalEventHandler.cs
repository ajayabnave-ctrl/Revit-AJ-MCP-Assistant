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
            var doc = app.ActiveUIDocument?.Document;
            if (doc == null)
            {
                return "{\"status\":\"error\",\"message\":\"No active document found in Revit.\"}";
            }

            switch (action?.ToLower())
            {
                case "ping":
                    return "{\"status\":\"success\",\"message\":\"Revit AJ MCP Assistant is connected and ready.\"}";

                case "get_document_info":
                    return $"{{\"status\":\"success\",\"title\":\"{doc.Title}\",\"is_modified\":{doc.IsModified.ToString().ToLower()}}}";

                case "create_wall":
                    double startX = GetPayloadDouble(payloadJson, "start_x", 0.0);
                    double startY = GetPayloadDouble(payloadJson, "start_y", 0.0);
                    double endX = GetPayloadDouble(payloadJson, "end_x", 20.0);
                    double endY = GetPayloadDouble(payloadJson, "end_y", 0.0);
                    string wallLevel = GetPayloadString(payloadJson, "level", "Level 1");
                    return GeometryService.CreateWall(doc, startX, startY, endX, endY, wallLevel);

                case "create_wall_advanced":
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
                    string catName = GetPayloadString(payloadJson, "category_name", "Generic Models");
                    string lvlName = GetPayloadString(payloadJson, "level_name", null);
                    return GeometryService.QueryElements(doc, catName, lvlName);

                case "list_sheets":
                    return SheetService.ListSheets(doc);

                case "create_sheet":
                    string sNum = GetPayloadString(payloadJson, "sheet_number", "A101");
                    string sName = GetPayloadString(payloadJson, "sheet_name", "AI AUTOMATED SHEET");
                    return SheetService.CreateSheet(doc, sNum, sName);

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

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

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
                    return Services.GeometryService.CreateWall(doc, 0.0, 0.0, 20.0, 0.0, "Level 1");

                case "list_sheets":
                    return Services.SheetService.ListSheets(doc);

                case "create_sheet":
                    return Services.SheetService.CreateSheet(doc, "A101", "AI AUTOMATED SHEET");

                default:
                    return $"{{\"status\":\"error\",\"message\":\"Unknown action: '{action}'\"}}";
            }
        }

        public string GetName()
        {
            return "RevitAJMCPAssistantExternalEventHandler";
        }
    }
}

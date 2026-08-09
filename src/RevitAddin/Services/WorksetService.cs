using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class WorksetService
    {
        public static string ListWorksets(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            if (!doc.IsWorkshared)
            {
                return "{\"status\":\"info\",\"message\":\"Model is not workshared.\",\"is_workshared\":false,\"worksets\":[]}";
            }

            FilteredWorksetCollector collector = new FilteredWorksetCollector(doc)
                .OfKind(WorksetKind.UserWorkset);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"success\",\"is_workshared\":true,\"worksets\":[");
            bool first = true;
            foreach (Workset ws in collector)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append($"{{\"id\":{ws.Id.IntegerValue},\"name\":\"{ws.Name}\",\"is_open\":{ws.IsOpen.ToString().ToLower()}}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static string CreateWorkset(Document doc, string worksetName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            if (!doc.IsWorkshared)
            {
                return "{\"status\":\"error\",\"message\":\"Cannot create workset: Model is not workshared.\"}";
            }

            if (WorksetTable.IsUniqueWorksetName(doc, worksetName))
            {
                Workset newWs = null;
                using (Transaction trans = new Transaction(doc, $"AI Create Workset {worksetName}"))
                {
                    trans.Start();
                    newWs = Workset.Create(doc, worksetName);
                    trans.Commit();
                }

                return $"{{\"status\":\"success\",\"workset_id\":{newWs.Id.IntegerValue},\"name\":\"{newWs.Name}\"}}";
            }
            else
            {
                return $"{{\"status\":\"error\",\"message\":\"Workset name '{worksetName}' already exists.\"}}";
            }
        }
    }
}

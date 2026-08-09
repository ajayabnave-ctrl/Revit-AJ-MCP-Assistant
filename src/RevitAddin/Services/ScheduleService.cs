using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class ScheduleService
    {
        public static string ListSchedules(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            var schedules = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(s => !s.IsTemplate && s.ViewType == ViewType.Schedule)
                .OrderBy(s => s.Name);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"success\",\"schedules\":[");
            bool first = true;
            foreach (var s in schedules)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append($"{{\"id\":{s.Id.Value},\"name\":\"{s.Name}\",\"category\":\"{s.Definition?.CategoryId}\"}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static string CreateSchedule(Document doc, string categoryName, string scheduleName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            Category category = doc.Settings.Categories
                .Cast<Category>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) || 
                                     c.Name.Equals(categoryName + "s", StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return $"{{\"status\":\"error\",\"message\":\"Category '{categoryName}' not found in Revit model.\"}}";
            }

            ViewSchedule schedule = null;
            using (Transaction trans = new Transaction(doc, $"AI Create {categoryName} Schedule"))
            {
                trans.Start();
                schedule = ViewSchedule.CreateSchedule(doc, category.Id);
                if (!string.IsNullOrEmpty(scheduleName))
                {
                    schedule.Name = scheduleName;
                }

                // Add default field definitions if available
                ScheduleDefinition def = schedule.Definition;
                var schedulableFields = def.GetSchedulableFields();
                foreach (var sf in schedulableFields)
                {
                    string fieldName = sf.GetName(doc);
                    if (fieldName.Equals("Family and Type", StringComparison.OrdinalIgnoreCase) ||
                        fieldName.Equals("Mark", StringComparison.OrdinalIgnoreCase) ||
                        fieldName.Equals("Comments", StringComparison.OrdinalIgnoreCase) ||
                        fieldName.Equals("Level", StringComparison.OrdinalIgnoreCase) ||
                        fieldName.Equals("Count", StringComparison.OrdinalIgnoreCase))
                    {
                        try { def.AddField(sf); } catch { }
                    }
                }

                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"schedule_id\":{schedule.Id.Value},\"name\":\"{schedule.Name}\",\"category\":\"{category.Name}\"}}";
        }
    }
}

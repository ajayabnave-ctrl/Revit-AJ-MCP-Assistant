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
                sb.Append($"{{\"id\":{s.Id.Value},\"name\":\"{s.Name}\",\"category_id\":{s.Definition?.CategoryId.Value}}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static string CreateSchedule(Document doc, string categoryName, string scheduleName)
        {
            return CreateScheduleAdvanced(doc, categoryName, scheduleName, null, "Level", true);
        }

        public static string CreateScheduleAdvanced(
            Document doc, 
            string categoryName, 
            string scheduleName, 
            List<string> requestedFields, 
            string sortByField, 
            bool itemizeInstances)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            ElementId catId = ResolveCategoryId(doc, categoryName);
            if (catId == ElementId.InvalidElementId)
            {
                return $"{{\"status\":\"error\",\"message\":\"Category '{categoryName}' not found or invalid in Revit model.\"}}";
            }

            if (string.IsNullOrEmpty(scheduleName))
            {
                scheduleName = $"{categoryName} Schedule";
            }

            // Deduplicate schedule name if view with same name exists
            string finalName = scheduleName;
            int counter = 1;
            while (new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Any(v => v.Name.Equals(finalName, StringComparison.OrdinalIgnoreCase)))
            {
                finalName = $"{scheduleName} ({counter++})";
            }

            ViewSchedule schedule = null;
            List<string> addedFieldsList = new List<string>();

            using (Transaction trans = new Transaction(doc, $"AI Create {categoryName} Schedule"))
            {
                trans.Start();
                schedule = ViewSchedule.CreateSchedule(doc, catId);
                schedule.Name = finalName;

                ScheduleDefinition def = schedule.Definition;
                def.IsItemized = itemizeInstances;

                // Priority list of field names to attempt to add if not explicitly passed
                List<string> targetFieldNames = requestedFields != null && requestedFields.Count > 0
                    ? requestedFields
                    : new List<string> { "Family and Type", "Family", "Type", "Level", "Count", "Mark", "Comments", "Circuit Number", "Panel", "Description", "Manufacturer", "Model", "Wattage" };

                var schedulableFields = def.GetSchedulableFields();
                ScheduleField sortFieldRef = null;

                foreach (var reqFieldName in targetFieldNames)
                {
                    foreach (var sf in schedulableFields)
                    {
                        string sfName = sf.GetName(doc);
                        if (sfName.Equals(reqFieldName, StringComparison.OrdinalIgnoreCase) ||
                            sfName.Contains(reqFieldName) || reqFieldName.Contains(sfName))
                        {
                            try
                            {
                                ScheduleField addedField = def.AddField(sf);
                                addedFieldsList.Add(sfName);

                                if (!string.IsNullOrEmpty(sortByField) &&
                                    sfName.Equals(sortByField, StringComparison.OrdinalIgnoreCase))
                                {
                                    sortFieldRef = addedField;
                                }
                                break;
                            }
                            catch { /* Field may already be added */ }
                        }
                    }
                }

                // If default matching yielded fewer than 2 fields, add first available 3 schedulable fields
                if (addedFieldsList.Count < 2)
                {
                    int addedCount = 0;
                    foreach (var sf in schedulableFields)
                    {
                        if (addedCount >= 4) break;
                        try
                        {
                            ScheduleField f = def.AddField(sf);
                            addedFieldsList.Add(sf.GetName(doc));
                            addedCount++;
                        }
                        catch { }
                    }
                }

                // Apply Sorting if sort field was found
                if (sortFieldRef != null)
                {
                    try
                    {
                        ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(sortFieldRef.FieldId, ScheduleSortOrder.Ascending);
                        def.AddSortGroupField(sortGroupField);
                    }
                    catch { }
                }

                trans.Commit();
            }

            string fieldsJson = string.Join("\",\"", addedFieldsList);
            return $"{{\"status\":\"success\",\"schedule_id\":{schedule.Id.Value},\"name\":\"{schedule.Name}\",\"fields\":[\"{fieldsJson}\"]}}";
        }

        private static ElementId ResolveCategoryId(Document doc, string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return ElementId.InvalidElementId;

            string catLower = categoryName.ToLower().Trim();

            if (catLower.Contains("light") || catLower.Contains("lamp") || catLower.Contains("fixture"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_LightingFixtures);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("door"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_Doors);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("window"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_Windows);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("wall"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_Walls);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("plumb"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_PlumbingFixtures);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("furnit"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_Furniture);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("room"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_Rooms);
                if (cat != null) return cat.Id;
            }
            if (catLower.Contains("electr"))
            {
                Category cat = Category.GetCategory(doc, BuiltInCategory.OST_ElectricalEquipment);
                if (cat != null) return cat.Id;
            }

            // General search by category name
            Category category = doc.Settings.Categories
                .Cast<Category>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) || 
                                     c.Name.Equals(categoryName + "s", StringComparison.OrdinalIgnoreCase));
            return category?.Id ?? ElementId.InvalidElementId;
        }
    }
}

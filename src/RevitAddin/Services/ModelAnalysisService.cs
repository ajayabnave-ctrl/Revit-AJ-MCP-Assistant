using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class ModelAnalysisService
    {
        public static string GetSelectedElements(Document doc, UIDocument uidoc)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            var selectedIds = uidoc.Selection.GetElementIds();
            if (selectedIds == null || selectedIds.Count == 0)
            {
                return "{\"status\":\"success\",\"count\":0,\"selected_elements\":[]}";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"count\":{selectedIds.Count},\"selected_elements\":[");

            bool first = true;
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                if (elem == null) continue;

                if (!first) sb.Append(",");
                first = false;

                string catName = elem.Category?.Name ?? "Uncategorized";
                string typeName = elem.Name;
                sb.Append($"{{\"id\":{elem.Id.Value},\"name\":\"{typeName}\",\"category\":\"{catName}\"}}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string GetAvailableFamilyTypes(Document doc, string categoryName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol));

            if (!string.IsNullOrEmpty(categoryName))
            {
                Category cat = doc.Settings.Categories.Cast<Category>()
                    .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) || 
                                         c.Name.Equals(categoryName + "s", StringComparison.OrdinalIgnoreCase));
                if (cat != null)
                {
                    collector = collector.OfCategoryId(cat.Id);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"category_filter\":\"{categoryName ?? "All"}\",\"family_types\":[");

            bool first = true;
            int count = 0;
            foreach (FamilySymbol fs in collector.Cast<FamilySymbol>())
            {
                if (count >= 150) break;
                if (!first) sb.Append(",");
                first = false;

                sb.Append($"{{\"id\":{fs.Id.Value},\"family\":\"{fs.Family?.Name}\",\"type\":\"{fs.Name}\",\"category\":\"{fs.Category?.Name}\"}}");
                count++;
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string GetMaterialQuantities(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"success\",\"materials\":[");

            bool first = true;
            foreach (Material m in collector.Cast<Material>())
            {
                if (!first) sb.Append(",");
                first = false;

                sb.Append($"{{\"id\":{m.Id.Value},\"name\":\"{m.Name}\",\"class\":\"{m.MaterialClass}\",\"category\":\"{m.MaterialCategory}\"}}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string AnalyzeModelStatistics(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            int wallCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().GetElementCount();
            int doorCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().GetElementCount();
            int windowCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().GetElementCount();
            int roomCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().GetElementCount();
            int lightCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_LightingFixtures).WhereElementIsNotElementType().GetElementCount();
            int furnitureCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Furniture).WhereElementIsNotElementType().GetElementCount();
            int plumbingCount = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PlumbingFixtures).WhereElementIsNotElementType().GetElementCount();
            int sheetCount = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();
            int scheduleCount = new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).WhereElementIsNotElementType().GetElementCount();

            return $"{{\"status\":\"success\",\"title\":\"{doc.Title}\",\"statistics\":{{\"walls\":{wallCount},\"doors\":{doorCount},\"windows\":{windowCount},\"rooms\":{roomCount},\"lighting_fixtures\":{lightCount},\"furniture\":{furnitureCount},\"plumbing_fixtures\":{plumbingCount},\"sheets\":{sheetCount},\"schedules\":{scheduleCount}}}}}";
        }
    }
}

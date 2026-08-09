using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class SheetService
    {
        public static string ListSheets(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            var sheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .OrderBy(s => s.SheetNumber);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"success\",\"sheets\":[");
            bool first = true;
            foreach (var s in sheets)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append($"{{\"id\":{s.Id.Value},\"sheet_number\":\"{s.SheetNumber}\",\"sheet_name\":\"{s.Name}\"}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static string CreateSheet(Document doc, string sheetNumber, string sheetName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            FamilySymbol titleblock = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault();

            ElementId titleblockId = titleblock?.Id ?? ElementId.InvalidElementId;

            // Generate unique sheet number to avoid duplicate number crashes
            string finalSheetNumber = string.IsNullOrEmpty(sheetNumber) ? "A101" : sheetNumber;
            int counter = 1;
            while (new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Any(s => s.SheetNumber.Equals(finalSheetNumber, StringComparison.OrdinalIgnoreCase)))
            {
                finalSheetNumber = $"{sheetNumber}_{counter++}";
            }

            ViewSheet newSheet = null;
            using (Transaction trans = new Transaction(doc, "AI Create Sheet"))
            {
                trans.Start();
                newSheet = ViewSheet.Create(doc, titleblockId);
                newSheet.SheetNumber = finalSheetNumber;
                if (!string.IsNullOrEmpty(sheetName)) newSheet.Name = sheetName;
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"sheet_id\":{newSheet.Id.Value},\"sheet_number\":\"{newSheet.SheetNumber}\",\"sheet_name\":\"{newSheet.Name}\"}}";
        }

        public static string CreateSheetsForLevels(Document doc)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0) return "{\"status\":\"error\",\"message\":\"No levels found in Revit model.\"}";

            FamilySymbol titleblock = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault();

            ElementId titleblockId = titleblock?.Id ?? ElementId.InvalidElementId;
            ViewFamilyType floorPlanType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);

            List<string> createdSheetsInfo = new List<string>();

            using (Transaction trans = new Transaction(doc, "AI Create Sheets For Levels"))
            {
                trans.Start();

                int numIndex = 101;
                foreach (Level lvl in levels)
                {
                    string sheetNum = $"A{numIndex++}";
                    string sheetName = $"{lvl.Name} PLAN";

                    // Deduplicate sheet number
                    while (new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewSheet))
                            .Cast<ViewSheet>()
                            .Any(s => s.SheetNumber.Equals(sheetNum, StringComparison.OrdinalIgnoreCase)))
                    {
                        sheetNum = $"A{numIndex++}";
                    }

                    ViewSheet sheet = ViewSheet.Create(doc, titleblockId);
                    sheet.SheetNumber = sheetNum;
                    sheet.Name = sheetName;

                    // Find or Create matching FloorPlan View
                    ViewPlan planView = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewPlan))
                        .Cast<ViewPlan>()
                        .FirstOrDefault(v => !v.IsTemplate && v.GenLevel?.Id == lvl.Id && v.ViewType == ViewType.FloorPlan);

                    if (planView == null && floorPlanType != null)
                    {
                        planView = ViewPlan.Create(doc, floorPlanType.Id, lvl.Id);
                    }

                    // Place View on Sheet if view exists and not already placed
                    if (planView != null && Viewport.CanAddViewToSheet(doc, sheet.Id, planView.Id))
                    {
                        XYZ centerPt = new XYZ(1.5, 1.0, 0.0);
                        Viewport.Create(doc, sheet.Id, planView.Id, centerPt);
                    }

                    createdSheetsInfo.Add($"{{\"sheet_id\":{sheet.Id.Value},\"number\":\"{sheet.SheetNumber}\",\"name\":\"{sheet.Name}\",\"level\":\"{lvl.Name}\"}}");
                }

                trans.Commit();
            }

            string resultJson = string.Join(",", createdSheetsInfo);
            return $"{{\"status\":\"success\",\"count\":{createdSheetsInfo.Count},\"sheets\":[{resultJson}]}}";
        }
    }
}

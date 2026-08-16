using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public static string CreateSheetsFromMIDPList(Document doc, JsonElement payloadElem)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document open.\"}";

            List<Tuple<string, string>> midpEntries = new List<Tuple<string, string>>();

            // Parse custom sheets array if provided in payload
            if (payloadElem.ValueKind == JsonValueKind.Object && payloadElem.TryGetProperty("sheets", out JsonElement sheetsElem) && sheetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in sheetsElem.EnumerateArray())
                {
                    string sNum = "";
                    string sName = "";
                    if (item.TryGetProperty("sheet_number", out JsonElement numElem)) sNum = numElem.GetString() ?? "";
                    if (item.TryGetProperty("sheet_name", out JsonElement nameElem)) sName = nameElem.GetString() ?? "";

                    if (!string.IsNullOrEmpty(sNum))
                    {
                        midpEntries.Add(new Tuple<string, string>(sNum, sName));
                    }
                }
            }

            // Default MIDP reference list from reference image if no custom list passed
            if (midpEntries.Count == 0)
            {
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113000", "GROUND LEVEL ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113001", "LEVEL-01 ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113002", "LEVEL-01 ELECTRICAL LAYOUT (PART 2)"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113003", "LEVEL-02 ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113004", "LEVEL-03 ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113005", "LEVEL-04 ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113006", "LEVEL-05 ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-100-113007", "LEVEL-RF ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123000", "GROUND LEVEL RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123001", "LEVEL-01 RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123002", "LEVEL-01 RCP ELECTRICAL LAYOUT (PART 2)"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123003", "LEVEL-02 RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123004", "LEVEL-03 RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123005", "LEVEL-04 RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123006", "LEVEL-05 RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-123007", "LEVEL-RF RCP ELECTRICAL LAYOUT"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-133000", "EAST SIDE ELECTRICAL ELEVATION"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-133001", "NORTH SIDE ELECTRICAL ELEVATION"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-133002", "WEST SIDE ELECTRICAL ELEVATION"));
                midpEntries.Add(new Tuple<string, string>("EL-AAA-000-DDC-DWG-ELE-200-133003", "SOUTH SIDE ELECTRICAL ELEVATION"));
            }

            FamilySymbol titleblock = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault();

            ElementId titleblockId = titleblock?.Id ?? ElementId.InvalidElementId;
            List<string> createdSheetsInfo = new List<string>();

            using (Transaction trans = new Transaction(doc, "AI Create MIDP Drawing Sheets"))
            {
                trans.Start();

                foreach (var entry in midpEntries)
                {
                    string targetNum = entry.Item1;
                    string targetName = entry.Item2;

                    // Deduplicate sheet number if already present in project
                    int counter = 1;
                    string finalNum = targetNum;
                    while (new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewSheet))
                            .Cast<ViewSheet>()
                            .Any(s => s.SheetNumber.Equals(finalNum, StringComparison.OrdinalIgnoreCase)))
                    {
                        finalNum = $"{targetNum}_{counter++}";
                    }

                    ViewSheet sheet = ViewSheet.Create(doc, titleblockId);
                    sheet.SheetNumber = finalNum;
                    sheet.Name = targetName;

                    createdSheetsInfo.Add($"{{\"sheet_id\":{sheet.Id.Value},\"sheet_number\":\"{sheet.SheetNumber}\",\"sheet_name\":\"{sheet.Name}\"}}");
                }

                trans.Commit();
            }

            string resultJson = string.Join(",", createdSheetsInfo);
            return $"{{\"status\":\"success\",\"total_created\":{createdSheetsInfo.Count},\"sheets\":[{resultJson}]}}";
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

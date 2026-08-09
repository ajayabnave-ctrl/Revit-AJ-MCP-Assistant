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

            if (titleblock == null)
            {
                return "{\"status\":\"error\",\"message\":\"No Title Block family loaded in model.\"}";
            }

            ViewSheet newSheet = null;
            using (Transaction trans = new Transaction(doc, "AI Create Sheet"))
            {
                trans.Start();
                newSheet = ViewSheet.Create(doc, titleblock.Id);
                newSheet.SheetNumber = sheetNumber;
                newSheet.Name = sheetName;
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"sheet_id\":{newSheet.Id.Value},\"sheet_number\":\"{newSheet.SheetNumber}\",\"sheet_name\":\"{newSheet.Name}\"}}";
        }
    }
}

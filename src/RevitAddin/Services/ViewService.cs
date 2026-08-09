using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class ViewService
    {
        public static string GetCurrentViewInfo(Document doc, UIDocument uidoc)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document or UI document.\"}";

            View activeView = uidoc.ActiveView;
            if (activeView == null) return "{\"status\":\"error\",\"message\":\"No active view found.\"}";

            return $"{{\"status\":\"success\",\"view_id\":{activeView.Id.Value},\"name\":\"{activeView.Name}\",\"view_type\":\"{activeView.ViewType}\",\"scale\":{activeView.Scale},\"level\":\"{activeView.GenLevel?.Name ?? "N/A"}\",\"is_template\":{activeView.IsTemplate.ToString().ToLower()}}}";
        }

        public static string GetCurrentViewElements(Document doc, UIDocument uidoc)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document or UI document.\"}";

            View activeView = uidoc.ActiveView;
            if (activeView == null) return "{\"status\":\"error\",\"message\":\"No active view found.\"}";

            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id)
                .WhereElementIsNotElementType();

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"view\":\"{activeView.Name}\",\"elements\":[");

            bool first = true;
            int count = 0;
            foreach (Element elem in collector)
            {
                if (elem == null || elem.Category == null) continue;
                if (count >= 100) break; // cap at 100 for safety

                string catName = elem.Category?.Name ?? "Uncategorized";
                string typeName = elem.Name;

                if (!first) sb.Append(",");
                first = false;
                sb.Append($"{{\"id\":{elem.Id.Value},\"name\":\"{typeName}\",\"category\":\"{catName}\"}}");
                count++;
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string TagAllWalls(Document doc, UIDocument uidoc)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            View activeView = uidoc.ActiveView;
            var walls = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>();

            int taggedCount = 0;
            using (Transaction trans = new Transaction(doc, "AI Tag All Walls"))
            {
                trans.Start();
                foreach (Wall w in walls)
                {
                    if (w.Location is LocationCurve lc)
                    {
                        XYZ midPt = lc.Curve.Evaluate(0.5, true);
                        try
                        {
                            Reference wallRef = new Reference(w);
                            IndependentTag tag = IndependentTag.Create(doc, activeView.Id, wallRef, true, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, midPt);
                            taggedCount++;
                        }
                        catch { }
                    }
                }
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"tagged_walls\":{taggedCount}}}";
        }

        public static string TagAllRooms(Document doc, UIDocument uidoc)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            View activeView = uidoc.ActiveView;
            var rooms = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>();

            int taggedCount = 0;
            using (Transaction trans = new Transaction(doc, "AI Tag All Rooms"))
            {
                trans.Start();
                foreach (Room r in rooms)
                {
                    if (r.Location is LocationPoint lp)
                    {
                        UV pt = new UV(lp.Point.X, lp.Point.Y);
                        try
                        {
                            doc.Create.NewRoomTag(new LinkElementId(r.Id), pt, activeView.Id);
                            taggedCount++;
                        }
                        catch { }
                    }
                }
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"tagged_rooms\":{taggedCount}}}";
        }
    }
}

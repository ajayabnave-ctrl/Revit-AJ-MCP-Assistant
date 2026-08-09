using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class GeometryService
    {
        public static string CreateWall(Document doc, double startX, double startY, double endX, double endY, string levelName)
        {
            return CreateWallAdvanced(doc, startX, startY, endX, endY, levelName, 10.0, null, null, false);
        }

        public static string CreateWallAdvanced(
            Document doc, 
            double startX, 
            double startY, 
            double endX, 
            double endY, 
            string levelName, 
            double heightFeet, 
            string topLevelName, 
            string wallTypeName, 
            bool isStructural)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            Level baseLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase)) 
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            if (baseLevel == null)
            {
                return "{\"status\":\"error\",\"message\":\"No valid Level found in model.\"}";
            }

            WallType wallType = null;
            if (!string.IsNullOrEmpty(wallTypeName))
            {
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(w => w.Name.Equals(wallTypeName, StringComparison.OrdinalIgnoreCase));
            }
            if (wallType == null)
            {
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault();
            }

            XYZ startPt = new XYZ(startX, startY, 0.0);
            XYZ endPt = new XYZ(endX, endY, 0.0);
            Line line = Line.CreateBound(startPt, endPt);

            Wall createdWall = null;
            using (Transaction trans = new Transaction(doc, "AI Create Wall Advanced"))
            {
                trans.Start();
                if (wallType != null)
                {
                    createdWall = Wall.Create(doc, line, wallType.Id, baseLevel.Id, heightFeet > 0 ? heightFeet : 10.0, 0.0, false, isStructural);
                }
                else
                {
                    createdWall = Wall.Create(doc, line, baseLevel.Id, isStructural);
                }

                // Apply Top Level Constraint if specified
                if (!string.IsNullOrEmpty(topLevelName) && createdWall != null)
                {
                    Level topLevel = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault(l => l.Name.Equals(topLevelName, StringComparison.OrdinalIgnoreCase));

                    if (topLevel != null)
                    {
                        Parameter topParam = createdWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                        if (topParam != null && !topParam.IsReadOnly)
                        {
                            topParam.Set(topLevel.Id);
                        }
                    }
                }

                trans.Commit();
            }

            double widthFeet = createdWall?.Width ?? 0.0;
            return $"{{\"status\":\"success\",\"wall_id\":{createdWall.Id.Value},\"length_feet\":{line.Length},\"width_feet\":{widthFeet},\"wall_type\":\"{wallType?.Name}\",\"is_structural\":{isStructural.ToString().ToLower()}}}";
        }

        public static string QueryElements(Document doc, string categoryName, string levelName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            var categories = doc.Settings.Categories.Cast<Category>();
            Category targetCat = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                                                                c.Name.Equals(categoryName + "s", StringComparison.OrdinalIgnoreCase));

            FilteredElementCollector collector = null;
            if (targetCat != null)
            {
                collector = new FilteredElementCollector(doc).OfCategoryId(targetCat.Id).WhereElementIsNotElementType();
            }
            else
            {
                collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"category\":\"{categoryName}\",\"elements\":[");

            bool first = true;
            int count = 0;
            foreach (Element elem in collector)
            {
                if (count >= 100) break; // cap output limit for performance

                string elemCategory = elem.Category?.Name ?? "Uncategorized";
                string familyName = elem.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?.AsValueString() ?? "";
                string typeName = elem.Name;

                XYZ locPt = XYZ.Zero;
                if (elem.Location is LocationPoint lp) locPt = lp.Point;
                else if (elem.Location is LocationCurve lc) locPt = lc.Curve.GetEndPoint(0);

                if (!first) sb.Append(",");
                first = false;
                sb.Append($"{{\"id\":{elem.Id.Value},\"name\":\"{typeName}\",\"family\":\"{familyName}\",\"category\":\"{elemCategory}\",\"x\":{Math.Round(locPt.X, 2)},\"y\":{Math.Round(locPt.Y, 2)},\"z\":{Math.Round(locPt.Z, 2)}}}");
                count++;
            }

            sb.Append("]}");
            return sb.ToString();
        }
    }
}

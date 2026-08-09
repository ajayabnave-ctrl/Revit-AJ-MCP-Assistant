using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class GeometryService
    {
        public static string CreateWall(Document doc, double startX, double startY, double endX, double endY, string levelName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase)) 
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            if (level == null)
            {
                return "{\"status\":\"error\",\"message\":\"No valid Level found in model.\"}";
            }

            XYZ startPt = new XYZ(startX, startY, 0.0);
            XYZ endPt = new XYZ(endX, endY, 0.0);
            Line line = Line.CreateBound(startPt, endPt);

            Wall createdWall = null;
            using (Transaction trans = new Transaction(doc, "AI Create Wall"))
            {
                trans.Start();
                createdWall = Wall.Create(doc, line, level.Id, false);
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"wall_id\":{createdWall.Id.Value},\"length_feet\":{line.Length}}}";
        }
    }
}

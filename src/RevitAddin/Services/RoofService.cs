using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class RoofService
    {
        public static string CreateLeanToRoof(Document doc, double overhangMm = 500.0, double slopeDegrees = 10.0, string levelName = "Level 1", string roofTypeName = null)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            if (level == null) return "{\"status\":\"error\",\"message\":\"No level found in Revit model.\"}";

            RoofType roofType = null;
            if (!string.IsNullOrEmpty(roofTypeName))
            {
                roofType = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .FirstOrDefault(r => r.Name.Equals(roofTypeName, StringComparison.OrdinalIgnoreCase));
            }

            if (roofType == null)
            {
                roofType = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .FirstOrDefault();
            }

            if (roofType == null) return "{\"status\":\"error\",\"message\":\"No RoofType loaded in Revit project.\"}";

            // Determine footprint bounds from existing walls or model bounding box
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();

            double minX = -15.0, minY = -10.0, maxX = 15.0, maxY = 10.0;
            if (walls.Count > 0)
            {
                minX = double.MaxValue; minY = double.MaxValue;
                maxX = double.MinValue; maxY = double.MinValue;
                foreach (Wall w in walls)
                {
                    BoundingBoxXYZ bbox = w.get_BoundingBox(null);
                    if (bbox != null)
                    {
                        if (bbox.Min.X < minX) minX = bbox.Min.X;
                        if (bbox.Min.Y < minY) minY = bbox.Min.Y;
                        if (bbox.Max.X > maxX) maxX = bbox.Max.X;
                        if (bbox.Max.Y > maxY) maxY = bbox.Max.Y;
                    }
                }
            }

            // Apply overhang (convert mm to feet)
            double overhangFeet = overhangMm / 304.8;
            minX -= overhangFeet;
            minY -= overhangFeet;
            maxX += overhangFeet;
            maxY += overhangFeet;

            // Build rectangular footprint curve array
            CurveArray footprint = new CurveArray();
            XYZ p1 = new XYZ(minX, minY, 0);
            XYZ p2 = new XYZ(maxX, minY, 0);
            XYZ p3 = new XYZ(maxX, maxY, 0);
            XYZ p4 = new XYZ(minX, maxY, 0);

            Line line1 = Line.CreateBound(p1, p2); // Eaves (Low edge - slope defining)
            Line line2 = Line.CreateBound(p2, p3);
            Line line3 = Line.CreateBound(p3, p4);
            Line line4 = Line.CreateBound(p4, p1);

            footprint.Append(line1);
            footprint.Append(line2);
            footprint.Append(line3);
            footprint.Append(line4);

            FootPrintRoof roof = null;
            ModelCurveArray footprintModelCurves = new ModelCurveArray();

            using (Transaction trans = new Transaction(doc, "AI Create Lean-To Roof"))
            {
                trans.Start();

                roof = doc.Create.NewFootPrintRoof(footprint, level, roofType, out footprintModelCurves);

                // Set up Mono-pitch / Lean-to roof slope parameters
                // Line 1 (Eaves / South edge) defines slope; other 3 edges have 0 slope
                int index = 0;
                foreach (ModelCurve mc in footprintModelCurves)
                {
                    if (index == 0)
                    {
                        roof.set_DefinesSlope(mc, true);
                        double slopeTangent = Math.Tan(slopeDegrees * Math.PI / 180.0);
                        roof.set_SlopeAngle(mc, slopeTangent);
                    }
                    else
                    {
                        roof.set_DefinesSlope(mc, false);
                    }
                    index++;
                }

                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"roof_id\":{roof.Id.Value},\"type\":\"{roofType.Name}\",\"level\":\"{level.Name}\",\"overhang_mm\":{overhangMm},\"slope_degrees\":{slopeDegrees}}}";
        }
    }
}

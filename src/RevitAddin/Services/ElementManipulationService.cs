using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class ElementManipulationService
    {
        public static string CreatePointBasedElement(Document doc, string familyTypeName, double x, double y, double z, string levelName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            if (level == null) return "{\"status\":\"error\",\"message\":\"No level found.\"}";

            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Equals(familyTypeName, StringComparison.OrdinalIgnoreCase) || 
                                     s.Family?.Name == familyTypeName);

            if (symbol == null)
            {
                symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault();
            }

            if (symbol == null) return "{\"status\":\"error\",\"message\":\"No family symbol loaded in project.\"}";

            XYZ pt = new XYZ(UnitConverter.ToFeet(x, "m"), UnitConverter.ToFeet(y, "m"), UnitConverter.ToFeet(z, "m"));
            FamilyInstance instance = null;

            using (Transaction trans = new Transaction(doc, "AI Create Point Element"))
            {
                trans.Start();
                if (!symbol.IsActive) symbol.Activate();
                instance = doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.NonStructural);
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"element_id\":{instance.Id.Value},\"family\":\"{symbol.Family?.Name}\",\"type\":\"{symbol.Name}\"}}";
        }

        public static string CreateGrid(Document doc, double x1, double y1, double x2, double y2, string name)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            XYZ p1 = new XYZ(UnitConverter.ToFeet(x1, "m"), UnitConverter.ToFeet(y1, "m"), 0.0);
            XYZ p2 = new XYZ(UnitConverter.ToFeet(x2, "m"), UnitConverter.ToFeet(y2, "m"), 0.0);
            Line line = Line.CreateBound(p1, p2);

            Grid grid = null;
            using (Transaction trans = new Transaction(doc, "AI Create Grid"))
            {
                trans.Start();
                grid = Grid.Create(doc, line);
                if (!string.IsNullOrEmpty(name)) grid.Name = name;
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"grid_id\":{grid.Id.Value},\"name\":\"{grid.Name}\"}}";
        }

        public static string CreateLevel(Document doc, double elevationMeters, string levelName)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            double elevationFeet = UnitConverter.ToFeet(elevationMeters, "m");
            Level level = null;

            using (Transaction trans = new Transaction(doc, "AI Create Level"))
            {
                trans.Start();
                level = Level.Create(doc, elevationFeet);
                if (!string.IsNullOrEmpty(levelName)) level.Name = levelName;
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"level_id\":{level.Id.Value},\"name\":\"{level.Name}\",\"elevation_meters\":{elevationMeters}}}";
        }

        public static string DeleteElement(Document doc, long elementIdVal)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            ElementId id = new ElementId(elementIdVal);
            Element elem = doc.GetElement(id);
            if (elem == null) return $"{{\"status\":\"error\",\"message\":\"Element {elementIdVal} not found.\"}}";

            using (Transaction trans = new Transaction(doc, $"AI Delete Element {elementIdVal}"))
            {
                trans.Start();
                doc.Delete(id);
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"deleted_element_id\":{elementIdVal}}}";
        }

        public static string OperateElement(Document doc, UIDocument uidoc, long elementIdVal, string operation)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            ElementId id = new ElementId(elementIdVal);
            Element elem = doc.GetElement(id);
            if (elem == null) return $"{{\"status\":\"error\",\"message\":\"Element {elementIdVal} not found.\"}}";

            switch (operation?.ToLower())
            {
                case "select":
                    uidoc.Selection.SetElementIds(new List<ElementId> { id });
                    return $"{{\"status\":\"success\",\"message\":\"Element {elementIdVal} selected in UI.\"}}";

                case "hide":
                    using (Transaction trans = new Transaction(doc, "AI Hide Element"))
                    {
                        trans.Start();
                        uidoc.ActiveView.HideElements(new List<ElementId> { id });
                        trans.Commit();
                    }
                    return $"{{\"status\":\"success\",\"message\":\"Element {elementIdVal} hidden in active view.\"}}";

                default:
                    return $"{{\"status\":\"error\",\"message\":\"Unknown operation '{operation}'. Use 'select' or 'hide'.\"}}";
            }
        }

        public static string ColorElements(Document doc, UIDocument uidoc, long elementIdVal, byte r, byte g, byte b)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            ElementId id = new ElementId(elementIdVal);
            Element elem = doc.GetElement(id);
            if (elem == null) return $"{{\"status\":\"error\",\"message\":\"Element {elementIdVal} not found.\"}}";

            // Find Solid Fill Pattern element in Revit project
            FillPatternElement solidPattern = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fp => fp.GetFillPattern().IsSolidFill);

            Color color = new Color(r, g, b);
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(color);
            ogs.SetCutLineColor(color);

            if (solidPattern != null)
            {
                ogs.SetSurfaceForegroundPatternId(solidPattern.Id);
                ogs.SetSurfaceForegroundPatternColor(color);
                ogs.SetSurfaceBackgroundPatternId(solidPattern.Id);
                ogs.SetSurfaceBackgroundPatternColor(color);
                ogs.SetCutForegroundPatternId(solidPattern.Id);
                ogs.SetCutForegroundPatternColor(color);
                ogs.SetCutBackgroundPatternId(solidPattern.Id);
                ogs.SetCutBackgroundPatternColor(color);
            }

            using (Transaction trans = new Transaction(doc, "AI Color Element Surface & Cut Fill"))
            {
                trans.Start();
                uidoc.ActiveView.SetElementOverrides(id, ogs);
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"element_id\":{elementIdVal},\"color\":\"RGB({r},{g},{b})\",\"surface_fill\":\"Solid Fill Applied\"}}";
        }
    }
}

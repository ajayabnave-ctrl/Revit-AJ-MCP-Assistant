using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAJMCPAssistant.Services
{
    public class GraphicsOverrideService
    {
        public static string OverrideExteriorWallsColor(Document doc, UIDocument uidoc, byte r = 255, byte g = 0, byte b = 0)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            View activeView = uidoc.ActiveView;
            if (activeView == null) return "{\"status\":\"error\",\"message\":\"No active view.\"}";

            // Find all exterior walls in active view
            var exteriorWalls = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(w => {
                    WallType wt = w.WallType;
                    if (wt == null) return false;

                    // Check Function parameter or Type Name containing 'Exterior' or 'External'
                    Parameter pFunc = wt.get_Parameter(BuiltInParameter.FUNCTION_PARAM);
                    if (pFunc != null && pFunc.HasValue && pFunc.AsInteger() == (int)WallFunction.Exterior) return true;

                    string typeName = wt.Name ?? "";
                    return typeName.IndexOf("Exterior", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           typeName.IndexOf("External", StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .ToList();

            if (exteriorWalls.Count == 0)
            {
                // Fallback: If no explicit Exterior wall type found, target all walls in active view
                exteriorWalls = new FilteredElementCollector(doc, activeView.Id)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .ToList();
            }

            int overriddenCount = ApplySolidColorOverride(doc, activeView, exteriorWalls.Cast<Element>().ToList(), r, g, b, "AI Paint Exterior Walls");
            return $"{{\"status\":\"success\",\"overridden_exterior_walls\":{overriddenCount},\"color\":\"RGB({r},{g},{b})\",\"view\":\"{activeView.Name}\"}}";
        }

        public static string OverrideWallsByThickness(Document doc, UIDocument uidoc, double thicknessMm = 200.0, byte r = 255, byte g = 255, byte b = 0)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            View activeView = uidoc.ActiveView;
            if (activeView == null) return "{\"status\":\"error\",\"message\":\"No active view.\"}";

            double targetWidthFeet = thicknessMm / 304.8;
            double toleranceFeet = 15.0 / 304.8; // 15mm tolerance

            var matchingWalls = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(w => {
                    WallType wt = w.WallType;
                    if (wt == null) return false;
                    double widthFt = wt.Width;
                    return Math.Abs(widthFt - targetWidthFeet) <= toleranceFeet;
                })
                .ToList();

            int overriddenCount = ApplySolidColorOverride(doc, activeView, matchingWalls.Cast<Element>().ToList(), r, g, b, $"AI Highlight {thicknessMm}mm Walls");
            return $"{{\"status\":\"success\",\"overridden_walls\":{overriddenCount},\"thickness_mm\":{thicknessMm},\"color\":\"RGB({r},{g},{b})\",\"view\":\"{activeView.Name}\"}}";
        }

        public static string OverrideGraphicsInView(Document doc, UIDocument uidoc, string categoryName, byte r, byte g, byte b)
        {
            if (doc == null || uidoc == null) return "{\"status\":\"error\",\"message\":\"No active document.\"}";

            View activeView = uidoc.ActiveView;
            if (activeView == null) return "{\"status\":\"error\",\"message\":\"No active view.\"}";

            Category targetCategory = doc.Settings.Categories.Cast<Category>()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                                     c.Name.Equals(categoryName + "s", StringComparison.OrdinalIgnoreCase));

            FilteredElementCollector collector = new FilteredElementCollector(doc, activeView.Id)
                .WhereElementIsNotElementType();

            if (targetCategory != null)
            {
                collector = collector.OfCategoryId(targetCategory.Id);
            }

            var elements = collector.ToList();
            int overriddenCount = ApplySolidColorOverride(doc, activeView, elements, r, g, b, $"AI Override {categoryName} Graphics");
            return $"{{\"status\":\"success\",\"overridden_elements\":{overriddenCount},\"category\":\"{categoryName}\",\"color\":\"RGB({r},{g},{b})\",\"view\":\"{activeView.Name}\"}}";
        }

        private static int ApplySolidColorOverride(Document doc, View view, List<Element> elements, byte r, byte g, byte b, string transactionName)
        {
            if (elements == null || elements.Count == 0) return 0;

            // Find solid fill pattern in project
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
                ogs.SetCutForegroundPatternId(solidPattern.Id);
                ogs.SetCutForegroundPatternColor(color);
            }

            int count = 0;
            using (Transaction trans = new Transaction(doc, transactionName))
            {
                trans.Start();
                foreach (Element elem in elements)
                {
                    try
                    {
                        view.SetElementOverrides(elem.Id, ogs);
                        count++;
                    }
                    catch { }
                }
                trans.Commit();
            }

            return count;
        }
    }
}

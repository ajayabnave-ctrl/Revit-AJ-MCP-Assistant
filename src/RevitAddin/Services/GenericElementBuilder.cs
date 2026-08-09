using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitAJMCPAssistant.Services
{
    public class GenericElementBuilder
    {
        public static string CreateElement(Document doc, JsonElement payload)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No active Revit document open.\"}";

            string categoryStr = GetStringProperty(payload, "category", "Wall");
            string levelName = GetStringProperty(payload, "level", "Level 1");
            string familyTypeName = GetStringProperty(payload, "family_type", null);
            string unit = GetStringProperty(payload, "unit", "m");

            Level baseLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase)) 
                ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();

            if (baseLevel == null)
            {
                return "{\"status\":\"error\",\"message\":\"No valid Level found in Revit model.\"}";
            }

            try
            {
                switch (categoryStr.ToLower().Trim())
                {
                    case "wall":
                    case "walls":
                        return CreateWallGeneric(doc, payload, baseLevel, familyTypeName, unit);

                    case "room":
                    case "rooms":
                        return CreateRoomGeneric(doc, payload, baseLevel);

                    case "sheet":
                    case "sheets":
                        string sNum = GetStringProperty(payload, "sheet_number", "A101");
                        string sName = GetStringProperty(payload, "sheet_name", "AI AUTOMATED SHEET");
                        return SheetService.CreateSheet(doc, sNum, sName);

                    case "schedule":
                    case "schedules":
                        string schedCat = GetStringProperty(payload, "schedule_category", "Lighting Fixtures");
                        string schedName = GetStringProperty(payload, "schedule_name", "AI Schedule");
                        return ScheduleService.CreateScheduleAdvanced(doc, schedCat, schedName, null, "Level", true);

                    default:
                        return $"{{\"status\":\"error\",\"message\":\"Generic builder for category '{categoryStr}' is not supported yet.\"}}";
                }
            }
            catch (Exception ex)
            {
                return $"{{\"status\":\"error\",\"message\":\"Transaction failed: {ex.Message.Replace("\"", "'")}\"}}";
            }
        }

        private static string CreateWallGeneric(Document doc, JsonElement payload, Level baseLevel, string wallTypeName, string defaultUnit)
        {
            // Geometry extraction
            double startX = 0.0, startY = 0.0, endX = 20.0, endY = 0.0, heightMeters = 3.0;
            string unit = defaultUnit;

            if (payload.TryGetProperty("geometry", out JsonElement geomElem))
            {
                if (geomElem.TryGetProperty("unit", out JsonElement uElem)) unit = uElem.GetString() ?? defaultUnit;

                if (geomElem.TryGetProperty("start", out JsonElement startElem) && startElem.ValueKind == JsonValueKind.Array && startElem.GetArrayLength() >= 2)
                {
                    startX = startElem[0].GetDouble();
                    startY = startElem[1].GetDouble();
                }
                if (geomElem.TryGetProperty("end", out JsonElement endElem) && endElem.ValueKind == JsonValueKind.Array && endElem.GetArrayLength() >= 2)
                {
                    endX = endElem[0].GetDouble();
                    endY = endElem[1].GetDouble();
                }
                if (geomElem.TryGetProperty("height", out JsonElement hElem) && hElem.ValueKind == JsonValueKind.Number)
                {
                    heightMeters = hElem.GetDouble();
                }
            }

            double startXFt = UnitConverter.ToFeet(startX, unit);
            double startYFt = UnitConverter.ToFeet(startY, unit);
            double endXFt = UnitConverter.ToFeet(endX, unit);
            double endYFt = UnitConverter.ToFeet(endY, unit);
            double heightFt = UnitConverter.ToFeet(heightMeters, unit);

            Wall createdWall = null;
            using (Transaction trans = new Transaction(doc, "AI Generic Create Wall"))
            {
                trans.Start();

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
                    wallType = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault();
                }

                XYZ startPt = new XYZ(startXFt, startYFt, 0.0);
                XYZ endPt = new XYZ(endXFt, endYFt, 0.0);
                Line line = Line.CreateBound(startPt, endPt);

                if (wallType != null)
                {
                    createdWall = Wall.Create(doc, line, wallType.Id, baseLevel.Id, heightFt, 0.0, false, false);
                }
                else
                {
                    createdWall = Wall.Create(doc, line, baseLevel.Id, false);
                }

                // Apply dynamic parameters if specified
                ApplyParameters(createdWall, payload);

                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"element_id\":{createdWall.Id.Value},\"category\":\"Wall\",\"length_feet\":{Math.Round(createdWall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble(), 2)}}}";
        }

        private static string CreateRoomGeneric(Document doc, JsonElement payload, Level baseLevel)
        {
            string roomName = "Room 101";
            string roomNumber = "101";

            if (payload.TryGetProperty("parameters", out JsonElement paramElem))
            {
                if (paramElem.TryGetProperty("name", out JsonElement nElem)) roomName = nElem.GetString() ?? roomName;
                if (paramElem.TryGetProperty("number", out JsonElement numElem)) roomNumber = numElem.GetString() ?? roomNumber;
            }

            Room createdRoom = null;
            using (Transaction trans = new Transaction(doc, "AI Generic Create Room"))
            {
                trans.Start();
                UV locationPoint = new UV(0, 0);
                createdRoom = doc.Create.NewRoom(baseLevel, locationPoint);
                createdRoom.Name = roomName;
                createdRoom.Number = roomNumber;
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"element_id\":{createdRoom.Id.Value},\"category\":\"Room\",\"name\":\"{createdRoom.Name}\",\"number\":\"{createdRoom.Number}\"}}";
        }

        private static void ApplyParameters(Element elem, JsonElement payload)
        {
            if (elem == null) return;
            if (payload.TryGetProperty("parameters", out JsonElement paramElem) && paramElem.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty prop in paramElem.EnumerateObject())
                {
                    Parameter p = elem.LookupParameter(prop.Name);
                    if (p != null && !p.IsReadOnly)
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String) p.Set(prop.Value.GetString());
                        else if (prop.Value.ValueKind == JsonValueKind.Number) p.Set(prop.Value.GetDouble());
                    }
                }
            }
        }

        private static string GetStringProperty(JsonElement elem, string propName, string defaultValue)
        {
            if (elem.TryGetProperty(propName, out JsonElement valElem) && valElem.ValueKind == JsonValueKind.String)
            {
                return valElem.GetString() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}

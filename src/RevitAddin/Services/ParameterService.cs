using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitAJMCPAssistant.Services
{
    public class ParameterService
    {
        public static string GetElementParameters(Document doc, long elementIdVal)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            ElementId elementId = new ElementId(elementIdVal);
            Element elem = doc.GetElement(elementId);

            if (elem == null)
            {
                return $"{{\"status\":\"error\",\"message\":\"Element with ID {elementIdVal} not found.\"}}";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{{\"status\":\"success\",\"element_id\":{elementIdVal},\"category\":\"{elem.Category?.Name}\",\"parameters\":[");

            bool first = true;
            foreach (Parameter param in elem.Parameters)
            {
                if (param.HasValue)
                {
                    if (!first) sb.Append(",");
                    first = false;

                    string valStr = param.AsValueString() ?? param.AsString() ?? param.AsDouble().ToString();
                    valStr = valStr.Replace("\"", "\\\"");

                    sb.Append($"{{\"name\":\"{param.Definition.Name}\",\"value\":\"{valStr}\"}}");
                }
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string SetElementParameter(Document doc, long elementIdVal, string paramName, string newValue)
        {
            if (doc == null) return "{\"status\":\"error\",\"message\":\"No document open.\"}";

            ElementId elementId = new ElementId(elementIdVal);
            Element elem = doc.GetElement(elementId);

            if (elem == null)
            {
                return $"{{\"status\":\"error\",\"message\":\"Element with ID {elementIdVal} not found.\"}}";
            }

            Parameter param = elem.LookupParameter(paramName);
            if (param == null || param.IsReadOnly)
            {
                return $"{{\"status\":\"error\",\"message\":\"Parameter '{paramName}' is missing or read-only on Element {elementIdVal}.\"}}";
            }

            using (Transaction trans = new Transaction(doc, $"AI Set Param {paramName}"))
            {
                trans.Start();
                switch (param.StorageType)
                {
                    case StorageType.String:
                        param.Set(newValue);
                        break;
                    case StorageType.Double:
                        if (double.TryParse(newValue, out double dVal)) param.Set(dVal);
                        break;
                    case StorageType.Integer:
                        if (int.TryParse(newValue, out int iVal)) param.Set(iVal);
                        break;
                }
                trans.Commit();
            }

            return $"{{\"status\":\"success\",\"element_id\":{elementIdVal},\"parameter\":\"{paramName}\",\"new_value\":\"{newValue}\"}}";
        }
    }
}

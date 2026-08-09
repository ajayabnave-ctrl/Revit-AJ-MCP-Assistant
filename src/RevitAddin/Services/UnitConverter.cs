using System;

namespace RevitAJMCPAssistant.Services
{
    public static class UnitConverter
    {
        public const double MetersToFeetMultiplier = 3.280839895013123;
        public const double FeetToMetersMultiplier = 0.3048;
        public const double MmToFeetMultiplier = 0.003280839895013123;

        public static double ToFeet(double value, string unit)
        {
            if (string.IsNullOrEmpty(unit)) return value * MetersToFeetMultiplier; // default meters

            switch (unit.ToLower().Trim())
            {
                case "m":
                case "meter":
                case "meters":
                    return value * MetersToFeetMultiplier;

                case "mm":
                case "millimeter":
                case "millimeters":
                    return value * MmToFeetMultiplier;

                case "ft":
                case "feet":
                case "foot":
                case "'":
                    return value;

                case "in":
                case "inch":
                case "inches":
                case "\"":
                    return value / 12.0;

                default:
                    return value * MetersToFeetMultiplier;
            }
        }

        public static double FeetToMeters(double feet)
        {
            return feet * FeetToMetersMultiplier;
        }
    }
}

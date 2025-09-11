using System.Collections.Generic;

namespace SmartValveMatcherEngine
{
    public static class ValveTypeDataProvider
    {
        /// <summary>
        /// Returns hardcoded attributes for a given valve type
        /// </summary>
        public static Dictionary<string, string> GetAttributesForValve(string valveType)
        {
            var attrs = new Dictionary<string, string>();

            switch (valveType.ToUpperInvariant())
            {
                case "BUTTERFLY_VALVE":
                    attrs["Operation"] = "Manual";
                    attrs["Design Standard"] = "EN593 / API 609 Cat. A";
                    attrs["Body Material"] = "CI : FG260";
                    attrs["Pressure Rating"] = "PN10";
                    attrs["End Connection"] = "Wafer end";
                    attrs["Additional Types"] = "Pneumatic Double acting";
                    break;

                case "BALL_VALVE":
                    attrs["Operation"] = "Manual";
                    attrs["Design Standard"] = "ISO 16135";
                    attrs["Body Material"] = "UPVC";
                    attrs["Pressure Rating"] = "150#";
                    attrs["End Connection"] = "Socket end";
                    attrs["Additional Types"] = "Manual, Manual";
                    break;

                case "MANUAL_DIAPHRAGM_VALVE":
                    attrs["Operation"] = "Manual";
                    attrs["Design Standard"] = "BS 5156";
                    attrs["Body Material"] = "CIEL";
                    attrs["Pressure Rating"] = "PN10";
                    attrs["End Connection"] = "Flange end";
                    attrs["Additional Types"] = "-";
                    break;

                case "NON_RETURN_VALVE":
                    attrs["Operation"] = "Wafer check";
                    attrs["Design Standard"] = "ISO 16137";
                    attrs["Body Material"] = "UPVC";
                    attrs["Pressure Rating"] = "PN10";
                    attrs["End Connection"] = "Socket end";
                    attrs["Additional Types"] = "Wafer check, Dual Plate check, Ball check";
                    break;

                case "GLOBE_VALVE":
                    attrs["Operation"] = "Manual";
                    attrs["Design Standard"] = "API 602";
                    attrs["Body Material"] = "CF8M";
                    attrs["Pressure Rating"] = "150#";
                    attrs["End Connection"] = "Flange end";
                    attrs["Additional Types"] = "-";
                    break;

                case "NEEDLE_VALVE":
                    attrs["Operation"] = "Manual";
                    attrs["Design Standard"] = "Manuf. Std.";
                    attrs["Body Material"] = "SS316";
                    attrs["Pressure Rating"] = "600#";
                    attrs["End Connection"] = "Screw end";
                    attrs["Additional Types"] = "-";
                    break;
            }

            return attrs;
        }
    }
}

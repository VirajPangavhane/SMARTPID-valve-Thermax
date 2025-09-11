using System.Net.Http;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;

namespace SmartValveMatcherEngine
{
    public static class SmartMarkDataLoader
    {
        public static Dictionary<string, string> LoadSmartMarkAttributes(Editor ed)
        {
            string url = "https://enaibotdevfrappe.inventivebizsol.co.in/api/v2/document/Instrumentation Files?fields=[\"*\"]&filters=[[\"name\", \"=\", \"guur80oug4\"]]";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "token cbe182e6b5c60f6:8d7e0ae7bad6198");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                var json = response.Content.ReadAsStringAsync().Result;
                var root = JObject.Parse(json);

                if (!root.TryGetValue("data", out var dataToken) || !dataToken.HasValues)
                {
                    ed.WriteMessage("\n SmartMark API returned no data.");
                    return new();
                }

                foreach (var item in dataToken)
                {
                    var rawJson = item["instrumentation_attribute_data"]?.ToString();
                    if (string.IsNullOrWhiteSpace(rawJson))
                        continue;

                    ed.WriteMessage($"\n instrumentation_attribute_data found (truncated): {rawJson.Substring(0, Math.Min(200, rawJson.Length))}...");

                    var parsed = ParseSmartMarkJson(rawJson);
                    ed.WriteMessage($"\n Loaded {parsed.Count} attributes from SmartMark.");
                    return parsed;
                }

                ed.WriteMessage("\n No valid instrumentation_attribute_data found.");
                return new();
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n Failed to fetch SmartMark data: {ex.Message}");
                return new();
            }
        }

        public static Dictionary<string, string> ParseSmartMarkJson(string rawJson)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var root = JObject.Parse(rawJson);

                // We only care about actual attribute fields inside the 4 sections
                string[] sections = { "instrumentation_data", "mechanical_data", "process_data", "electrical_data" };

                foreach (var section in sections)
                {
                    if (!root.TryGetValue(section, out var sectionNode) || sectionNode is not JObject facilities)
                        continue;

                    foreach (var facilityPair in facilities) // e.g. "undefined"
                    {
                        if (facilityPair.Value is not JObject subFacilities)
                            continue;

                        foreach (var subPair in subFacilities) // e.g. "undefined"
                        {
                            if (subPair.Value is JObject attrObj)
                            {
                                foreach (var kvp in attrObj)
                                {
                                    // ✅ Just store the attribute as-is, no facility/subfacility metadata
                                    result[kvp.Key.Trim()] = kvp.Value?.ToString()?.Trim() ?? "";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n JSON parsing failed: {ex.Message}");
            }

            return result;
        }

    }

}

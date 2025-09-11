using Newtonsoft.Json.Linq;
using SmartValveMatcherEngine.Models;

namespace SmartValveMatcherEngine;

public static class PatternLoader
{
    public static List<ValvePattern> LoadPatterns(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var root = JObject.Parse(json);

        var patterns = new List<ValvePattern>();

        foreach (var prop in root.Properties())
        {
            string valveType = prop.Name;
            var elements = prop.Value as JArray;
            if (elements == null) continue;

            var pattern = new ValvePattern { ValveType = valveType };

            foreach (var elem in elements)
            {
                string type = elem["Type"]?.ToString()?.ToUpperInvariant();

                switch (type)
                {
                    case "LINE":
                        pattern.Lines.Add(new LineData
                        {
                            X1 = elem["Start"]["X"]?.Value<double>() ?? 0,
                            Y1 = elem["Start"]["Y"]?.Value<double>() ?? 0,
                            X2 = elem["End"]["X"]?.Value<double>() ?? 0,
                            Y2 = elem["End"]["Y"]?.Value<double>() ?? 0
                        });
                        break;

                    case "CIRCLE":
                        pattern.Circles.Add(new CircleData
                        {
                            CenterX = elem["Center"]["X"]?.Value<double>() ?? 0,
                            CenterY = elem["Center"]["Y"]?.Value<double>() ?? 0,
                            Radius = elem["Radius"]?.Value<double>() ?? 0
                        });
                        break;

                    case "ARC":
                        pattern.Arcs.Add(new ArcData
                        {
                            CenterX = elem["Center"]["X"]?.Value<double>() ?? 0,
                            CenterY = elem["Center"]["Y"]?.Value<double>() ?? 0,
                            Radius = elem["Radius"]?.Value<double>() ?? 0,
                            StartAngle = elem["StartAngle"]?.Value<double>() ?? elem["Angle"]?.Value<double>() ?? 0,
                            EndAngle = elem["EndAngle"]?.Value<double>() ?? 0
                        });
                        break;

                    case "POLYLINE":
                        // Optional: implement if you're storing polyline points in future
                        break;

                    case "SOLID":
                        pattern.Solids.Add(new SolidData
                        {
                            X1 = elem["Points"]?[0]?["X"]?.Value<double>() ?? 0,
                            Y1 = elem["Points"]?[0]?["Y"]?.Value<double>() ?? 0,
                            X2 = elem["Points"]?[1]?["X"]?.Value<double>() ?? 0,
                            Y2 = elem["Points"]?[1]?["Y"]?.Value<double>() ?? 0,
                            X3 = elem["Points"]?[2]?["X"]?.Value<double>() ?? 0,
                            Y3 = elem["Points"]?[2]?["Y"]?.Value<double>() ?? 0
                        });
                        break;

                    case "HATCH":
                        pattern.Hatches.Add(new HatchData
                        {
                            PatternName = elem["PatternName"]?.ToString() ?? "",
                            CenterX = elem["Center"]?["X"]?.Value<double>() ?? 0,
                            CenterY = elem["Center"]?["Y"]?.Value<double>() ?? 0
                        });
                        break;

                    case "LEADER":
                        // Leaders don't have specific geometric data to store in patterns
                        break;



                }
            }

            patterns.Add(pattern);
        }

        return patterns;
    }
}
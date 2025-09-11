using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SmartValveMatcherEngine.Models;

namespace SmartValveMatcherEngine.Services;

public class PatternEntity
{
    public string Type { get; set; } = string.Empty;
    public double Length { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public int VertexCount { get; set; }
}

public static class PatternMatcher
{
    public static string MatchGeometryToPattern(
        List<Line> lines,
        List<Circle> circles,
        List<Arc> arcs,
        List<Polyline> polylines,
        List<Solid> solids,
        List<Hatch> hatches,
        List<Leader> leaders,
        List<ValvePattern> patterns)
    {
        var liveEntities = new List<PatternEntity>();
        liveEntities.AddRange(lines.Select(l => new PatternEntity { Type = "Line", Length = l.Length }));
        liveEntities.AddRange(circles.Select(c => new PatternEntity { Type = "Circle", Radius = c.Radius }));
        liveEntities.AddRange(arcs.Select(a => new PatternEntity { Type = "Arc", Radius = a.Radius, StartAngle = a.StartAngle, EndAngle = a.EndAngle }));
        liveEntities.AddRange(polylines.Select(p => new PatternEntity { Type = "Polyline", VertexCount = p.NumberOfVertices }));
        liveEntities.AddRange(solids.Select(_ => new PatternEntity { Type = "Solid" }));
        liveEntities.AddRange(hatches.Select(_ => new PatternEntity { Type = "Hatch" }));
        liveEntities.AddRange(leaders.Select(_ => new PatternEntity { Type = "Leader" }));

        string bestValveType = "";
        double bestScore = double.MaxValue;

        foreach (var pattern in patterns)
        {
            var patternEntities = new List<PatternEntity>();
            patternEntities.AddRange(pattern.Lines.Select(l => new PatternEntity { Type = "Line", Length = 0 })); // Patterns don't store lengths
            patternEntities.AddRange(pattern.Circles.Select(c => new PatternEntity { Type = "Circle", Radius = c.Radius }));
            patternEntities.AddRange(pattern.Arcs.Select(a => new PatternEntity { Type = "Arc", Radius = a.Radius, StartAngle = a.StartAngle, EndAngle = a.EndAngle }));
            patternEntities.AddRange(pattern.Polylines.Select(p => new PatternEntity { Type = "Polyline", VertexCount = 0 }));
            patternEntities.AddRange(pattern.Solids.Select(_ => new PatternEntity { Type = "Solid" }));
            patternEntities.AddRange(pattern.Hatches.Select(_ => new PatternEntity { Type = "Hatch" }));
            patternEntities.AddRange(pattern.Leaders.Select(_ => new PatternEntity { Type = "Leader" }));

            double score = CalculateSimilarityScore(patternEntities, liveEntities);

            if (score < bestScore)
            {
                bestScore = score;
                bestValveType = pattern.ValveType;
            }
        }

        return bestValveType;
    }

    /// <summary>
    /// Strict matching - only returns a valve type if ALL pattern elements are present in the target
    /// with exact geometric matching (no missing elements allowed)
    /// </summary>
    public static string MatchGeometryToPatternStrict(
        List<Line> lines,
        List<Circle> circles,
        List<Arc> arcs,
        List<Polyline> polylines,
        List<Solid> solids,
        List<Hatch> hatches,
        List<Leader> leaders,
        List<ValvePattern> patterns)
    {
        var liveEntities = new List<PatternEntity>();
        liveEntities.AddRange(lines.Select(l => new PatternEntity { Type = "Line", Length = l.Length }));
        liveEntities.AddRange(circles.Select(c => new PatternEntity { Type = "Circle", Radius = c.Radius }));
        liveEntities.AddRange(arcs.Select(a => new PatternEntity { Type = "Arc", Radius = a.Radius, StartAngle = a.StartAngle, EndAngle = a.EndAngle }));
        liveEntities.AddRange(polylines.Select(p => new PatternEntity { Type = "Polyline", VertexCount = p.NumberOfVertices }));
        liveEntities.AddRange(solids.Select(_ => new PatternEntity { Type = "Solid" }));
        liveEntities.AddRange(hatches.Select(_ => new PatternEntity { Type = "Hatch" }));
        liveEntities.AddRange(leaders.Select(_ => new PatternEntity { Type = "Leader" }));

        foreach (var pattern in patterns)
        {
            var patternEntities = new List<PatternEntity>();
            patternEntities.AddRange(pattern.Lines.Select(l => new PatternEntity { Type = "Line", Length = 0 })); // Patterns don't store lengths
            patternEntities.AddRange(pattern.Circles.Select(c => new PatternEntity { Type = "Circle", Radius = c.Radius }));
            patternEntities.AddRange(pattern.Arcs.Select(a => new PatternEntity { Type = "Arc", Radius = a.Radius, StartAngle = a.StartAngle, EndAngle = a.EndAngle }));
            patternEntities.AddRange(pattern.Polylines.Select(p => new PatternEntity { Type = "Polyline", VertexCount = 0 }));
            patternEntities.AddRange(pattern.Solids.Select(_ => new PatternEntity { Type = "Solid" }));
            patternEntities.AddRange(pattern.Hatches.Select(_ => new PatternEntity { Type = "Hatch" }));
            patternEntities.AddRange(pattern.Leaders.Select(_ => new PatternEntity { Type = "Leader" }));

            // Check if all pattern elements are present in the target
            if (IsPatternFullyMatched(patternEntities, liveEntities))
            {
                return pattern.ValveType;
            }
        }

        return ""; // No complete match found
    }

    /// <summary>
    /// Lower score = better match
    /// </summary>
    private static double CalculateSimilarityScore(List<PatternEntity> pattern, List<PatternEntity> target)
    {
        var patternCounts = pattern.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
        var targetCounts = target.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());

        double score = 0;

        // Count-based scoring
        foreach (var type in patternCounts.Keys.Union(targetCounts.Keys))
        {
            int patCount = patternCounts.ContainsKey(type) ? patternCounts[type] : 0;
            int tgtCount = targetCounts.ContainsKey(type) ? targetCounts[type] : 0;
            score += Math.Abs(patCount - tgtCount) * 10; // Weight count differences more heavily
        }

        // Additional geometric scoring for entities with specific properties
        foreach (var targetType in targetCounts.Keys)
        {
            var patternEntitiesOfType = pattern.Where(e => e.Type == targetType).ToList();
            var targetEntitiesOfType = target.Where(e => e.Type == targetType).ToList();

            int minCount = Math.Min(patternEntitiesOfType.Count, targetEntitiesOfType.Count);
            
            for (int i = 0; i < minCount; i++)
            {
                var patternEntity = patternEntitiesOfType[i];
                var targetEntity = targetEntitiesOfType[i];
                
                // Score based on geometric properties
                if (targetEntity.Type == "Circle" && patternEntity.Radius > 0)
                {
                    double radiusDiff = Math.Abs(targetEntity.Radius - patternEntity.Radius);
                    score += radiusDiff / 10.0; // Normalize radius differences
                }
                
                if (targetEntity.Type == "Arc" && patternEntity.Radius > 0)
                {
                    double radiusDiff = Math.Abs(targetEntity.Radius - patternEntity.Radius);
                    double angleDiff = Math.Abs((targetEntity.EndAngle - targetEntity.StartAngle) - 
                                              (patternEntity.EndAngle - patternEntity.StartAngle));
                    score += (radiusDiff / 10.0) + (angleDiff / 10.0);
                }
                
                if (targetEntity.Type == "Line" && targetEntity.Length > 0)
                {
                    // For lines, we don't have pattern lengths, so we don't score them
                }
                
                if (targetEntity.Type == "Polyline")
                {
                    int vertexDiff = Math.Abs(targetEntity.VertexCount - patternEntity.VertexCount);
                    score += vertexDiff * 2; // Weight vertex count differences
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Checks if all pattern elements are present in the target entities
    /// </summary>
    private static bool IsPatternFullyMatched(List<PatternEntity> pattern, List<PatternEntity> target)
    {
        var patternCounts = pattern.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
        var targetCounts = target.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());

        // Check if we have at least the required number of each entity type
        foreach (var kvp in patternCounts)
        {
            string type = kvp.Key;
            int requiredCount = kvp.Value;
            
            int availableCount = targetCounts.ContainsKey(type) ? targetCounts[type] : 0;
            
            // If we don't have enough of any entity type, return false
            if (availableCount < requiredCount)
            {
                return false;
            }
        }

        // Additional geometric checks for specific entity types
            foreach (var patternEntity in pattern)
            {
                bool foundMatch = false;
                
                foreach (var targetEntity in target.Where(e => e.Type == patternEntity.Type))
                {
                    // For entities with specific geometric properties, check if they match
                    if (patternEntity.Type == "Circle" && patternEntity.Radius > 0)
                    {
                        // Allow some tolerance for radius matching
                        double radiusDiff = Math.Abs(targetEntity.Radius - patternEntity.Radius);
                        if (radiusDiff <= 0.5) // 0.5mm tolerance (more strict)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    else if (patternEntity.Type == "Arc" && patternEntity.Radius > 0)
                    {
                        // Allow some tolerance for arc matching
                        double radiusDiff = Math.Abs(targetEntity.Radius - patternEntity.Radius);
                        double angleDiff = Math.Abs((targetEntity.EndAngle - targetEntity.StartAngle) - 
                                                  (patternEntity.EndAngle - patternEntity.StartAngle));
                        if (radiusDiff <= 0.5 && angleDiff <= 0.05) // 0.5mm radius tolerance, 0.05 radian angle tolerance (more strict)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    else if (patternEntity.Type == "Polyline")
                    {
                        // For polylines, check vertex count with some tolerance
                        int vertexDiff = Math.Abs(targetEntity.VertexCount - patternEntity.VertexCount);
                        if (vertexDiff <= 0) // No difference allowed (more strict)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    else
                    {
                        // For other entity types (Line, Solid, Hatch, Leader), just matching the type is enough
                        foundMatch = true;
                        break;
                    }
                }
                
                // If we couldn't find a match for this pattern entity, return false
                if (!foundMatch)
                {
                    return false;
                }
            }

        return true;
    }
}
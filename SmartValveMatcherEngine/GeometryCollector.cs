using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;

namespace SmartValveMatcherEngine;

public static class GeometryCollector
{
    /// <summary>
    /// Collects all entities in modelspace connected near a given point (typically a tag).
    /// </summary>
    public static List<Entity> CollectConnectedEntities(BlockTableRecord ms, Point3d start, Transaction tr, double proximity = 10.0)
    {
        var visited = new HashSet<ObjectId>();
        var queue = new Queue<Entity>();
        var result = new List<Entity>();

        double maxLengthThreshold = 5.0; // Reject lines longer than this

        // Start by finding all nearby valid entities
        foreach (ObjectId id in ms)
        {
            var obj = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (obj == null || obj.IsErased || obj.IsDisposed || !obj.Bounds.HasValue)
                continue;

            // ❗ Check distance first, then apply IsValidGeometry
            if (start.DistanceTo(GetCenter(obj.Bounds.Value)) <= proximity && IsValidGeometry(obj))
            {
                queue.Enqueue(obj);
                visited.Add(id);
            }

        }

        // Breadth-first search with proximity-based growth
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (ObjectId id in ms)
            {
                if (visited.Contains(id)) continue;

                var next = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (!IsValidGeometry(next)) continue;

                double dist = GetCenter(current.Bounds.Value).DistanceTo(GetCenter(next.Bounds.Value));
                if (dist < proximity)
                {
                    queue.Enqueue(next);
                    visited.Add(id);
                }
            }
        }

        return result;

        // Local helper to validate geometry type and constraints
        bool IsValidGeometry(Entity ent)
        {
            if (ent == null || ent.IsErased || ent.IsDisposed || !ent.Bounds.HasValue)
                return false;

            // ✅ allow Line, Circle, Arc, Solid
            if (!(ent is Line || ent is Circle || ent is Arc || ent is Solid || ent is Hatch))
                return false;


            if (ent is Line line && line.Length > maxLengthThreshold)
                return false;

            if (ent is Polyline pl && pl.Layer == "AREA_ZONE")
                return false;

            return true;
        }

    }



    public static List<Entity> CollectEntitiesNearPoint(BlockTableRecord ms, Point3d position, double radius, Transaction tr)
    {
        var nearby = new List<Entity>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null || ent.IsErased) continue;

            var ext = ent.GeometricExtents;
            if (IsWithinRadius(ext, position, radius))
            {
                nearby.Add(ent);
            }
        }
        return nearby;
    }

    private static bool IsWithinRadius(Extents3d ext, Point3d center, double radius)
    {
        return
            center.X >= ext.MinPoint.X - radius && center.X <= ext.MaxPoint.X + radius &&
            center.Y >= ext.MinPoint.Y - radius && center.Y <= ext.MaxPoint.Y + radius;
    }


    private static Point3d GetCenter(Extents3d ext)
    {
        return new Point3d(
            (ext.MinPoint.X + ext.MaxPoint.X) / 2,
            (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
            (ext.MinPoint.Z + ext.MaxPoint.Z) / 2
        );
    }
}

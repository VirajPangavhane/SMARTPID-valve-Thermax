using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartValveMatcherEngine
{
    public static class BlockBuilder
    {
        private const double ConnectionTolerance = 0.1; // mm

        public static void CreateValveBlock(
            BlockTable bt,
            BlockTableRecord ms,
            Transaction tr,
            DBText tagText,
            string valveTag,
            string valveType,
            List<Line> lines,
            List<Circle> circles,
            List<Arc> arcs,
            List<Solid> solids,
            List<Hatch> hatches,
            List<Polyline> polylines,
            List<Leader> leaders,
            List<DBText> extras,
            Dictionary<string, string>? extraAttributes,
            ref int createdCount
        )
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Unique block name
            string blockName = $"VALVE_{valveType}_{Guid.NewGuid().ToString("N")[..8]}";
            var blockDef = new BlockTableRecord { Name = blockName, Origin = Point3d.Origin };

            bt.UpgradeOpen();
            bt.Add(blockDef);
            tr.AddNewlyCreatedDBObject(blockDef, true);

            var transformToOrigin = Matrix3d.Displacement(tagText.Position.GetAsVector().Negate());

            // === Collect All Entities ===
            var allEntities = lines.Cast<Entity>()
                .Concat(circles)
                .Concat(arcs)
                .Concat(polylines)
                .Concat(solids)
                .Concat(hatches)
                .Concat(leaders)
                .Concat(extras)
                .ToList();

            // Keep only largest connected cluster
            var filteredEntities = GetLargestConnectedCluster(allEntities, ConnectionTolerance);

            foreach (var ent in filteredEntities)
            {
                if (ent is Polyline pl && pl.NumberOfVertices >= 4 && pl.Closed)
                    continue; // skip zone boundaries

                var copy = (Entity)ent.Clone();
                copy.TransformBy(transformToOrigin);
                blockDef.AppendEntity(copy);
                tr.AddNewlyCreatedDBObject(copy, true);
            }

            // === Add Tag Text ===
            if (tagText != null)
            {
                var tagClone = (DBText)tagText.Clone();
                tagClone.TransformBy(transformToOrigin);
                blockDef.AppendEntity(tagClone);
                tr.AddNewlyCreatedDBObject(tagClone, true);
            }

            // === Add Extra Texts ===
            foreach (var extra in extras ?? Enumerable.Empty<DBText>())
            {
                var copy = (DBText)extra.Clone();
                copy.TransformBy(transformToOrigin);
                blockDef.AppendEntity(copy);
                tr.AddNewlyCreatedDBObject(copy, true);
            }

            // === Attributes ===
            var attDefs = new List<AttributeDefinition>();

            void AddAtt(string tag, string val, int row)
            {
                var pos = new Point3d(0, -row * 3.0, 0);
                var def = new AttributeDefinition
                {
                    Tag = tag,
                    TextString = val,
                    Position = pos,
                    Height = 2.5,
                    Justify = AttachmentPoint.MiddleCenter,
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    Invisible = true
                };
                attDefs.Add(def);
                blockDef.AppendEntity(def);
                tr.AddNewlyCreatedDBObject(def, true);
            }

            AddAtt("VALVE_TAG", valveTag, 0);
            AddAtt("VALVE_TYPE", valveType, 1);

            if (extraAttributes != null)
            {
                int i = 2;
                foreach (var kvp in extraAttributes)
                {
                    string key = kvp.Key?.Trim().ToUpper() ?? "";
                    string value = kvp.Value?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(key))
                        AddAtt(key, value, i++);
                }
            }

            // === Insert Block Reference ===
            var blkRef = new BlockReference(tagText.Position, blockDef.ObjectId);
            ms.AppendEntity(blkRef);
            tr.AddNewlyCreatedDBObject(blkRef, true);

            foreach (var def in attDefs)
            {
                var attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(def, blkRef.BlockTransform);
                attRef.TextString = def.TextString;
                attRef.Tag = def.Tag;
                attRef.Position = def.Position;
                blkRef.AttributeCollection.AppendAttribute(attRef);
                tr.AddNewlyCreatedDBObject(attRef, true);
            }

            createdCount++;
            ed.WriteMessage($"\n Block inserted: {blockName} at {tagText.Position}");
        }

        // === Helpers ===
        private static List<Entity> GetLargestConnectedCluster(List<Entity> entities, double tolerance)
        {
            var clusters = new List<List<Entity>>();
            var visited = new HashSet<Entity>();

            foreach (var entity in entities)
            {
                if (visited.Contains(entity))
                    continue;

                var cluster = new List<Entity>();
                var queue = new Queue<Entity>();
                queue.Enqueue(entity);
                visited.Add(entity);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    cluster.Add(current);

                    foreach (var other in entities)
                    {
                        if (!visited.Contains(other) &&
                            EntitiesAreClose(current, other, tolerance))
                        {
                            visited.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }

                clusters.Add(cluster);
            }

            return clusters.OrderByDescending(c => c.Count).FirstOrDefault() ?? new List<Entity>();
        }

        private static bool EntitiesAreClose(Entity e1, Entity e2, double tolerance)
        {
            try
            {
                var bb1 = e1.GeometricExtents;
                var bb2 = e2.GeometricExtents;

                double dx = Math.Max(0, Math.Max(bb1.MinPoint.X - bb2.MaxPoint.X, bb2.MinPoint.X - bb1.MaxPoint.X));
                double dy = Math.Max(0, Math.Max(bb1.MinPoint.Y - bb2.MaxPoint.Y, bb2.MinPoint.Y - bb1.MaxPoint.Y));
                double dist = Math.Sqrt(dx * dx + dy * dy);

                return dist <= tolerance;
            }
            catch
            {
                return false;
            }
        }
    }
}

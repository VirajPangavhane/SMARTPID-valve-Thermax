using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartValveMatcherEngine
{
    public static class BlockExploder
    {
        private static readonly string[] ValvePrefixes = new[]
        {
            "VGAT", "VGLO", "VNRV", "VBAL", "CIRC", "VNDL", "VSFANG", "ARROW", "VFM",
            "A$C0F3F5412", "A$C0FBB6139", "A$C0FE459DB", "A$C12373155", "A$C1BAC7DA9",
            "A$C25474332", "A$C2d2bd378", "A$C4D9B5FFC", "A$C70DC7051", "A$C720B3F21", "A$C72B120EA",
            "GLOBE", "PISTEN VLV", "VG", "VGATF", "VGAT1", "vglof1", "VSF", "VSF1", "VC", "A$C43754A24",
            "A$C21c2fcb2", "A$C244e2b3a", "A$C5E4A6316", "Ball", "BUTTERFLY VALVE", "Diaphragm",
            "FO-BF-VALVE-DA", "GAUAGE COCK", "Nrv", "WW_J_MANU VALVE1","A$C68DA09D3",


            "A$C290A4BE8","A$C34E118AB","A$C70D47027","A$C7351586E","GV1",
        };

        public static void ExplodeValveBlocks(BlockTableRecord ms, Transaction tr, Editor ed)
        {
            var toExplode = new List<BlockReference>();

            foreach (ObjectId id in ms)
            {
                if (!id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                    continue;

                var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (br == null || br.IsErased || br.IsDisposed)
                    continue;

                string name = GetBlockName(br, tr);

                if (ValvePrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    toExplode.Add(br);
                }
            }

            ed.WriteMessage($"\n Found {toExplode.Count} valve block(s) to explode...");

            int explodedCount = 0;

            foreach (var br in toExplode)
            {
                try
                {
                    RecursiveExplodeIntoModelspace(br, ms, tr, ed);
                    explodedCount++;
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n Failed to explode block {br.Handle}: {ex.Message}");
                }
            }

            ed.WriteMessage($"\n Exploded {explodedCount} block(s).");
        }

        private static string GetBlockName(BlockReference br, Transaction tr)
        {
            var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
            return btr.Name;
        }

        private static void RecursiveExplodeIntoModelspace(BlockReference br, BlockTableRecord ms, Transaction tr, Editor ed)
        {
            var exploded = new DBObjectCollection();

            try
            {
                br.Explode(exploded);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage($"\n Cannot explode block {br.Handle}: {ex.Message}");
                return;
            }

            foreach (Entity ent in exploded)
            {
                if (ent is BlockReference nestedBr)
                {
                    RecursiveExplodeIntoModelspace(nestedBr, ms, tr, ed);
                }
                else
                {
                    ms.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }
            }

            // ✅ Only upgrade if not already write-enabled
            if (!br.IsWriteEnabled)
            {
                br.UpgradeOpen();
            }

            br.Erase(); // Remove the original block reference
        }
    }
}

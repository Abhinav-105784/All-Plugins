using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
[assembly: CommandClass(typeof(TopSurfaceOfSolide.TopSurface))]
namespace TopSurfaceOfSolide
{
    public class TopSurface
    {
        [CommandMethod("GetSurface")]
        public void TopExtrude()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("Select solids");
            PromptSelectionResult solidsSelected = ed.GetSelection();
            if (solidsSelected.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n Wrong Selection re-run command");
                return;
            }
          
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;              
                try
                {
                  
                        SelectionSet set = solidsSelected.Value;
                        foreach (SelectedObject obj in set)
                        {
                            if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Solid3d))))
                            {
                                Solid3d solids = tr.GetObject(obj.ObjectId, OpenMode.ForWrite) as Solid3d;
                                Solid3d solidsClone = solids.Clone() as Solid3d;
                                btr.AppendEntity(solidsClone);
                                tr.AddNewlyCreatedDBObject(solidsClone, true);
                                DBObjectCollection explodedClones = new DBObjectCollection();
                                solidsClone.Explode(explodedClones);
                                foreach (Entity ent in explodedClones)
                                {
                                    if (ent is Surface)
                                    {
                                        btr.AppendEntity(ent);
                                        tr.AddNewlyCreatedDBObject(ent, true);
                                        ent.Layer = "0";
                                    }
                                }
                                solidsClone.Erase();
                            }
                        }                                    
                }
                catch (System.Exception exp)
                {
                    ed.WriteMessage($"{exp}");
                }
                tr.Commit();
            }
        }

    }
}

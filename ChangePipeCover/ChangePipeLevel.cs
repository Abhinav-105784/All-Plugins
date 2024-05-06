
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System.Runtime.InteropServices;

[assembly:CommandClass(typeof(Change_Pipe_Cover.ChangePipeLevel))]
namespace Change_Pipe_Cover
{
    public class ChangePipeLevel
    {
        [CommandMethod("MOVEPIPECOVER")]
        public void Move()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            editor.WriteMessage("\nSelect the Pipes to move");
            PromptSelectionResult selectPipes = editor.GetSelection();
            if(selectPipes.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nPipes Not selected Properly, re-try by running the command again");
                return;
            }

            PromptEntityResult selectSurface = editor.GetEntity("\n Select the surface of pipe network");
            if(selectSurface.Status!=PromptStatus.OK)
            {
                editor.WriteMessage("\nSurface not selected properly, re-try by running the command again");
                return;
            }
            PromptDoubleOptions coverGive = new PromptDoubleOptions($"\nGive cover Value")
            {
                AllowNegative = false,
                AllowZero = true,
                AllowNone = false,
            };
            PromptDoubleResult coverGiven = editor.GetDouble(coverGive);

            SelectionSet pipesCounts = selectPipes.Value;
            using (Transaction tr = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                TinSurface surfaceSelected = tr.GetObject(selectSurface.ObjectId, OpenMode.ForRead) as TinSurface;
                foreach (SelectedObject obj in pipesCounts)
                {
                    if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Pipe))))
                    {
                        Pipe pipeSelected = tr.GetObject(obj.ObjectId, OpenMode.ForWrite) as Pipe;
                        pipeSelected.RefSurfaceId = surfaceSelected.ObjectId;
                        double elevMinPoint = surfaceSelected.FindElevationAtXY(pipeSelected.StartPoint.X, pipeSelected.StartPoint.Y);
                        editor.WriteMessage($"\n Surface Elevation at {pipeSelected.Name} startPoint is {elevMinPoint:N3}");
                        double elevMaxPoint = surfaceSelected.FindElevationAtXY(pipeSelected.EndPoint.X, pipeSelected.EndPoint.Y);
                        editor.WriteMessage($"\n Surface Elevation at {pipeSelected.Name} EndPoint is {elevMaxPoint:N3}");
                        double startPoint = elevMinPoint - coverGiven.Value;
                        double endPoint = elevMaxPoint - coverGiven.Value;
                        Point3d newStartPoint = new Point3d(pipeSelected.StartPoint.X, pipeSelected.StartPoint.Y, startPoint - (pipeSelected.OuterHeight / 2));
                        Point3d newEndPoint = new Point3d(pipeSelected.EndPoint.X, pipeSelected.EndPoint.Y, endPoint - (pipeSelected.OuterHeight / 2));
                        pipeSelected.StartPoint = newStartPoint;
                        pipeSelected.EndPoint = newEndPoint;
                        editor.WriteMessage($"\n {pipeSelected.Name} StartPoint elevation :{pipeSelected.StartPoint.Z} ");
                        editor.WriteMessage($"\n{pipeSelected.Name} EndPoint elevation : {pipeSelected.EndPoint.Z}");
                        if (pipeSelected.StartStructureId != ObjectId.Null || pipeSelected.EndStructureId != ObjectId.Null)
                        {
                            ObjectId strId = pipeSelected.StartStructureId;
                            Structure str = tr.GetObject(strId, OpenMode.ForWrite) as Structure;
                            str.RefSurfaceId = surfaceSelected.ObjectId;
                            double strElevation = surfaceSelected.FindElevationAtXY(str.Position.X, str.Position.Y);
                            str.Position = new Point3d(str.Position.X, str.Position.Y, strElevation);
                            str.ResizeByPipeDepths();
                            ObjectId strId2 = pipeSelected.EndStructureId;
                            Structure str2 = tr.GetObject(strId2, OpenMode.ForWrite) as Structure;
                            str2.RefSurfaceId = surfaceSelected.ObjectId;
                            double strElevation2 = surfaceSelected.FindElevationAtXY(str2.Position.X, str2.Position.Y);
                            str2.Position = new Point3d(str2.Position.X, str2.Position.Y, strElevation2);
                            str2.ResizeByPipeDepths();
                        }                    
                    }
                   
                }         
                tr.Commit();
            }
        }
    }
}

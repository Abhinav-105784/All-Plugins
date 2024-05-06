
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

[assembly: CommandClass(typeof(GetPipeInvertLevelandStructureTopLevel.InsertElevation))]
namespace GetPipeInvertLevelandStructureTopLevel
{
    public class InsertElevation
    {
        [CommandMethod("GetLEVELSIN-TOP")]

        public void GetElevations()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\n Select Pipes");

            PromptSelectionResult blockSelected = editor.GetSelection();
            if (blockSelected.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Selected wrong Objects. Please restart the command to try again");
                return;
            }

            SelectionSet set = blockSelected.Value;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                CogoPointCollection cogoPoints = cDoc.CogoPoints;
                foreach (SelectedObject obj in set)
                {
                    if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Structure))))
                    {

                        Structure str = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Structure;
                        string name = $"TopLevel {str.Name}";
                        string modifiedName = $"{name}";
                        double centreX = str.GeometricExtents.MinPoint.X + ((str.GeometricExtents.MaxPoint.X - str.GeometricExtents.MinPoint.X) / 2);
                        double centreY = str.GeometricExtents.MinPoint.Y + ((str.GeometricExtents.MaxPoint.Y - str.GeometricExtents.MinPoint.Y) / 2);
                        double MaxPointZ = str.GeometricExtents.MaxPoint.Z;
                        Point3d point3D = new Point3d(centreX, centreY, MaxPointZ);
                        ObjectId cgp = cogoPoints.Add(point3D, true);
                        CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint.PointName = modifiedName;

                    }

                   else if (obj.ObjectId.ObjectClass.DxfName.Contains("3DSOLID"))
                   {

                        Solid3d solid = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Solid3d;
                        string name = $"TopLevel {solid}";
                        string modifiedName = $"{name}";
                        double minX = solid.GeometricExtents.MinPoint.X;
                        double minY = solid.GeometricExtents.MinPoint.Y;
                        double maxZ = solid.GeometricExtents.MaxPoint.Z;
                        Point3d point3D = new Point3d(minX, minY, maxZ);
                        ObjectId cgp = cogoPoints.Add(point3D, true);
                        CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint.PointName = modifiedName;

                   }
                   else if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Pipe))))
                   {

                        Pipe pipeSelected = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Pipe;
                        string name = $"InverLevelof{pipeSelected.Name}";
                        string modifiedName = $"{name}";
                        double minX = pipeSelected.StartPoint.X;
                        double minY = pipeSelected.StartPoint.Y;
                        double invertZ = pipeSelected.StartPoint.Z - (pipeSelected.InnerDiameterOrWidth / 2);
                        Point3d point3D = new Point3d(minX, minY, invertZ);
                        ObjectId cgp = cogoPoints.Add(point3D, true);
                        CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint.PointName = modifiedName;
                        double minEndX = pipeSelected.EndPoint.X;
                        double minEndY = pipeSelected.EndPoint.Y;
                        double invertEndZ = pipeSelected.EndPoint.Z - (pipeSelected.InnerDiameterOrWidth / 2);
                        Point3d point3D1 = new Point3d(minEndX, minEndY, invertEndZ);
                        ObjectId cgp1 = cogoPoints.Add(point3D1, true);
                        CogoPoint cogoPoint1 = cgp1.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint1.PointName = $"IntvertLevelatEndPointof{pipeSelected.Name}";
                   }
                }
                transaction.Commit();
            }
        }
    }
}

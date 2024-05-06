using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System.Reflection;

[assembly: CommandClass(typeof(InsertCogoPoints.InsertatBase))]

namespace InsertCogoPoints
{
    public class InsertatBase
    {
        [CommandMethod("InsertCogo")]
        public void InsertCogo()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\n Select a 3D solid or structure");

            PromptSelectionResult solidSelected = editor.GetSelection();
            if (solidSelected.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Selected wrong Objects. Please restart the command to try again");
                return;
            }
            SelectionSet set = solidSelected.Value;
           
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
              
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                CogoPointCollection cogoPoints = cDoc.CogoPoints;
                PromptStringOptions namethePoint = new PromptStringOptions("\n Give name to the point.");
                PromptResult nameResult = editor.GetString(namethePoint);
                string nameOfPoint = nameResult.StringResult;
                int digit = 1;
                PromptStringOptions description = new PromptStringOptions("Write the point description");
                PromptResult descriptionFinal = editor.GetString(description);
                string disc = descriptionFinal.StringResult;
                foreach (SelectedObject obj in set)
                {
                    if(obj.ObjectId.ObjectClass.DxfName.Contains("3DSOLID"))
                    {
                        string modifiedName = $"{nameResult}-{digit}";
                        digit++;
                        Solid3d solid = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Solid3d;
                        double centreX = solid.GeometricExtents.MinPoint.X + ((solid.GeometricExtents.MaxPoint.X - solid.GeometricExtents.MinPoint.X) / 2);
                        double centreY = solid.GeometricExtents.MinPoint.Y + ((solid.GeometricExtents.MaxPoint.Y - solid.GeometricExtents.MinPoint.Y) / 2);
                        double centreZ = solid.GeometricExtents.MinPoint.Z;
                        Point3d point3D = new Point3d(centreX, centreY, centreZ);
                        ObjectId cgp = cogoPoints.Add(point3D,disc,true);
                        CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint.PointName = modifiedName;

                    }
                    else if(obj.ObjectId.ObjectClass.DxfName.Contains("STRUCTURE"))
                    {
                        string modifiedName = $"{nameOfPoint}-{digit}";
                        digit++;
                        Structure str = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Structure; 
                        double centreX = str.GeometricExtents.MinPoint.X + ((str.GeometricExtents.MaxPoint.X - str.GeometricExtents.MinPoint.X)/2);
                        double centreY = str.GeometricExtents.MinPoint.Y + ((str.GeometricExtents.MaxPoint.Y - str.GeometricExtents.MinPoint.Y) / 2);
                        double centreZ = str.GeometricExtents.MinPoint.Z;
                        Point3d point3D = new Point3d(centreX, centreY, centreZ);
                        ObjectId cgp = cogoPoints.Add(point3D, disc , true);
                        CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                        cogoPoint.PointName = modifiedName;

                    }

                }
                transaction.Commit();
            }
        }
    }
}

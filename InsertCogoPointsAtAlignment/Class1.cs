using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[assembly:CommandClass(typeof(InsertCogoAtAlignment.InsertPointsSop))]
namespace InsertCogoAtAlignment
{
    public class InsertPointsSop
    {
        [CommandMethod("InsertSOP")]

        public void Insert()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\n Select Alignments");

            PromptSelectionResult blockSelected = editor.GetSelection();
            if (blockSelected.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Selected wrong Objects. Please restart the command to try again");
                return;
            }
            PromptDoubleOptions intervalInput = new PromptDoubleOptions("\nEnter the interval of stations")
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = false,
            };
            PromptDoubleResult intervals = editor.GetDouble(intervalInput);
            if (intervals.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Wrong Input");
                return;
            }
            SelectionSet set = blockSelected.Value;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                CogoPointCollection cogoPoints = cDoc.CogoPoints;
                foreach (SelectedObject obj in set)
                {
                   
                    if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Alignment))))
                    {
                        Alignment str = transaction.GetObject(obj.ObjectId, OpenMode.ForRead) as Alignment;
                        for (double j = 0; j < str.Length; j += intervals.Value)
                        {
                            Point3d point3D = str.GetPointAtDist(j);
                            ObjectId cgp = cogoPoints.Add(point3D, true);
                            CogoPoint cogoPoint = cgp.GetObject(OpenMode.ForWrite) as CogoPoint;
                            string modifiedName = $"Level : {str.Name}-{j}";
                            cogoPoint.PointName = modifiedName;
                        }
                    }
                    
                }
                transaction.Commit();
            }

        }
    }
}

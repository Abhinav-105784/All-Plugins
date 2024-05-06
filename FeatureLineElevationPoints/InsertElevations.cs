
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Net.Http.Headers;

[assembly: CommandClass(typeof(FeatureLineElevations.InsertElevations))]
namespace FeatureLineElevations
{
    public class InsertElevations
    {
        [CommandMethod("GiveElevations")]

        public void ElevationAtPoints()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\n Select Feature Lines");
            PromptSelectionResult selectedFls = editor.GetSelection();
            if (selectedFls.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection. Re-Run the Command");
                return;
            }
            SelectionSet set = selectedFls.Value;
            
           
            editor.WriteMessage("\nGive interval of Elevations Points.");
            PromptDoubleResult intervals = editor.GetDouble("\nGive Interval");
            


            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                foreach (SelectedObject obj in set)
                {
                    if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(FeatureLine)))) 
                    {
                       
                        FeatureLine fl = transaction.GetObject(obj.ObjectId, OpenMode.ForWrite) as FeatureLine;
                        if (intervals.Status != PromptStatus.OK && intervals.Value%fl.Length3D!=0 && intervals.Value ==0)
                        {

                            editor.WriteMessage($"Wrong selection of internval, Give interval between {0.01} to {fl.Length2D/2} ");
                            return;
                        }
                    for(double i = 0; i <= fl.Length2D; i+=intervals.Value)
                        {
                            Point3d internalDivisions = fl.GetPointAtDist(i);
                            editor.WriteMessage($"\n points{internalDivisions}");
                            Point3dCollection flpC = fl.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
                            
                            if(!flpC.Contains(internalDivisions))
                            {
                               
                                fl.InsertElevationPoint(internalDivisions);
                                
                            }
                          
                        }
                     
                     
                        
                       
                      
                       

                    }
                    
                    
                }
                transaction.Commit();
            }
        }
    }
}

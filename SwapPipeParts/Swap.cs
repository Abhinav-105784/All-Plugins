
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System;
using System.Linq;

[assembly: CommandClass(typeof(SwapParts.Swap))]
namespace SwapParts
{
    public class Swap
    {
        [CommandMethod("SwapAllParts")]
        public void SwapParts()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\n Select the structures to replace");

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
                PartsListCollection partsListCollection = cDoc.Styles.PartsListSet;
                foreach (ObjectId part in partsListCollection)
                {
                    PartsList partsList = transaction.GetObject(part, OpenMode.ForRead) as PartsList;
                    {
                        editor.WriteMessage($"\nPARTS LISTS: {partsList.Name}");
                    }
                }
                    PromptStringOptions partsListName = new PromptStringOptions("\nSelect the Parts List from which you want to insert the new part")
                    {
                        AllowSpaces = true,
                    };
                    PromptResult partlistName1 = editor.GetString(partsListName);
                    string partsListName2 = partlistName1.StringResult;
                    ObjectId PartsList1 = cDoc.Styles.PartsListSet[partsListName2];
                    PartsList partsList2 = transaction.GetObject(PartsList1, OpenMode.ForWrite) as PartsList;
                    ObjectIdCollection partFamilyCollection = partsList2.GetPartFamilyIdsByDomain(DomainType.Structure);
                    foreach (ObjectId partStr in partFamilyCollection)
                    {
                        PartFamily partFamily1 = transaction.GetObject(partStr, OpenMode.ForWrite) as PartFamily;
                        if (partFamily1.Domain == DomainType.Structure)
                        {
                         editor.WriteMessage($"\nStructures: {partFamily1.Name}");
                        }
                    } 
                PromptStringOptions partFamilyName = new PromptStringOptions("\nSelect the structure you want to swap these structures with")
                {
                    AllowSpaces = true,
                };
                PromptResult partFamilySwapping = editor.GetString(partFamilyName);
                string swapper = partFamilySwapping.StringResult;
                ObjectId structureId = partsList2[swapper];
                Structure structure = transaction.GetObject(structureId, OpenMode.ForWrite) as Structure;
                PartFamily str1 = transaction.GetObject(structureId, OpenMode.ForWrite) as PartFamily;
                ObjectId psizestr = str1[0];
                

                foreach (SelectedObject obj in set)
                {
                    if (obj.ObjectId.ObjectClass.DxfName.Contains("STRUCTURE"))
                    {
                        Structure str = transaction.GetObject(obj.ObjectId, OpenMode.ForWrite) as Structure;
                        
                            string[] pipeNames = str.GetConnectedPipeNames();
                            foreach (string count in pipeNames)
                            {
                                editor.WriteMessage($"\n{count} ");
                            }
                        if (pipeNames.Length > 1)
                        {
                            try
                            {
                                ObjectId pipeId = str.get_ConnectedPipe(1);
                                Pipe pipe = transaction.GetObject(pipeId, OpenMode.ForRead) as Pipe;
                                Point3d pt1 = pipe.StartPoint;
                                Point3d pt2 = pipe.EndPoint;
                                double angle = Vector3d.XAxis.GetAngleTo(pt1.GetVectorTo(pt2), Vector3d.ZAxis);
                                str.Rotation = angle;
                                editor.WriteMessage($"\n 2 pipes connected angle of Rotation : {angle:N3}");
                            }
                            catch (System.Exception exx)
                            {
                                editor.WriteMessage($"\n {exx}");
                            }
                        }
                        else
                        {
                            
                            try
                            {
                                ObjectId pipedId = str.get_ConnectedPipe(0);
                                Pipe pipe = transaction.GetObject(pipedId, OpenMode.ForRead) as Pipe;
                               
                                {
                                    Point3d pt1 = pipe.StartPoint;
                                    Point3d pt2 = pipe.EndPoint;
                                    double angle = Vector3d.XAxis.GetAngleTo(pt1.GetVectorTo(pt2), Vector3d.ZAxis);
                                    str.Rotation = angle;
                                    editor.WriteMessage($"\n 1 pipe connected angle of Rotation : {angle:N3}");
                                }

                            }
                            catch (System.Exception ex)
                            {
                                editor.WriteMessage($"{ex}");
                            }
                        }
                        
                        str.SwapPartFamilyAndSize(structureId, psizestr);

                    }
                }
                transaction.Commit();
            }
        }
    }
}

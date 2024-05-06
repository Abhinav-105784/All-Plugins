using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq.Expressions;

[assembly:CommandClass(typeof(markups_SW.Marks))]

namespace markups_SW
{
    public class Marks
    {
        [CommandMethod("Markups")]
        public void MarksMake()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            //editor.WriteMessage("\nSelect the pipes");

           /* PromptSelectionResult selectedPipes = editor.GetSelection();
            if(selectedPipes.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection, re-run the command to re-try");
                return;
            }*/
            PromptEntityResult selectedPipes = editor.GetEntity("Select the pipe");
            if (selectedPipes.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nWrong Selection, re-run the command to re-try");
                return;
            }
            PromptDoubleOptions setHeight = new PromptDoubleOptions("\n Set Text Height")
            {
                AllowNegative = false,
                AllowZero = false,
                AllowNone = false
            };

            PromptDoubleResult height = editor.GetDouble(setHeight);
            if (height.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\n Wrong Input Re-Run command to try again");
                return;
            }
            //SelectionSet set = selectedPipes.Value;
            using(Transaction tr = database.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(database.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                Entity pipeSelected = tr.GetObject(selectedPipes.ObjectId, OpenMode.ForWrite) as Entity;
                string layer = pipeSelected.Layer;
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.LayerName, layer)
                };
                SelectionFilter filter = new SelectionFilter(filterList);
                PromptSelectionResult selRes = editor.SelectAll(filter);
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet selectedEntities = selRes.Value;

                    foreach (SelectedObject obj in selectedEntities)
                    {
                        if (obj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Polyline))))
                        {
                            Polyline pl = tr.GetObject(obj.ObjectId, OpenMode.ForRead) as Polyline;
                            Point3dCollection pointsOnLine = new Point3dCollection();
                            
                             for (int i = 0; i < pl.NumberOfVertices; i++)
                             {
                                 Point3d vertex = pl.GetPoint3dAt(i);
                                 pointsOnLine.Add(vertex);
                             }
                            var objectIds = editor.SelectFence(pointsOnLine).Value.GetObjectIds();
                            foreach (ObjectId obj2 in objectIds)
                            {
                                Entity ent = tr.GetObject(obj2, OpenMode.ForRead) as Entity;
                                if (ent != null && !(ent is DBText) && !(ent is MText) && !(ent is MLeader) && !(ent is Hatch) && ent != pl && !ent.Layer.Contains("Markings"))
                                {
                                    try
                                    {
                                        Point3dCollection plPoints = new Point3dCollection();
                                        ent.IntersectWith(pl, Intersect.OnBothOperands, plPoints, System.IntPtr.Zero, System.IntPtr.Zero);
                                        string layerName = ent.Layer;
                                        foreach (Point3d intersect in plPoints)
                                        {
                                            MLeader mleader = new MLeader
                                            {
                                                MText = new MText { Contents = layerName },
                                                LandingGap = .1,
                                                TextHeight = height.Value,
                                                TextLocation = new Point3d(intersect.X + .5, intersect.Y + .5, intersect.Z + .5),
                                                ContentType = ContentType.MTextContent,
                                                ArrowSize = .5,
                                                ArrowSymbolId = ObjectId.Null,
                                                LeaderLineType = LeaderType.StraightLeader,
                                                EnableDogleg = false,
                                                Layer = ent.Layer,
                                                EnableFrameText = true
                                            };
                                            mleader.AddLeaderLine(intersect);
                                            btr.AppendEntity(mleader);
                                            tr.AddNewlyCreatedDBObject(mleader, true);
                                        }

                                    }
                                    catch (System.Exception ex)
                                    {
                                        editor.WriteMessage($"\n{ex}");
                                    }

                                }
                                else if (ent is BlockReference)
                                {

                                    try
                                    {
                                        BlockReference br = tr.GetObject(ent.ObjectId, OpenMode.ForRead) as BlockReference;

                                        Point3dCollection ptBlock = new Point3dCollection();
                                        br.IntersectWith(pl, Intersect.OnBothOperands, ptBlock, IntPtr.Zero, IntPtr.Zero);
                                        foreach (Point3d pt1 in ptBlock)
                                        {

                                            
                                                MLeader mleader = new MLeader
                                                {
                                                    MText = new MText { Contents = br.Layer},
                                                    LandingGap = .1,
                                                    TextHeight = height.Value,
                                                    TextLocation = new Point3d(pt1.X + .5, pt1.Y + .5, pt1.Z + .5),
                                                    ContentType = ContentType.MTextContent,
                                                    ArrowSize = .5,
                                                    ArrowSymbolId = ObjectId.Null,
                                                    LeaderLineType = LeaderType.StraightLeader,
                                                    EnableDogleg = false,
                                                    Layer = ent.Layer,
                                                    EnableFrameText = true
                                                };
                                                mleader.AddLeaderLine(pt1);
                                                btr.AppendEntity(mleader);
                                                tr.AddNewlyCreatedDBObject(mleader, true);
                                            
                                        }

                                    }
                                    catch (System.Exception ex)
                                    {
                                        editor.WriteMessage($"\n{ex}");
                                    }
                                }

                                else if (ent is Hatch)
                                
                                {
                                    try
                                    {
                                        Hatch hatch = ent as Hatch;
                                        for (int i = 0; i < hatch.NumberOfLoops; i++)
                                        {
                                            Point3dCollection ptHatch = new Point3dCollection();
                                            HatchLoop hatchLoop = hatch.GetLoopAt(i);
                                            // BulgeVertexCollection bvc = hatch.GetLoopAt(i).Polyline;
                                            
                                            
                                                if (hatchLoop != null && hatchLoop.Polyline !=null)
                                                {
                                                    foreach (Polyline pl2 in hatchLoop.Polyline)
                                                    {
                                                        pl2.IntersectWith(pl, Intersect.OnBothOperands, ptHatch, IntPtr.Zero, IntPtr.Zero);
                                                        foreach (Point3d pt1 in ptHatch)
                                                        {
                                                            MLeader mleader = new MLeader
                                                            {
                                                                MText = new MText { Contents = hatch.Layer },
                                                                LandingGap = .1,
                                                                TextHeight = height.Value,
                                                                TextLocation = new Point3d(pt1.X + .5, pt1.Y + .5, pt1.Z + .5),
                                                                ContentType = ContentType.MTextContent,
                                                                ArrowSize = .5,
                                                                ArrowSymbolId = ObjectId.Null,
                                                                LeaderLineType = LeaderType.StraightLeader,
                                                                EnableDogleg = false,
                                                                Layer = ent.Layer,
                                                                EnableFrameText = true
                                                            };
                                                            mleader.AddLeaderLine(pt1);
                                                            btr.AppendEntity(mleader);
                                                            tr.AddNewlyCreatedDBObject(mleader, true);

                                                        }

                                                    }
                                                }



                                            

                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        editor.WriteMessage($"{ex}");

                                    }
                                }

                            }
                        }
                    }
                }
                tr.Commit();
            }

        }
    }
}

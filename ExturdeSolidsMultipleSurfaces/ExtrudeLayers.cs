using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System.Collections.Generic;
[assembly: CommandClass(typeof(PavementElevationMulti.ExtrudeLayers))]
namespace PavementElevationMulti
{
    public class ExtrudeLayers
    {
        [CommandMethod("ExtrudeLayerMulti")]
        public void LayerCreate()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\nSelect Polylines");

            PromptSelectionResult plineSelected = ed.GetSelection();
            if (plineSelected.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nWrong Selection. Re-run Command.");
                return;
            }
            string name = "SurfaceNew";
            int j = 1;
            SelectionSet set = plineSelected.Value;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                CivilDocument cDoc = CivilApplication.ActiveDocument;
                PromptEntityResult surfaceSelected = ed.GetEntity("\nSelect a surface");
                if (surfaceSelected.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSurface selection failed. Re-run Command.");
                    return;
                }
                TinSurface surface = transaction.GetObject(surfaceSelected.ObjectId, OpenMode.ForRead) as TinSurface;
                PromptDoubleOptions depthOptions = new PromptDoubleOptions($"\nEnter the depth for solid:");
                PromptDoubleResult depthResult = ed.GetDouble(depthOptions);
                double depth = depthResult.Value;
                LayerTable LayerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                PromptStringOptions layerOption = new PromptStringOptions("\nName the layer to Place the Solid: ");
                PromptResult resultLayer = ed.GetString(layerOption);
                string selectedLayerName = resultLayer.StringResult;
                ObjectId selectedLayerId = LayerTable[selectedLayerName];
                foreach (SelectedObject obj in set)
                {
                    if (obj.ObjectId.ObjectClass.DxfName.Contains("POLYLINE"))
                    {
                        Polyline pline = transaction.GetObject(obj.ObjectId, OpenMode.ForWrite) as Polyline;
                        Polyline3d polyline3D = new Polyline3d();
                        polyline3D.SetDatabaseDefaults();
                        polyline3D.Layer = pline.Layer;
                        List<PolylineVertex3d> vertices = new List<PolylineVertex3d>();
                        Point3dCollection breakLinePoints = new Point3dCollection();
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            Point2d vertex2D = pline.GetPoint2dAt(i);
                            double elevation = surface.FindElevationAtXY(vertex2D.X, vertex2D.Y);
                            Point3d vertex3D = new Point3d(vertex2D.X, vertex2D.Y, elevation);
                            PolylineVertex3d vertex = new PolylineVertex3d(vertex3D);
                            vertices.Add(vertex);
                            breakLinePoints.Add(vertex3D);

                        }

                        BlockTableRecord btr = transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        btr.AppendEntity(polyline3D);
                        transaction.AddNewlyCreatedDBObject(polyline3D, true);

                        foreach (PolylineVertex3d vertex in vertices)
                        {

                            polyline3D.AppendVertex(vertex);
                            transaction.AddNewlyCreatedDBObject(vertex, true);
                        }
                        PolylineVertex3d firstVertex = vertices[0].Clone() as PolylineVertex3d;
                        polyline3D.AppendVertex(firstVertex);
                        transaction.AddNewlyCreatedDBObject(firstVertex, true);


                       // pline.Erase();
                        string modifiedName = $"{name}-{j}";
                        j++;
                        ObjectId surfaceStyleId = cDoc.Styles.SurfaceStyles[2];

                        ObjectId surfaceNew = TinSurface.Create(modifiedName, surfaceStyleId);
                        ObjectId[] boundaries = { polyline3D.ObjectId };
                        TinSurface surface1 = surfaceNew.GetObject(OpenMode.ForWrite) as TinSurface;
                        surface1.BreaklinesDefinition.AddStandardBreaklines(breakLinePoints, 1, 1, 1, 0.01);
                        try
                        {
                            surface1.BoundariesDefinition.AddBoundaries(new ObjectIdCollection(boundaries), 10, Autodesk.Civil.SurfaceBoundaryType.Outer, true);
                            surface1.Rebuild();

                        }
                        catch (System.Exception e)
                        {
                            ed.WriteMessage("Failed to add the boundary: {0}", e.Message);

                        }
                       
                        surface1.CreateSolidsAtDepth(depth, surface1.Layer, 0);
                        transaction.GetObject(surface1.ObjectId, OpenMode.ForWrite).Erase();
                        polyline3D.Erase();
                        
                         BlockTableRecord btrC = transaction.GetObject(database.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                         foreach (ObjectId entityId in btrC)
                         {

                             Autodesk.AutoCAD.DatabaseServices.Entity entity = transaction.GetObject(entityId, OpenMode.ForWrite) as Autodesk.AutoCAD.DatabaseServices.Entity;

                             Vector3d moveSolid = new Vector3d(0, 0, -depth);

                             if ( entity is Solid3d && entity.Layer == surface1.Layer)
                             {
                                 if (entity.IsModified == false)
                                 {
                                     entity.UpgradeOpen();
                                     entity.TransformBy(Matrix3d.Displacement(moveSolid));
                                     using (LayerTable = transaction.GetObject(database.LayerTableId,OpenMode.ForRead) as LayerTable)
                                     {
                                         entity.Layer = selectedLayerName;
                                     }
                                 }

                             }


                         }

                    }




                }
                transaction.Commit();
            }
        }
    }
}

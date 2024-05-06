using Autodesk.Aec.Modeler;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System.Collections.Generic;

[assembly: CommandClass(typeof(PavementElevation.ExtrudeLayers))]
namespace PavementElevation
{
    public class ExtrudeLayers
    {
        [CommandMethod("ExtrudeLayer1")]
        public void LayerCreate()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
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
                LayerTable LayerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                /*PromptStringOptions layerOption = new PromptStringOptions("\nName the layer to Place the Solid: ")
                {
                    AllowSpaces = true,
                };
                PromptResult resultLayer = ed.GetString(layerOption);
                string selectedLayerName = resultLayer.StringResult;*/
                ObjectId selectedLayerId = GetLayerIdFromUser();
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


                        pline.Erase();
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
                        PromptIntegerOptions options = new PromptIntegerOptions("\nEnter the number of solids to create:")
                        {
                            AllowZero = false,
                            AllowNegative = false
                        };

                        PromptIntegerResult result = ed.GetInteger(options);

                        if (result.Status != PromptStatus.OK)
                        {
                            ed.WriteMessage("\nInvalid input for the number of solids. Re-run Command.");
                            return;
                        }

                        int numberOfSolids = result.Value;
                        double cumulativeDepth = 0;
                        for (int m = 0; m < numberOfSolids; m++)
                        {
                            PromptDoubleOptions depthOptions = new PromptDoubleOptions($"\nEnter the depth for solid {m + 1}:");

                            PromptDoubleResult depthResult = ed.GetDouble(depthOptions);

                            if (depthResult.Status != PromptStatus.OK)
                            {
                                ed.WriteMessage($"\nInvalid input for the depth of solid {m + 1}. Re-run Command.");
                                return;
                            }

                            double solidDepth = depthResult.Value;

                            surface1.CreateSolidsAtDepth(solidDepth, surface1.Layer, 0);
                            cumulativeDepth += solidDepth;

                            BlockTableRecord btrC = transaction.GetObject(database.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                            foreach (ObjectId entityId in btrC)
                            {

                                Autodesk.AutoCAD.DatabaseServices.Entity entity = transaction.GetObject(entityId, OpenMode.ForWrite) as Autodesk.AutoCAD.DatabaseServices.Entity;

                                Vector3d moveSolid = new Vector3d(0, 0, -cumulativeDepth);

                                if (entity is Solid3d && entity.Layer == surface1.Layer)
                                {
                                    if (entity.IsModified == false)
                                    {
                                        entity.UpgradeOpen();
                                        entity.TransformBy(Matrix3d.Displacement(moveSolid));
                                        using (LayerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable)
                                        {
                                            LayerTableRecord selectedLayer = transaction.GetObject(selectedLayerId, OpenMode.ForRead) as LayerTableRecord;
                                            if (selectedLayer != null)
                                            {
                                                entity.LayerId = selectedLayerId;
                                            }

                                        }
                                    }

                                }


                            }
                        }

                        transaction.GetObject(surface1.ObjectId, OpenMode.ForWrite).Erase();
                        polyline3D.Erase();


                    }




                }
                transaction.Commit();
            }
        }
        public ObjectId GetLayerIdFromUser()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\nSelect a Layer for created Solids from one of the listed below:");

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                ObjectId selectedLayerId = ObjectId.Null;
                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layerTableRecord = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    editor.WriteMessage($"\nLayers available: {layerTableRecord.Name}");
                }

                PromptStringOptions options = new PromptStringOptions("\nEnter the name of the layer to place the Solids (keep it different from the surface layer): ")
                {
                    AllowSpaces = true
                };
                PromptResult result = editor.GetString(options);

                if (result.Status == PromptStatus.OK)
                {
                    string selectedLayerName = result.StringResult;

                    if (layerTable.Has(selectedLayerName))
                    {
                        selectedLayerId = layerTable[selectedLayerName];

                        LayerTableRecord layer = transaction.GetObject(selectedLayerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layer != null)
                        {
                            editor.WriteMessage($"\nSelected layer: {layer.Name}");
                        }
                    }
                    else
                    {
                        editor.WriteMessage($"\nLayer not found: {selectedLayerName}");
                    }
                }



                transaction.Commit();
                return selectedLayerId;
            }
        }
    }
}
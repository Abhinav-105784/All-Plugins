using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

[assembly: CommandClass(typeof(CreateMultipleAlignments8.CreateAlignments))]
namespace CreateMultipleAlignments8
{
    public class CreateAlignments
    {
        [CommandMethod("CreateAlignments")]
        public void createAlignments()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\nSelect Polylines");
            PromptSelectionResult selectedPLs = editor.GetSelection();
            if (selectedPLs.Status != PromptStatus.OK)
            {
                editor.WriteMessage("\nNo objects selected.");
                return;
            }
            SelectionSet selectionSet = selectedPLs.Value;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                using (Transaction alignmentTransaction = database.TransactionManager.StartTransaction())
                {
                    CivilDocument civildoc = CivilApplication.ActiveDocument;

                    foreach (SelectedObject selectedPL in selectionSet)
                    {
                        if (selectedPL.ObjectId.ObjectClass.DxfName.Contains("POLYLINE"))
                        {
                            PromptStringOptions addCurve = new PromptStringOptions("\nWant to add Curves at the vertices? Y or N");
                            PromptResult addCurveResult = editor.GetString(addCurve);
                            string addorNo = addCurveResult.StringResult;
                            PolylineOptions polylineOptions = new PolylineOptions();

                            if (addorNo.Contains("Yes") || addorNo.Contains("Y") || addorNo.Contains("1"))
                            {
                                polylineOptions.AddCurvesBetweenTangents = true;
                                
                            }
                            else
                            {
                                polylineOptions.AddCurvesBetweenTangents = false;
                            }
                            polylineOptions.EraseExistingEntities = true;
                            polylineOptions.PlineId = selectedPL.ObjectId;

                            PromptStringOptions name = new PromptStringOptions("\nGive the name for the Alignment: ");
                            PromptResult nameresult = editor.GetString(name);
                            string alignmentName = nameresult.StringResult;
                            PromptStringOptions AlignmentStyle = new PromptStringOptions("\nGive the type of Alignment Style you want(ex, Basic, Offsets, Propsed, Layout etc.)")
                            { 
                            AllowSpaces=true
                            };
                            PromptResult nameAlResult = editor.GetString(AlignmentStyle);
                            string styleName = nameAlResult.StringResult;

                            ObjectId styleId = civildoc.Styles.AlignmentStyles[styleName];
                            ObjectId labelSetId = civildoc.Styles.LabelSetStyles.AlignmentLabelSetStyles[0];
                            ObjectId siteId = ObjectId.Null;

                            ObjectId layerId = GetLayerIdFromUser();
                            if (layerId == ObjectId.Null)
                            {
                                editor.WriteMessage("\nNo layer selected.");
                                alignmentTransaction.Abort();
                                return;
                            }

                            ObjectId alignment = Alignment.Create(civildoc, polylineOptions, alignmentName, siteId, layerId, styleId, labelSetId);
                        }
                    }

                    alignmentTransaction.Commit();
                }
                transaction.Commit();
            }
        }

        public ObjectId GetLayerIdFromUser()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            editor.WriteMessage("\nSelect a Layer for created Alignment from one of the listed below:");

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                ObjectId selectedLayerId = ObjectId.Null;
                LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layerTableRecord = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    editor.WriteMessage($"\nLayers available: {layerTableRecord.Name}");
                }

                PromptStringOptions options = new PromptStringOptions("\nEnter the name of the layer to place the Alignment: ")
                {
                    AllowSpaces = true,
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
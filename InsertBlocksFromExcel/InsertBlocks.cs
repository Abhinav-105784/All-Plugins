using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using ExcelDataReader;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Autodesk.Civil;

namespace InsertBlocksFromExcel8
{
    public class InsertBlocks
    {
        [CommandMethod("InsertBlocksFromExcel")]
        public void ShowForm()
        {
            Browse_Excel browse = new Browse_Excel();
            browse.Show();
        }

        public static void Blocks(string selectedFile)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                using (var stream = File.Open(selectedFile, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet dataset = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                        });
                        var dataTable = dataset.Tables[0];
                        string columnHeader1 = dataTable.Columns[0].ColumnName;
                        string columnHeader2 = dataTable.Columns[1].ColumnName;
                        string columnHeader3 = dataTable.Columns[2].ColumnName;
                        string columnHeader4 = dataTable.Columns[3].ColumnName;
                        string columnHeader5 = dataTable.Columns[4].ColumnName;
                        //string columnHeader6 = dataTable.Columns[5].ColumnName;

                        if (columnHeader1 == "Block Name" && columnHeader2 == "x" && columnHeader3 == "y" && columnHeader4 == "z" && columnHeader5 == "Rotation" )//&& columnHeader6=="Layer")
                        {
                            document.LockDocument();
                            BlockTable bt = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
                            LayerTable lt = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                            for (int row = 0; row < dataTable.Rows.Count; row++)
                            {
                                string blockName = dataTable.Rows[row][0].ToString();
                                double x = Convert.ToDouble(dataTable.Rows[row][1]);
                                double y = Convert.ToDouble(dataTable.Rows[row][2]);
                                double z = Convert.ToDouble(dataTable.Rows[row][3]);
                                double rotation = Convert.ToDouble(dataTable.Rows[row][4]);
                                //string layer = dataTable.Rows[row][5].ToString();
                                double rotationRads = rotation * 3.14159265 / 180;
                                if (bt.Has(blockName))
                                {
                                    if (!lt.Has(blockName))
                                    {
                                        LayerTableRecord layerRecord = new LayerTableRecord
                                        {
                                            Name = blockName
                                        };
                                        lt.UpgradeOpen();
                                        lt.Add(layerRecord);
                                        transaction.AddNewlyCreatedDBObject(layerRecord, true);
                                    }
                                    BlockTableRecord btr = transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    BlockReference br = new BlockReference(new Point3d(x, y, z), bt[blockName])
                                    {
                                        ScaleFactors = new Scale3d(1.0, 1.0, 1.0),
                                        Rotation = rotationRads,
                                        Layer = blockName,
                                        

                                    };
                                    transaction.GetObject(database.BlockTableId, OpenMode.ForWrite);
                                    btr.AppendEntity(br);
                                    transaction.AddNewlyCreatedDBObject(br, true);
                                    document.Editor.UpdateScreen();
                                    editor.WriteMessage($"\nBlock {blockName} inserted");
                                }
                                else
                                {
                                    editor.WriteMessage($"\nBlock '{blockName}' not found.");
                                    return;
                                }
                                
                            }
                        }
                        else
                        {
                            MessageBox.Show("The Excel file is not in the proper format. Please make sure the column headers are correct.", "Excel Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                transaction.Commit();
            }
        }
    }
}

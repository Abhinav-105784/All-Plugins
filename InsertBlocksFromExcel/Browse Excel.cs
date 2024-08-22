using ExcelDataReader;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
namespace InsertBlocksFromExcel8
{
    public partial class Browse_Excel : Form
    {
        public Browse_Excel()
        {
            InitializeComponent();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Open_Click(object sender, EventArgs e)
        {
            string selectedFile = textBox1.Text;

            if(!string.IsNullOrEmpty(selectedFile))
            {
                InsertBlocks.Blocks(selectedFile);
                MessageBox.Show("Blocks Inserted succesfully");
            }
            else
            {
                MessageBox.Show("Please select the excel file");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Data.DataTable dt = dataTable[comboBox1.SelectedItem.ToString()];
            dataGridView1.DataSource = dt;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }
        DataTableCollection dataTable;
        private void browse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                openFile.Filter = "Excel Files| *.xls;*.xlsm;*.xlsx";
                openFile.Title = "Select an excel File";

                if(openFile.ShowDialog()==DialogResult.OK)
                {
                    textBox1.Text = openFile.FileName;
                    using (var stream = System.IO.File.Open(openFile.FileName, System.IO.FileMode.Open,FileAccess.Read))
                    {
                        using(IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }

                            });
                          
                            dataTable = result.Tables;
                            comboBox1.Items.Clear();
                            foreach (DataTable table in dataTable)
                            {
                                comboBox1.Items.Add(table.TableName);
                            }

                        }
                          
                    }
                }
            }

        }
    }
}

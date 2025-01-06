using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.BlockSectionMenu
{
    /// <summary>
    /// Interaction logic for BlockSectionCurriculum.xaml
    /// </summary>
    public partial class BlockSectionCurriculum : Window
    {

        public int? CurriculumId { get; private set; }

        public string CurriculumDescription { get; private set; }

        public int DepartmentId { get; set; }

        string connectionString = App.ConnectionString;

        public BlockSectionCurriculum(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;
            LoadCurriculumDataGrid();
        }
        private void LoadCurriculumDataGrid()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    Curriculum_Id, 
                    Curriculum_Revision, 
                    Curriculum_Description, 
                    CONCAT(Year_Effective_In, '-', Year_Effective_Out) AS Year
                FROM curriculum
                WHERE Dept_Id = @departmentID AND Status = 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentID", DepartmentId);

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSectionCurriculum_data.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        int SelectedCurriculumId;
        private void BlockSection_Curriculum_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if there is a selected item
            if (BlockSectionCurriculum_data.SelectedItem is DataRowView selectedRow)
            {
                // Retrieve the CurriculumId from the selected row
                SelectedCurriculumId = Convert.ToInt32(selectedRow["Curriculum_Id"]); // Adjust the column name as necessary
                CurriculumDescription = selectedRow["Curriculum_Description"].ToString();

            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void all_btn_Click(object sender, RoutedEventArgs e)
        {
            CurriculumId = null;
            CurriculumDescription = "";
            this.Close();
        }

        private void curriculum_btn_Click(object sender, RoutedEventArgs e)
        {

            CurriculumId = SelectedCurriculumId;
            this.Close();
        }
    }
}

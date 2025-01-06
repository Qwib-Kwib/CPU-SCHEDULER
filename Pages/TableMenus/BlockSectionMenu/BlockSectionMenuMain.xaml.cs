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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.BlockSectionMenu
{
    /// <summary>
    /// Interaction logic for BlockSectionMenuMain.xaml
    /// </summary>
    public partial class BlockSectionMenuMain : Page
    {
        int DepartmentId { get; set; }

        int CurriculumId { get; set; }

        string connectionString = App.ConnectionString;

        public BlockSectionMenuMain(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Block Sections", TopBar_BackButtonClicked);
            LoadcCreateTabUI();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigateBack("BlockSection");
        }

        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }

        private void main_tab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (main_tab.SelectedItem is TabItem selectedTab)
            {
                // Get the header of the selected tab
                string tabTag = selectedTab.Tag.ToString();
                if (tabTag == "1")
                {
                    LoadcCreateTabUI();
                }
                else if (tabTag == "2") 
                {

                }
            }
        }

        #region //Create Tab

        private void LoadcCreateTabUI()
        {
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
                WHERE Dept_Id = @departmentID";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentID", DepartmentId);

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSection_Curriculum_grid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        #endregion

        private void BlockSection_Curriculum_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        
    }
}

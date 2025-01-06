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

namespace Info_module.Pages.TableMenus.AssignmentMenu
{
    /// <summary>
    /// Interaction logic for ScheduleMenuBlockSection.xaml
    /// </summary>
    public partial class ScheduleMenuBlockSection : Window
    {

        public int CurriculumId { get; set; }

        public int BlockSectionId { get; set; }

        string connectionString = App.ConnectionString;

        public ScheduleMenuBlockSection()
        {
            InitializeComponent();


            LoadCurriculum();
            LoadDepartmentitems();
        }

        public class Department
        {
            public int DepartmentIds { get; set; }
            public string DepartmentCodes { get; set; }

        }

        private void LoadDepartmentitems()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Id, Dept_Code FROM departments";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    List<Department> departments = new List<Department>();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departments.Add(new Department
                            {
                                DepartmentIds = reader.GetInt32("Dept_Id"),
                                DepartmentCodes = reader.GetString("Dept_Code")
                            });
                        }
                    }

                    // Add "All" option at the top
                    departments.Insert(0, new Department { DepartmentIds = -1, DepartmentCodes = "All" });

                    collegiate_cmbx.ItemsSource = departments;

                    collegiate_cmbx.DisplayMemberPath = "DepartmentCodes";
                    collegiate_cmbx.SelectedValuePath = "DepartmentIds";

                    // Set the default selected item as "All"
                    collegiate_cmbx.SelectedIndex = 0; // Selects "All"
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading Departments: " + ex.Message);
            }
        }

        #region curriculum

        private DataTable curriculumDataTable; // Store the loaded data

        private void LoadCurriculum(string filter = "All")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Base query to load curriculum with block section assignment status
                    string query = @"
        SELECT 
            c.Curriculum_Id AS Curriculum_Id, 
            c.Curriculum_Revision AS Curriculum_Revision,
            c.Curriculum_Description AS Curriculum_Description,
            d.Dept_Code AS Department,
            CONCAT(c.Year_Effective_In, '-', c.Year_Effective_Out) AS Year_Effective,
            SUM(CASE WHEN bsl.status = 'assigned' THEN 1 ELSE 0 END) AS AssignedCount,
            SUM(CASE WHEN bsl.status = 'waiting' THEN 1 ELSE 0 END) AS UnassignedCount
        FROM curriculum c
        JOIN departments d ON c.Dept_Id = d.Dept_Id
        LEFT JOIN block_section bs ON bs.curriculumId = c.Curriculum_Id
        LEFT JOIN block_subject_list bsl ON bsl.blockSectionId = bs.blockSectionId
        WHERE c.Status = 1
        GROUP BY c.Curriculum_Id";

                    // Apply filter logic to the aggregated counts
                    switch (filter)
                    {
                        case "No Assignments":
                            query += " HAVING AssignedCount = 0 AND UnassignedCount > 0";
                            break;

                        case "Partial Assignments":
                            query += " HAVING AssignedCount > 0 AND UnassignedCount > 0";
                            break;

                        case "Complete Assignments":
                            query += " HAVING UnassignedCount = 0 AND AssignedCount > 0";
                            break;

                        case "all":
                        default:
                            // No additional filter, show all curricula with block section assignments
                            // No filter is required in this case
                            break;
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);

                    curriculumDataTable = new DataTable(); // Initialize the DataTable
                    dataAdapter.Fill(curriculumDataTable); // Fill the DataTable

                    // Bind the resulting data to the DataGrid
                    curriculum_data.ItemsSource = curriculumDataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        private void curriculum_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculum_data.SelectedItem is DataRowView selectedRow)
            {
                CurriculumId = (int)selectedRow["Curriculum_Id"];
                curriculum_Id_txt.Text = selectedRow[3].ToString();
                LoadBlockSection(CurriculumId);
            }

        }

        private void blockSections_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (blockSections_data.SelectedItem is DataRowView selectedRow)
            {
                BlockSectionId = (int)selectedRow["assignment_BlockSection_Id"];
               blockSection_txt.Text = selectedRow[1].ToString();
                blocksection_Id_txt.Text = BlockSectionId.ToString();
                
            }

        }

        private void collegiate_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only filter if the ComboBox has a valid selection
            if (collegiate_cmbx.SelectedValue != null)
            {
                // Get the selected department ID
                int selectedDeptId = (int)collegiate_cmbx.SelectedValue;

                // Find the department code corresponding to the selected ID
                string selectedDeptCode = null;

                if (collegiate_cmbx.SelectedItem is Department selectedDepartment)
                {
                    selectedDeptCode = selectedDepartment.DepartmentCodes; // Get the department code
                }

                // Use DataView to filter the DataTable
                if (curriculumDataTable != null)
                {
                    DataView view = new DataView(curriculumDataTable);

                    if (selectedDeptCode != "All") // If the selected value is not "All"
                    {
                        view.RowFilter = $"Department = '{selectedDeptCode}'"; // Filter by department code
                    }
                    else
                    {
                        view.RowFilter = string.Empty; // Show all if "All" is selected
                    }

                    curriculum_data.ItemsSource = view; // Bind the filtered view to the DataGrid
                }
            }
            else
            {
                // If nothing is selected, reset the DataGrid to show all data
                curriculum_data.ItemsSource = curriculumDataTable.DefaultView;
            }
        }

        private void instructorAssignment_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculumAssignment_cmbx.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedFilter = selectedItem.Content.ToString();
                LoadCurriculum(selectedFilter); // Pass the filter to the LoadClasses method
            }
        }

        #endregion


        private void LoadBlockSection(int curriculumId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to retrieve block section details with assigned status based on all subject list statuses
                    string query = @"
        SELECT 
            bs.blockSectionId AS assignment_BlockSection_Id, 
            bs.blockSectionName AS assignment_BlockSection_Name, 
            bs.year_level AS assignment_BlockSection_Year, 
            bs.semester AS assignment_BlockSection_Semester, 
            CASE 
                WHEN COUNT(bsl.subjectList_Id) = COUNT(CASE WHEN bsl.status = 'assigned' THEN 1 END) THEN 'Assigned'
                ELSE 'Waiting'
            END AS Assigned
        FROM block_section bs
        LEFT JOIN block_subject_list bsl ON bsl.blockSectionId = bs.blockSectionId
        WHERE bs.curriculumId = @curriculumId
        GROUP BY bs.blockSectionId, bs.blockSectionName, bs.year_level, bs.semester";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@curriculumId", curriculumId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the DataTable to the DataGrid
                    blockSections_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading Block Sections: " + ex.Message);
            }
        }



        private void subjectFilter_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the ComboBox has a valid selection
            if (subjectFilter_cmbx.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedStatus = selectedItem.Tag.ToString(); // Get the tag for filtering

                // Use DataView to filter the DataTable
                if (blockSections_data.ItemsSource is DataView view)
                {
                    if (selectedStatus != "All") // If the selected value is not "All"
                    {
                        view.RowFilter = $"Assigned = '{selectedStatus}'"; // Adjust 'Status' based on your actual column name
                    }
                    else
                    {
                        view.RowFilter = string.Empty; // Show all if "All" is selected
                    }

                    blockSections_data.ItemsSource = view; // Bind the filtered view to the DataGrid
                }
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public int SelectedBlockSectionId;
        public string SelectedBlockSectionName;

        private void blockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            SelectedBlockSectionId = BlockSectionId;
            SelectedBlockSectionName = blockSection_txt.Text;

            this.Close();

        }
    }
}

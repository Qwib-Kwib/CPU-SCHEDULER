using Info_module.Pages.TableMenus.Assignment;
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

namespace Info_module.Pages.TableMenus.AssignmentMenu
{
    /// <summary>
    /// Interaction logic for ScheduleMenuView.xaml
    /// </summary>
    public partial class ScheduleMenuView : Page
    {

        public int CurriculumId { get; set; }

        public int BlockSectionId { get; set; }

        string connectionString = App.ConnectionString;

        public ScheduleMenuView()
        {
            InitializeComponent();

            LoadDepartmentitems();

            LoadData();

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Schedule Menu", TopBar_BackButtonClicked);

            viewBy_cmbx.SelectedIndex = 0;

            byBlockSection_viewbox.Margin = new Thickness(0);
            byInstructor_viewbox.Margin = new Thickness(0);
            byBuilding_viewbox.Margin = new Thickness(0);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new AssignMenuMain());
        }

        private void viewBy_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the ComboBox has a selected item
            if (viewBy_cmbx.SelectedIndex < 0) return; // No selection

            // Get the selected index
            int selectedIndex = viewBy_cmbx.SelectedIndex;

            // Use a switch statement to handle different cases based on the selected index
            switch (selectedIndex)
            {
                case 0:
                    byBlockSection_viewbox.Visibility = Visibility.Visible;
                    byInstructor_viewbox.Visibility = Visibility.Collapsed;
                    byBuilding_viewbox.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    byBlockSection_viewbox.Visibility = Visibility.Collapsed;
                    byInstructor_viewbox.Visibility = Visibility.Visible;
                    byBuilding_viewbox.Visibility = Visibility.Collapsed;
                    break;

                case 2:
                    byBlockSection_viewbox.Visibility = Visibility.Collapsed;
                    byInstructor_viewbox.Visibility = Visibility.Collapsed;
                    byBuilding_viewbox.Visibility = Visibility.Visible;
                    break;

                default:
                    // Optional: Handle unexpected cases
                    MessageBox.Show("Unexpected selection.");
                    break;
            }

        }

        #region By Block Section
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
                    

                    instructorCollege_cmbx.ItemsSource = departments;
                    instructorCollege_cmbx.DisplayMemberPath = "DepartmentCodes";
                    instructorCollege_cmbx.SelectedValuePath = "DepartmentIds";
                    instructorCollege_cmbx.SelectedIndex = 0;
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
                blocksection_Id_txt.Text = BlockSectionId.ToString();

                loadScheduleByBlock(BlockSectionId);
                loadUnassinedSubject(BlockSectionId);

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

        private void loadScheduleByBlock(int section)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to load data from the class table
                    string query = @"
                SELECT c.Class_Id,
                       c.Subject_Id,
                       c.Internal_Employee_Id,
                       c.Room_Id,
                       c.Stub_Code,
                       s.Lecture_Lab as Lec_Lab,
                       c.Class_Day,
                       c.Start_Time,
                       c.End_Time,
                       s.Subject_Code AS SubjectCode,
                       CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
                       r.Room_Code AS RoomCode
                FROM subjects s
                LEFT JOIN class c ON c.Subject_Id = s.Subject_Id
                LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
                LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
                Where c.Block_Section_Id = '" + section + "'";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the resulting data to the DataGrid
                    schedule_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading class details: " + ex.Message);
            }
        }


        private void loadUnassinedSubject(int section)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with "waiting" status for the specified block section
                    string query = @"
                SELECT 
                    s.Subject_Code,
                    s.Subject_Type,
                    s.Lecture_Lab,
                    s.Hours,
                    s.Units
                FROM 
                    block_subject_list bsl
                JOIN 
                    subjects s ON bsl.subjectId = s.Subject_Id
                WHERE 
                    bsl.blockSectionId = @blockSectionId AND 
                    bsl.status = 'waiting'";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add the blockSectionId parameter
                        command.Parameters.AddWithValue("@blockSectionId", section);

                        // Create a DataTable to hold the results
                        DataTable dataTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            // Fill the DataTable with the results
                            adapter.Fill(dataTable);
                        }

                        // Bind the DataTable to a DataGrid or other UI component
                        // Assuming you have a DataGrid named 'UnassignedSubjectsGrid'
                        unAssingedSubjects_data.ItemsSource = dataTable.DefaultView; // Replace with your actual DataGrid name
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        #endregion

        #region by Instructor

        private void instructorCollege_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only filter if the ComboBox has a valid selection
            if (instructorCollege_cmbx.SelectedValue != null)
            {
                // Get the selected department ID
                int selectedDeptId = (int)instructorCollege_cmbx.SelectedValue;
                LoadInstructors(selectedDeptId);
            }
            else
            {
            }

        }

        private void LoadInstructors(int departmentFilter = -1)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Base query
                    string query = @"
        SELECT i.Internal_Employee_Id, 
               d.Dept_code AS Department,
               i.Employee_Id,
               i.Lname AS LastName, 
               i.Mname AS MiddleName, 
               i.Fname AS FirstName, 
               i.Employment_Type AS Employment, 
               i.Employee_Sex AS Sex,
               i.Email,
               i.Disability,
               CASE 
                   WHEN i.Status = 1 THEN 'Active'
                   ELSE 'Inactive'
               END AS Status
        FROM instructor i
        JOIN departments d ON i.Dept_Id = d.Dept_Id
        WHERE i.Status = 1"; // Always filter for active instructors

                    // Add a condition for filtering by department
                    if (departmentFilter != -1)
                    {
                        query += " AND i.Dept_Id = @deptId";
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    // Only add the parameter if departmentFilter is not -1
                    if (departmentFilter != -1)
                    {
                        command.Parameters.AddWithValue("@deptId", departmentFilter);
                    }

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    Instructor_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }

        private void Instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Instructor_data.SelectedItem is DataRowView selectedRow)
            {
                int instructorId = (int)selectedRow[0];
                
                selectedInstructor_txt.Text = selectedRow["Employee_Id"].ToString();

                loadScheduleByInstructor(instructorId);
            }

        }

        private void loadScheduleByInstructor(int instructorId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to load data from the class table
                    string query = @"
        SELECT c.Class_Id,
               c.Subject_Id,
               c.Internal_Employee_Id,
               c.Room_Id,
               c.Stub_Code,
               s.Lecture_Lab AS Lec_Lab,
               c.Class_Day,
               c.Start_Time,
               c.End_Time,
               s.Subject_Code AS SubjectCode,
               CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
               r.Room_Code AS RoomCode
        FROM subjects s
        LEFT JOIN class c ON c.Subject_Id = s.Subject_Id
        LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
        LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
        WHERE c.Internal_Employee_Id = @instructorId"; // Filter by instructor ID

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@instructorId", instructorId); // Use parameterized query

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the resulting data to the DataGrid
                    schedule_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading class details: " + ex.Message);
            }
        }



        #endregion


        #region By Building
        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                        Building_Id, Building_Code, Building_Name
                                    FROM buildings WHERE Status = 1";

                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    building_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        private void loadScheduleByBuilding(int buildingId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to load data from the class table, filtering by Building_Id and ordering by Room_Code
                    string query = @"
        SELECT c.Class_Id,
               c.Subject_Id,
               c.Internal_Employee_Id,
               c.Room_Id,
               c.Stub_Code,
               s.Lecture_Lab AS Lec_Lab,
               c.Class_Day,
               c.Start_Time,
               c.End_Time,
               s.Subject_Code AS SubjectCode,
               CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
               r.Room_Code AS RoomCode
        FROM subjects s
        LEFT JOIN class c ON c.Subject_Id = s.Subject_Id
        LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
        LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
        WHERE r.Building_Id = @buildingId
        ORDER BY r.Room_Code"; // Order by Room_Code

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@buildingId", buildingId); // Use parameterized query

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the resulting data to the DataGrid
                    schedule_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading class details: " + ex.Message);
            }
        }


        private void building_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (building_data.SelectedItem is DataRowView selectedRow)
            {
                int buildingId = (int)selectedRow[0];

                selectedBuilding_txt.Text = selectedRow["Building_Code"].ToString();

                loadScheduleByBuilding(buildingId);

            }
        }




        #endregion

        private void editBlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if a block section is selected
            if (BlockSectionId <= 0) // Assuming BlockSectionId is set to a valid ID when a section is selected
            {
                MessageBox.Show("Please select a block section first.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Exit the method if no block section is selected
            }

            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                ScheduleMenuEdit windowMenu = new ScheduleMenuEdit(BlockSectionId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                windowMenu.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                loadScheduleByBlock(BlockSectionId);
            }
        }
    }
}

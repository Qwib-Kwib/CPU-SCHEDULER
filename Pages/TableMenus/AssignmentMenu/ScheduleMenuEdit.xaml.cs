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
using static Info_module.Pages.TableMenus.AssignmentMenu.ScheduleMenuEdit;

namespace Info_module.Pages.TableMenus.AssignmentMenu
{
    /// <summary>
    /// Interaction logic for ScheduleMenuEdit.xaml
    /// </summary>
    public partial class ScheduleMenuEdit : Window
    {
        string connectionString = App.ConnectionString;

        public int SectionId;

        int? Deptid;
        int? DepartmentBuilding;
        int SubjectId;

        public ScheduleMenuEdit(int sectionId)
        {
            InitializeComponent();
            SectionId = sectionId;

            Deptid = GetDeptIdByBlockSectionId(SectionId);
            DepartmentBuilding = GetBuildingIdByDeptId(Convert.ToInt32(Deptid));

            LoadUi();

        }

        public void LoadUi()
        {
            loadUnassinedSubject(SectionId);
            LoadClassTimes(SectionId);
            LoadBuildings();
            SetSelectedBuilding(DepartmentBuilding);
            LoadInstructorsByDeptId(null);
        }

        #region Load Ui

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
                    s.Subject_Id,
                    s.Dept_Id,
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

        private void LoadClassTimes(int blockSectionId, string day = null)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load class times
                    string query = @"
                SELECT 
                    Class_Day AS Day,
                    Start_Time,
                    End_Time
                FROM 
                    class
                WHERE 
                    Block_Section_Id = @blockSectionId
                    AND (@day IS NULL OR Class_Day = @day)
                ORDER BY 
                    FIELD(Class_Day, 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'), 
                    Start_Time;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                        command.Parameters.AddWithValue("@day", (object)day ?? DBNull.Value); // Use DBNull.Value for null

                        // Create a DataTable to hold the results
                        DataTable dataTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            // Fill the DataTable with the results
                            adapter.Fill(dataTable);
                        }

                        // Bind the DataTable to the DataGrid
                        OccupiedTime_data.ItemsSource = dataTable.DefaultView; // Replace 'YourDataGrid' with the actual name of your DataGrid
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

        private void LoadBuildings()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get all buildings
                    string query = "SELECT Building_Id, Building_Code FROM buildings";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            List<Building> buildings = new List<Building>();

                            while (reader.Read())
                            {
                                // Create a new Building object and populate it
                                Building building = new Building
                                {
                                    BuildingId = reader.GetInt32("Building_Id"),
                                    BuildingCode = reader.GetString("Building_Code")
                                };

                                buildings.Add(building);
                            }

                            // Bind the list of buildings to the ComboBox
                            building_cmbx.ItemsSource = buildings;
                        }
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

        private void SetSelectedBuilding(int? buildingId)
        {
            if (buildingId == null)
            {
                MessageBox.Show("Building ID cannot be null.");
                return;
            }

            if (building_cmbx.ItemsSource is List<Building> buildings)
            {
                // Find the index of the building with the matching ID
                int index = buildings.FindIndex(b => b.BuildingId == buildingId);

                // If a matching building is found, set the selected index
                if (index != -1)
                {
                    building_cmbx.SelectedIndex = index;
                }
                else
                {
                    MessageBox.Show("Building ID not found.");
                }
            }
        }

        private void LoadRooms(int BuildingId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    Room_Id, 
                    Room_Code, 
                    Room_Floor, 
                    Room_Type,  
                    Max_Seat
                FROM rooms
                WHERE Building_Id = @BuildingId and Status = 1";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BuildingId", BuildingId);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    room_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInstructorsByDeptId(int? deptId)
        {
            try
            { 

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load instructor data filtered by Dept_Id (optional) and Status = 1
                    string query = @"
                SELECT 
                    Internal_Employee_Id,
                    Employee_Id,
                    CONCAT(Fname, ' ', Mname, ' ', Lname) AS FullName,
                    Employment_Type,
                    Employee_Sex,
                    Disability
                FROM 
                    instructor
                WHERE 
                    Status = 1
                    AND (@deptId IS NULL OR Dept_Id = @deptId)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add the deptId parameter, using DBNull.Value if deptId is null
                        command.Parameters.AddWithValue("@deptId", (object)deptId ?? DBNull.Value);

                        // Create a DataTable to hold the results
                        DataTable dataTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            // Fill the DataTable with the results
                            adapter.Fill(dataTable);
                        }

                        // Bind the DataTable to a DataGrid or other UI component
                        instructor_data.ItemsSource = dataTable.DefaultView; // Replace 'YourDataGrid' with the actual name of your DataGrid
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

        #region Load Values
        private int? GetDeptIdByBlockSectionId(int blockSectionId)
        {
            int? deptId = null; // Initialize deptId to null

            try
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get Dept_id based on blockSectionId
                    string query = @"
                SELECT 
                    c.Dept_id
                FROM 
                    block_section b
                JOIN 
                    curriculum c ON b.curriculumId = c.Curriculum_Id
                WHERE 
                    b.blockSectionId = @blockSectionId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add the blockSectionId parameter
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                        // Execute the command and retrieve the Dept_id
                        object result = command.ExecuteScalar(); // Use ExecuteScalar to get a single value

                        if (result != null)
                        {
                            deptId = Convert.ToInt32(result); // Convert the result to int
                        }
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

            return deptId; // Return the retrieved Dept_id or null if not found
        }
        private int? GetBuildingIdByDeptId(int deptId)
        {
            int? buildingId = null; // Initialize buildingId to null

            try
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get Building_Id based on Dept_Id
                    string query = @"
                SELECT 
                    Building_Id
                FROM 
                    departments
                WHERE 
                    Dept_Id = '"+deptId+"'";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {

                        // Execute the command and retrieve the Building_Id
                        object result = command.ExecuteScalar(); // Use ExecuteScalar to get a single value

                        if (result != null)
                        {
                            buildingId = Convert.ToInt32(result); // Convert the result to int
                        }
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

            return buildingId; // Return the retrieved Building_Id or null if not found
        }
        public class Building
        {
            public int BuildingId { get; set; }
            public string BuildingCode { get; set; }

            public override string ToString()
            {
                return BuildingCode; // This will be displayed in the ComboBox
            }
        }

        #endregion

        #region UI event/Classes

        private void unAssingedSubjects_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if there is a selected item
            if (unAssingedSubjects_data.SelectedItem is DataRowView selectedRow)
            {
                // Assuming your DataTable has columns named "SubjectId" and "SubjectCode"
                int subjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                SubjectId = subjectId;
                string subjectCode = selectedRow["Subject_Code"].ToString();
                subjectCode_txt.Text = subjectCode;
                int deptId = Convert.ToInt32(selectedRow["Dept_Id"]);
                LoadInstructorsByDeptId(deptId);
                

            }
            else
            {
                // Handle the case where no item is selected
                MessageBox.Show("No subject selected.");
            }
        }

        private void day_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected item from the ComboBox
            ComboBoxItem selectedItem = (ComboBoxItem)day_cmbx.SelectedItem;

            // Check if the selected item is not null
            if (selectedItem != null)
            {
                // Get the content of the selected item
                string selectedDay = selectedItem.Content.ToString();

                // Check if "Day" is selected
                if (selectedDay == "Day")
                {
                    // Call the method to load class times without filtering
                    LoadClassTimes(SectionId); // Replace with your actual blockSectionId
                }
                else
                {
                    // Call the method to load class times with the selected day as a filter
                    LoadClassTimes(SectionId, selectedDay);
                }
            }
        }

        private void building_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (building_cmbx.SelectedItem is Building selectedBuilding)
            {
                DepartmentBuilding = selectedBuilding.BuildingId;
                LoadRooms(Convert.ToInt32(DepartmentBuilding));
            }

        }
        private void room_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (room_data.SelectedItem is DataRowView selectedRow) 
            {
                int roomId = Convert.ToInt32(selectedRow[0]);
                string roomCode = selectedRow[1].ToString();
                roomCode_txt.Text = roomCode;
            }
        }




        #endregion


    }
}

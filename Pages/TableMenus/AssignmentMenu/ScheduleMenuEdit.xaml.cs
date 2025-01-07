using CsvHelper.Configuration.Attributes;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

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
        int InstructorId;
        int ClassId;
        int StubCode;
        int SchedSubject;
        int SchedInstructor;
        string SelectedDay;
        int RoomId;

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
            loadScheduleByBlock(SectionId);
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
                    s.Units,
                    bsl.status
                FROM 
                    block_subject_list bsl
                JOIN 
                    subjects s ON bsl.subjectId = s.Subject_Id
                WHERE 
                    bsl.blockSectionId = @blockSectionId";

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

        private void LoadInstructorsBySubjectId(int subjectId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load instructor data along with their subject load
                    string query = @"
                SELECT 
                    i.Internal_Employee_Id,
                    i.Employee_Id,
                    CONCAT(i.Fname, ' ', i.Mname, ' ', i.Lname) AS FullName,
                    i.Employment_Type,
                    i.Employee_Sex,
                    i.Disability,
                    sl.Subject_Id,
                    sl.Subject_Code,
                    COUNT(CASE WHEN sl.Status = 'assigned' THEN 1 END) AS TotalAssigned,
                    COUNT(CASE WHEN sl.Status = 'waiting' THEN 1 END) AS TotalWaiting
                FROM 
                    instructor i
                JOIN 
                    subject_load sl ON i.Internal_Employee_Id = sl.Internal_Employee_Id
                WHERE 
                    sl.Subject_Id = @subjectId
                GROUP BY 
                    i.Internal_Employee_Id, 
                    sl.Subject_Id, 
                    sl.Subject_Code";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add the subjectId parameter
                        command.Parameters.AddWithValue("@subjectId", subjectId);

                        // Create a DataTable to hold the results
                        DataTable dataTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            // Fill the DataTable with the results
                            adapter.Fill(dataTable);
                        }

                        // Bind the DataTable to a DataGrid or other UI component
                        instructor_data.ItemsSource = dataTable.DefaultView; // Replace 'instructor_data' with the actual name of your DataGrid
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
                    Dept_Id = '" + deptId + "'";

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
                LoadInstructorsBySubjectId(subjectId);


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
                SelectedDay = selectedDay;

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
                RoomId = Convert.ToInt32(selectedRow[0]);  
                string roomCode = selectedRow[1].ToString();
                roomCode_txt.Text = roomCode;
            }
        }
        private void instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                InstructorId = Convert.ToInt32(selectedRow[0]);
                instructor_txt.Text = selectedRow[1].ToString();
            }
        }

        private void schedule_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (schedule_data.SelectedItem is DataRowView selectedRow)
            {
                ClassId = Convert.ToInt32(selectedRow[0]);
                SchedSubject = Convert.ToInt32(selectedRow[1]);
                SchedInstructor = Convert.ToInt32(selectedRow[2]);
                StubCode = Convert.ToInt32(selectedRow[4]);
                stubCode_txt.Text = selectedRow[4].ToString();
            }
        }

        private void save_Instructor_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!InstructorHasWaitingSubjectLoad(InstructorId, SchedSubject))
            {
                MessageBox.Show($"Invalid Instructor");
                return;
            }

            ChangeSubjectLoad(InstructorId, SchedInstructor, SubjectId);
            UpdateInstructorByStubCode(StubCode, InstructorId, SchedInstructor);
            loadScheduleByBlock(SectionId);
        }


        #endregion

        private void close_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region //Algo

        private void UpdateInstructorByStubCode(int stubCode, int newInstructorId, int currentInstructorId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to update the instructor for a specified Stub_Code
                    string query = @"
                UPDATE `class`
                SET `Internal_Employee_Id` = @newInstructorId
                WHERE `Stub_Code` = @stubCode AND `Internal_Employee_Id` = @currentInstructorId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@newInstructorId", newInstructorId);
                        command.Parameters.AddWithValue("@stubCode", stubCode);
                        command.Parameters.AddWithValue("@currentInstructorId", currentInstructorId);

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Instructor updated successfully.");
                        }
                        else
                        {
                            MessageBox.Show("No matching records found to update.");
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

        private bool InstructorHasWaitingSubjectLoad(int instructorId, int subjectId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to check if the instructor has a subject load for the specified subject with status 'waiting'
                    string query = @"
                SELECT COUNT(*) 
                FROM subject_load 
                WHERE Internal_Employee_Id = @instructorId AND Subject_Id = @subjectId AND Status = 'waiting'";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@instructorId", instructorId);
                        command.Parameters.AddWithValue("@subjectId", subjectId);

                        // Execute the command and retrieve the count
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Return true if the count is greater than 0, otherwise false
                        return count > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Log the error (consider using a logging framework)
                Console.WriteLine($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                // Log the error (consider using a logging framework)
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }


        public bool ChangeSubjectLoad(int instructorId1, int instructorId2, int subjectId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Start a transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Update the first instructor's subject load to 'waiting'
                        string updateWaitingQuery = @"
                        UPDATE subject_load 
                        SET Status = 'waiting' 
                        WHERE Internal_Employee_Id = @instructorId1 AND Subject_Id = @subjectId";

                        using (MySqlCommand command = new MySqlCommand(updateWaitingQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@instructorId1", instructorId1);
                            command.Parameters.AddWithValue("@subjectId", subjectId);
                            command.ExecuteNonQuery();
                        }

                        // Update the second instructor's subject load to 'assigned'
                        string updateAssignedQuery = @"
                        UPDATE subject_load 
                        SET Status = 'assigned' 
                        WHERE Internal_Employee_Id = @instructorId2 AND Subject_Id = @subjectId";

                        using (MySqlCommand command = new MySqlCommand(updateAssignedQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@instructorId2", instructorId2);
                            command.Parameters.AddWithValue("@subjectId", subjectId);
                            command.ExecuteNonQuery();
                        }

                        // Commit the transaction
                        transaction.Commit();
                    }
                }
                return true; // Return true if the operation was successful
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }

        }

        

        public bool IsSubjectAssignedToBlockSection(int subjectId, int blockSectionId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to check if the subject is assigned to the specified block section
                    string query = @"
                    SELECT COUNT(*) 
                    FROM class 
                    WHERE Subject_Id = @subjectId AND Block_Section_Id = @blockSectionId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@subjectId", subjectId);
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                        // Execute the command and retrieve the count
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Return true if the count is greater than 0, otherwise false
                        return count > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }
        public bool IsScheduleConflict(int blockSectionId, string classDay, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to check for scheduling conflicts
                    string query = @"
                    SELECT COUNT(*) 
                    FROM class 
                    WHERE Block_Section_Id = @blockSectionId 
                    AND Class_Day = @classDay 
                    AND (
                        (Start_Time < @endTime AND End_Time > @startTime)
                    )";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                        command.Parameters.AddWithValue("@classDay", classDay);
                        command.Parameters.AddWithValue("@startTime", startTime);
                        command.Parameters.AddWithValue("@endTime", endTime);

                        // Execute the command and retrieve the count
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Return true if there is a conflict, otherwise false
                        return count > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }

        public bool IsRoomAvailable(int roomId, string classDay, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to check for room availability
                    string query = @"
                    SELECT COUNT(*) 
                    FROM class 
                    WHERE Room_Id = @roomId 
                    AND Class_Day = @classDay 
                    AND (
                        (Start_Time < @endTime AND End_Time > @startTime)
                    )";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@roomId", roomId);
                        command.Parameters.AddWithValue("@classDay", classDay);
                        command.Parameters.AddWithValue("@startTime", startTime);
                        command.Parameters.AddWithValue("@endTime", endTime);

                        // Execute the command and retrieve the count
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Return true if there is a conflict (i.e., the room is not available), otherwise false
                        return count == 0; // Return true if available (no conflicts)
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }

        public bool HasScheduleConflict(int instructorId, string classDay, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to check for scheduling conflicts
                    string query = @"
                    SELECT COUNT(*) 
                    FROM class 
                    WHERE Internal_Employee_Id = @instructorId 
                    AND Class_Day = @classDay 
                    AND (
                        (Start_Time < @endTime AND End_Time > @startTime)
                    )";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@instructorId", instructorId);
                        command.Parameters.AddWithValue("@classDay", classDay);
                        command.Parameters.AddWithValue("@startTime", startTime);
                        command.Parameters.AddWithValue("@endTime", endTime);

                        // Execute the command and retrieve the count
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // Return true if there is a conflict (i.e., the instructor is not available), otherwise false
                        return count > 0; // Return true if there is a conflict
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }



        #endregion

        private void StartTime_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //// Allow only digits and colon
            //Regex regex = new Regex(@"^[0-2]?[0-9]:[0-5][0-9]$");
            //string newText = (sender as TextBox).Text + e.Text;

            //// Validate the input
            //e.Handled = !regex.IsMatch(newText);
        }

        private void StartTime_txt_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //if (TimeSpan.TryParse(startTime_txt.Text, out TimeSpan startTime))
            //{
            //    // Add 3 hours to the start time
            //    TimeSpan endTime = startTime.Add(TimeSpan.FromHours(3));

            //    // Update the end time TextBox
            //    endTime_txt.Text = endTime.ToString(@"hh\:mm");
            //}
            //else
            //{
            //    // Clear the end time if the start time is invalid
            //    endTime_txt.Text = string.Empty;
            //}
        }

        

        private void IsTextInt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void check_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("hellooooooo");


            // Check if the subject is assigned to the block section
            if (IsSubjectAssignedToBlockSection(SubjectId, SectionId))
            {
                MessageBox.Show("Subject is already in block section.");
            }

            // Initialize TimeSpan variables
            TimeSpan startTime;
            TimeSpan endTime;

            // Try to parse the start time
            if (!TimeSpan.TryParse(startTime_txt.Text, out startTime))
            {
                MessageBox.Show("Invalid start time format. Please enter time in HH:mm format.");
            }

            // Try to parse the end time
            if (!TimeSpan.TryParse(endTime_txt.Text, out endTime))
            {
                MessageBox.Show("Invalid end time format. Please enter time in HH:mm format.");
            }

                if (IsScheduleConflict(SectionId, SelectedDay, startTime, endTime))
                {
                    MessageBox.Show("There is a time scheduling conflict.");
                }

                if (!IsRoomAvailable(RoomId, SelectedDay, startTime, endTime))
                {
                    MessageBox.Show("There is a conflict with rooms.");
                }

                if (HasScheduleConflict(InstructorId, SelectedDay, startTime, endTime))
                {
                    MessageBox.Show("There is a conflict with instructors.");
               }
            

            if (string.IsNullOrWhiteSpace(insertStubCode_txt.Text))
            {
                MessageBox.Show("Subject code cannot be empty or whitespace.");
                return; // Exit the method if the input is invalid
            }
        }

        private void switchStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            // Assuming you have a way to get the selected subject ID and block section ID from your UI
            int subjectId = SubjectId; // Implement this method to get the selected SubjectId
            int blockSectionId = SectionId; // Implement this method to get the selected BlockSectionId

            if (subjectId == 0 || blockSectionId == 0) // Check if a subject and block section are selected
            {
                MessageBox.Show("Please select a subject and a block section to switch the status.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // First, retrieve the current status
                    string currentStatus = GetCurrentStatus(connection, subjectId, blockSectionId);
                    if (currentStatus == null)
                    {
                        MessageBox.Show("Subject not found in the specified block section.");
                        return;
                    }

                    // Determine the new status
                    string newStatus = currentStatus == "assigned" ? "waiting" : "assigned";

                    // Update the status in the database
                    if (UpdateSubjectStatus(connection, subjectId, blockSectionId, newStatus))
                    {
                        MessageBox.Show($"Status updated to '{newStatus}' successfully.");
                        // Optionally, refresh the UI or reload data
                        loadUnassinedSubject(blockSectionId); // Reload the subjects if needed
                    }
                    else
                    {
                        MessageBox.Show("Failed to update the status. Please try again.");
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

        private string GetCurrentStatus(MySqlConnection connection, int subjectId, int blockSectionId)
        {
            string status = null;
            try
            {
                // SQL query to get the current status
                string query = @"
            SELECT status 
            FROM block_subject_list 
            WHERE subjectId = @subjectId AND blockSectionId = @blockSectionId";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@subjectId", subjectId);
                    command.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    status = command.ExecuteScalar()?.ToString();
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
            return status;
        }

        private bool UpdateSubjectStatus(MySqlConnection connection, int subjectId, int blockSectionId, string newStatus)
        {
            try
            {
                // SQL query to update the subject status
                string query = @"
            UPDATE block_subject_list 
            SET status = @newStatus 
            WHERE subjectId = @subjectId AND blockSectionId = @blockSectionId";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@newStatus", newStatus);
                    command.Parameters.AddWithValue("@subjectId", subjectId);
                    command.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    // Execute the command and return true if the update was successful
                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
                return false; // Return false in case of an error
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false; // Return false in case of an error
            }
        }

        private void manualAssign_btn_Click(object sender, RoutedEventArgs e)
        {
            // Assuming you have TextBoxes or other UI elements to get the IDs
            int internalEmployeeId = InstructorId; // Implement this method to get the Internal Employee ID
            int subjectId = SubjectId; // Implement this method to get the Subject ID

            if (internalEmployeeId == 0 || subjectId == 0) // Check if both IDs are valid
            {
                MessageBox.Show("Please provide valid Internal Employee ID and Subject ID.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to update the subject load status to 'assigned'
                    string updateAssignedQuery = @"
                UPDATE subject_load 
                SET Status = 'assigned' 
                WHERE Internal_Employee_Id = @internalEmployeeId 
                AND Subject_Id = @subjectId 
                AND Status = 'waiting'"; // Only update if the current status is 'waiting'

                    using (MySqlCommand command = new MySqlCommand(updateAssignedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@internalEmployeeId", internalEmployeeId);
                        command.Parameters.AddWithValue("@subjectId", subjectId);

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Subject load status updated to 'assigned' successfully.");
                            // Optionally, refresh the UI or reload data
                            loadScheduleByBlock(SectionId); // Implement this method to refresh the subject load list
                        }
                        else
                        {
                            MessageBox.Show("No matching subject load found or status is not 'waiting'.");
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

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            
            // Assuming you have a way to get the selected class ID from your DataGrid
            int classId = StubCode; // Implement this method to get the selected ClassId
            if (classId == 0) // Check if a class is selected
            {
                MessageBox.Show("Please select a class to remove.");
                return;
            }
                // Show the conflict messages
                MessageBoxResult result = MessageBox.Show(
                    "Do you want to continue?",
                    "delete stub code: '"+classId+"'?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

            // Check the user's response
            if (result == MessageBoxResult.No)
            {
                return; // Exit the method if the user chooses not to continue
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to delete the class entry
                    string deleteQuery = @"
                DELETE FROM class 
                WHERE Stub_Code = @classId"; // Assuming Class_Id is the primary key

                    using (MySqlCommand command = new MySqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@classId", classId);

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Class removed successfully.");
                            // Optionally, refresh the UI or reload data
                            loadScheduleByBlock(SectionId); // Implement this method to refresh the class list
                        }
                        else
                        {
                            MessageBox.Show("No class found with the specified ID.");
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


        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello");

            // Convert IDs to integers
            int subjectId = SubjectId; // Assuming SubjectId is already an int
            int instructorId = InstructorId; // Assuming InstructorId is already an int
            int section = SectionId; // Assuming SectionId is already an int
            int roomId = RoomId; // Assuming RoomId is already an int
            string stubCode = insertStubCode_txt.Text; // Assuming this is a string
            string classMode = "Face_To_Face";
            string day = SelectedDay.ToString();
            string start = startTime_txt.Text;
            string end = endTime_txt.Text;
            start = start + "00";
            end = end + "00";

            // Initialize TimeSpan variables
            TimeSpan startTime;
            TimeSpan endTime;

            // Try to parse the start time
            if (!TimeSpan.TryParse(startTime_txt.Text, out startTime))
            {
                MessageBox.Show("Invalid start time format. Please enter time in HH:mm format.");
                return; // Exit if parsing fails
            }

            // Try to parse the end time
            if (!TimeSpan.TryParse(endTime_txt.Text, out endTime))
            {
                MessageBox.Show("Invalid end time format. Please enter time in HH:mm format.");
                return; // Exit if parsing fails
            }

            // Check for scheduling conflicts
            if (IsScheduleConflict(section, day, startTime, endTime))
            {
                MessageBox.Show("There is a time scheduling conflict.");
                return; // Exit if there is a scheduling conflict
            }

            if (day_cmbx.SelectedIndex == 0)
            {
                MessageBox.Show("Please Select A Week Day");
                return;
            }

            if (!IsRoomAvailable(roomId, day, startTime, endTime))
            {
                MessageBox.Show("There is a conflict with rooms.");
                return; // Exit if there is a room conflict
            }

            if (HasScheduleConflict(instructorId, day, startTime, endTime))
            {
                MessageBox.Show("There is a conflict with instructors.");
                return; // Exit if there is an instructor conflict
            }

            // Check if the stub code is empty or whitespace
            if (string.IsNullOrWhiteSpace(stubCode))
            {
                MessageBox.Show("Subject code cannot be empty or whitespace.");
                return; // Exit if the input is invalid
            }

            description_txt.Text = roomId.ToString();

            // Proceed with the rest of your logic if there are no conflicts
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO class 
                (Subject_Id, Internal_Employee_Id, Block_Section_Id, Room_Id, Stub_Code, Class_Mode, Class_Day, Start_Time, End_Time) 
                VALUES 
                (@SubjectId, @InternalEmployeeId, @BlockSectionId, @RoomId, @StubCode, @ClassMode, @ClassDay, @StartTime, @EndTime)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@SubjectId", subjectId);
                        command.Parameters.AddWithValue("@InternalEmployeeId", instructorId);
                        command.Parameters.AddWithValue("@BlockSectionId", section);
                        command.Parameters.AddWithValue("@RoomId", roomId);
                        command.Parameters.AddWithValue("@StubCode", stubCode);
                        command.Parameters.AddWithValue("@ClassMode", classMode);
                        command.Parameters.AddWithValue("@ClassDay", day);
                        command.Parameters.AddWithValue("@StartTime", start);
                        command.Parameters.AddWithValue("@EndTime", end);

                        // Execute query
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Added successfully");
                            description_txt.Text = day;
                        }
                        else
                        {
                            MessageBox.Show("No records were added.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., log the error)
                // Log the error message and stack trace for debugging
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show("An error occurred while adding the class: " + ex.Message);
                MessageBox.Show(ex.StackTrace);
            }
        }
    }
}

using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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

namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for Assignment.xaml
    /// </summary>
    /// 
    public partial class Assignment : Page
    {

        public int DepartmentId { get; set; }

        public int EmployeeId { get; set; } 

        public int SubjectId { get; set; }

        //public static readonly string ConnectionString = @"Server=26.182.137.35;Database=universitydb;User ID=test;Password=;";
        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        public Assignment()
        {
            InitializeComponent();
            LoadUI();
        }

        //UI
        #region UI 
        private void LoadUI()
        {
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Assignment Menu", TopBar_BackButtonClicked);
            LoadInstructors();
            LoadDepartmentitems();
            loadSchedule();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new MainMenu());
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



        #endregion

        //Instructor
        #region Instructor

        private DataTable instructorDataTable; // Store the loaded data

        private void LoadInstructors(string filter = "No Assignments")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Base query to load instructors with relevant subject status
                    string query = @"
            SELECT i.Internal_Employee_Id, 
                   i.Employee_Id AS EmployeeId,
                   d.Dept_Code AS Department,
                   CONCAT(i.Lname, ', ', i.Fname, ' ', IFNULL(i.Mname, '')) AS FullName
            FROM instructor i
            JOIN departments d ON i.Dept_Id = d.Dept_Id
            WHERE i.Status = 1";

                    // Apply the filter logic based on subject load status
                    switch (filter)
                    {
                        case "No Assignments":
                            // Only instructors with "waiting" subjects, but no "assigned" subjects
                            query += @" AND EXISTS (
                                    SELECT 1 
                                    FROM subject_load s 
                                    WHERE s.Internal_Employee_Id = i.Internal_Employee_Id 
                                      AND s.Status = 'waiting')
                                AND NOT EXISTS (
                                    SELECT 1 
                                    FROM subject_load s 
                                    WHERE s.Internal_Employee_Id = i.Internal_Employee_Id 
                                      AND s.Status = 'assigned')";
                            break;

                        case "Partial Assignments":
                            // Instructors who have both "waiting" and "assigned" subjects
                            query += @" AND EXISTS (
                                    SELECT 1 
                                    FROM subject_load s 
                                    WHERE s.Internal_Employee_Id= i.Internal_Employee_Id 
                                      AND s.Status = 'waiting')
                                AND EXISTS (
                                    SELECT 1 
                                    FROM subject_load s 
                                    WHERE s.Internal_Employee_Id = i.Internal_Employee_Id 
                                      AND s.Status = 'assigned')";
                            break;

                        case "Complete Assignments":
                            // Only instructors with "assigned" subjects and no "waiting" subjects
                            query += @" AND EXISTS (
                                        SELECT 1 
                                        FROM subject_load s 
                                        WHERE s.Internal_Employee_Id = i.Internal_Employee_Id 
                                            AND s.Status = 'assigned')
                                    AND NOT EXISTS (
                                        SELECT 1 
                                        FROM subject_load s 
                                        WHERE s.Internal_Employee_Id = i.Internal_Employee_Id 
                                        AND s.Status = 'waiting')";
                            break;

                        case "all":
                        default:
                            // No additional filter, show all instructors with subject load
                            query += @" AND EXISTS (
                                    SELECT 1 
                                    FROM subject_load s 
                                    WHERE s.Internal_Employee_Id = i.Internal_Employee_Id)";
                            break;
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    instructorDataTable = new DataTable(); // Initialize the DataTable
                    dataAdapter.Fill(instructorDataTable); // Fill the DataTable

                    // Bind the resulting data to the DataGrid
                    instructor_data.ItemsSource = instructorDataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }



        private void instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                // Get the Internal_Employee_Id from the selected row
                EmployeeId = (int)selectedRow["Internal_Employee_Id"];
                employeeId_txt.Text = (string)selectedRow["EmployeeId"];
                loadScheduleByInstructor(EmployeeId);


                // Set the value of the employee_id textbox
                employee_id.Text = EmployeeId.ToString();

                // Load instructor subjects for the selected employee
                LoadInstructorSubjects(EmployeeId);
            }
        }

        private void instructorSubject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructorSubject_data.SelectedItem is DataRow selectedRow) 
            {
                SubjectId = (int)selectedRow["Instructor_Subject_Id"];
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
                if (instructorDataTable != null)
                {
                    DataView view = new DataView(instructorDataTable);

                    if (selectedDeptCode != "All") // If the selected value is not "All"
                    {
                        view.RowFilter = $"Department = '{selectedDeptCode}'"; // Filter by department code
                    }
                    else
                    {
                        view.RowFilter = string.Empty; // Show all if "All" is selected
                    }

                    instructor_data.ItemsSource = view; // Bind the filtered view to the DataGrid
                }
            }
            else
            {
                // If nothing is selected, reset the DataGrid to show all data
                instructor_data.ItemsSource = instructorDataTable.DefaultView;
            }
        }

        private void instructorAssignment_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructorAssignment_cmbx.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedFilter = selectedItem.Content.ToString();
                LoadInstructors(selectedFilter); // Pass the filter to the LoadClasses method
            }
        }

        #endregion

        #region //SUbject

        private void LoadInstructorSubjects(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT sl.ID AS SubjectLoadId, 
                        s.Subject_Code, 
                        s.Subject_Title, 
                        sl.Max_Student, 
                        sl.Status
                    FROM subject_load sl
                    JOIN subjects s ON sl.Subject_Id = s.Subject_Id
                    WHERE sl.Internal_Employee_Id = @internalEmployeeId"; // Change this line to filter by Employee_Id

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@internalEmployeeId", internalEmployeeId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    instructorSubject_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor subjects: " + ex.Message);
            }
        }

        private void subjectFilter_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the ComboBox has a valid selection
            if (subjectFilter_cmbx.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedStatus = selectedItem.Tag.ToString(); // Get the tag for filtering

                // Use DataView to filter the DataTable
                if (instructorSubject_data.ItemsSource is DataView view)
                {
                    if (selectedStatus != "All") // If the selected value is not "All"
                    {
                        view.RowFilter = $"Status = '{selectedStatus}'"; // Adjust 'Status' based on your actual column name
                    }
                    else
                    {
                        view.RowFilter = string.Empty; // Show all if "All" is selected
                    }

                    instructorSubject_data.ItemsSource = view; // Bind the filtered view to the DataGrid
                }
            }
        }








        #endregion

        #region //sechedule

        private void loadSchedule()
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
                       c.Class_Mode,
                       c.Class_Day,
                       c.Start_Time,
                       c.End_Time,
                       s.Subject_Code AS SubjectCode,
                       CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
                       r.Room_Code AS RoomCode
                FROM class c
                LEFT JOIN subjects s ON c.Subject_Id = s.Subject_Id
                LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
                LEFT JOIN rooms r ON c.Room_Id = r.Room_Id";

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

        private void loadScheduleByInstructor(int EmployeeId)
        {
            string employeeId = EmployeeId.ToString();
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
                       c.Class_Mode,
                       c.Class_Day,
                       c.Start_Time,
                       c.End_Time,
                       s.Subject_Code AS SubjectCode,
                       CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
                       r.Room_Code AS RoomCode
                FROM class c
                LEFT JOIN subjects s ON c.Subject_Id = s.Subject_Id
                LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
                LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
                Where c.Internal_Employee_Id = @EmployeeId";


                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@EmployeeId", employeeId);
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
        private void loadSchedule_btn_Click(object sender, RoutedEventArgs e)
        {
            loadSchedule();

        }

        #endregion

        #region //algorithm

        //------------------------------------------------------------------------------------------------------------------------------------
        public class MatchMinimum
        {
            public Dictionary<int, int> CalculateMatchCounts(List<(int, int)> roomList, List<int> parameter)
            {
                // Define a dictionary to count the matches for each identifier
                try
                {
                    Dictionary<int, int> matchCountByIdentifier = new Dictionary<int, int>();

                    // Iterate over roomList and check for numbers in parameter that are less than or equal to the second number in roomList
                    foreach (var item in roomList)
                    {
                        int identifier = item.Item1;
                        int valueToMatch = item.Item2;
                        int count = 0;

                        foreach (int number in parameter)
                        {
                            if (valueToMatch >= number)
                            {
                                count++;
                            }
                        }

                        // Accumulate the match count for the identifier
                        if (matchCountByIdentifier.ContainsKey(identifier))
                        {
                            matchCountByIdentifier[identifier] += count;
                        }
                        else
                        {
                            matchCountByIdentifier[identifier] = count;
                        }
                    }

                    return matchCountByIdentifier;
                }
                catch (Exception)
                {
                    MessageBox.Show("error match minimum dictionary");
                    throw;
                }
            }

            public int FindMaxMatches(Dictionary<int, int> matchCountByIdentifier)
            {
                try
                {
                    // Find the maximum total match count
                    return matchCountByIdentifier.Values.Max();
                }
                catch (Exception)
                {
                    MessageBox.Show("error find max matches");
                    throw;
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        public class MatchDefinite
        {
            public Dictionary<int, int> CalculateMatchCounts(List<(int, int)> roomList, List<int> parameter)
            {
                // Define a dictionary to count the matches for each identifier
                try
                {
                    Dictionary<int, int> matchCountByIdentifier = new Dictionary<int, int>();

                    // Iterate over roomList and check for numbers in parameter that are less than or equal to the second number in roomList
                    foreach (var item in roomList)
                    {
                        int identifier = item.Item1;
                        int valueToMatch = item.Item2;
                        int count = 0;

                        foreach (int number in parameter)
                        {
                            if (valueToMatch >= number)
                            {
                                count++;
                            }
                        }

                        // Accumulate the match count for the identifier
                        if (matchCountByIdentifier.ContainsKey(identifier))
                        {
                            matchCountByIdentifier[identifier] += count;
                        }
                        else
                        {
                            matchCountByIdentifier[identifier] = count;
                        }
                    }

                    return matchCountByIdentifier;
                }
                catch (Exception)
                {
                    MessageBox.Show("error match Definite");
                    throw;
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        public class FilterFloor
        {
            private string connectionString;

            public FilterFloor(string dbConnectionString)
            {
                connectionString = dbConnectionString;
            }

            // Method to filter room floors based on disability status
            public List<(int, int)> FilterRoomsByDisability(string employeeId, List<(int, int)> roomIdAndFloorList)
            {
                int disability = GetDisabilityStatus(employeeId);

                if (disability == -1)
                {
                    // Handle case where employee not found, or an error occurred
                    return new List<(int, int)>();
                }

                // If disability is 1, try to find the lowest available floor starting from floor 1
                if (disability == 1)
                {
                    return GetLowestAvailableFloors(roomIdAndFloorList);
                }

                // If disability is 0, return all room floors
                return roomIdAndFloorList;
            }

            // Method to get disability status for the given employee id
            private int GetDisabilityStatus(string employeeId)
            {
                try
                {
                    int disability = -1; // Default to -1 to indicate not found

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();


                    // Query to get disability status from the instructor table
                    string query = "SELECT Disability FROM instructor WHERE Internal_Employee_Id = @EmployeeId";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                disability = reader.GetInt32("Disability"); // Read the disability status
                            }
                        }
                    }

                    return disability;
                }
                catch (Exception)
                {
                    MessageBox.Show("error Get disability Status");
                    throw;
                }
            }

            // Method to get the lowest available floors, starting from floor 1 and going up
            private List<(int, int)> GetLowestAvailableFloors(List<(int, int)> roomIdAndFloorList)
            {
                int currentFloor = 1; // Start from the first floor
                List<(int, int)> filteredRooms = new List<(int, int)>();

                while (filteredRooms.Count == 0)
                {
                    // Filter rooms for the current floor
                    filteredRooms = roomIdAndFloorList.FindAll(room => room.Item2 == currentFloor);

                    if (filteredRooms.Count > 0)
                    {
                        return filteredRooms; // Return rooms if found
                    }

                    currentFloor++; // Increment the floor and try again
                }

                return filteredRooms; // This will return the rooms for the lowest available floor
            }
        }

        public class RoomFinder
        {
            private string connectionString;

            public RoomFinder(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<int> FindRooms(string subjectId, List<(int, string)> roomList_test)
            {
                try
                {
                    List<int> matchingRoomIds = new List<int>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Lecture_Lab attribute from the subjects table
                        string query = @"SELECT Lecture_Lab FROM subjects WHERE Subject_Id = @subjectId";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@subjectId", subjectId);

                        string lectureLabAttribute = command.ExecuteScalar()?.ToString();

                        if (!string.IsNullOrEmpty(lectureLabAttribute))
                        {
                            // Separate function calls based on whether it's 'LEC' or 'LAB'
                            if (lectureLabAttribute == "LEC")
                            {
                                matchingRoomIds = FindLectureRooms(roomList_test);
                            }
                            else if (lectureLabAttribute == "LAB")
                            {
                                matchingRoomIds = FindLaboratoryOrAvrRooms(roomList_test);
                            }
                        }
                    }

                    return matchingRoomIds;
                }
                catch (Exception)
                {
                    MessageBox.Show("error Find rooms");
                    throw;
                }
            }

            private List<int> FindLectureRooms(List<(int, string)> roomList_test)
            {
                try
                {
                    if (room.Item2.Contains("LEC"))
                    {
                        if (room.Item2.Contains("LEC"))
                        {
                            lectureRoomIds.Add(room.Item1);
                        }
                    }

                    return lectureRoomIds;
                }
                catch (Exception)
                {
                    MessageBox.Show("error Find Lecture Rooms");
                    throw;
                }
            }

            private List<int> FindLaboratoryOrAvrRooms(List<(int, string)> roomList_test)
            {
                try
                {
                    if (room.Item2.Contains("LAB") || room.Item2.Contains("AVR"))
                    {
                        if (room.Item2.Contains("LAB") || room.Item2.Contains("AVR"))
                        {
                            matchingRoomIds.Add(room.Item1);
                        }
                    }

                    return matchingRoomIds;
                }
                catch (Exception)
                {
                    MessageBox.Show("error find lab or avr");
                    throw;
                }
            }
        }

        public class ConvertToWeight
        {
            public List<(int, int)> TransformRoomList(List<int> roomlist)
            {
                List<(int, int)> transformedList = new List<(int, int)>();

                foreach (int roomId in roomlist)
                {
                    transformedList.Add((roomId, 1));
                }

                return transformedList;
            }
        }

        public class RoomFetcher
        {
            private string connectionString;

            public RoomFetcher(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<int> GetRoomIds()
            {
                try
                {
                    List<int> roomList_1 = new List<int>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get only Room_Id from rooms table
                        string query = "SELECT Room_Id FROM rooms";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomId = reader.GetInt32("Room_Id");

                                // Add Room_Id to the list
                                roomList_1.Add(roomId);
                            }
                        }
                    }

                    return roomList_1;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get room ids");
                    throw;
                }
            }
        }

        public class DeptFetcher
        {
            private string connectionString;

            // Constructor to initialize the connection string
            public DeptFetcher(string connectionString)
            {
                this.connectionString = connectionString;
            }

            // Method to fetch Dept_Id based on Subject_Id
            public int GetDeptIdBySubject(int subjectId)
            {
                try
                {
                    int deptId = -1; // Initialize to -1 to handle invalid cases

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "SELECT Dept_Id FROM subjects WHERE Subject_Id = @SubjectId";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@SubjectId", subjectId);
                            object result = command.ExecuteScalar();

                            if (result != null)
                            {
                                deptId = Convert.ToInt32(result); // Get the Dept_Id from the query result
                            }
                        }
                    }

                    return deptId;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get dept id by subject");
                    throw;
                }
            }
        }
        public class BuildingFetcher
        {
            private string connectionString;

            // Constructor to initialize the connection string
            public BuildingFetcher(string connectionString)
            {
                this.connectionString = connectionString;
            }

            // Method to fetch all Building_Id values for a given Dept_Id
            public List<int> GetBuildingIdsByDeptId(int deptId)
            {
                try
                {
                    List<int> buildingIds = new List<int>(); // Initialize the list to store Building_Id values

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "SELECT Building_Id FROM departments WHERE Dept_Id = @DeptId";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@DeptId", deptId);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int buildingId = reader.GetInt32("Building_Id");
                                    buildingIds.Add(buildingId); // Add each Building_Id to the list
                                }
                            }
                        }
                    }

                    return buildingIds;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get building id by dept id");
                    throw;
                } // Return the list of Building_Id values
            }
        }

        public class RoomIdFetcher
        {
            private string connectionString;

            // Constructor to initialize the connection string
            public RoomIdFetcher(string connectionString)
            {
                this.connectionString = connectionString;
            }

            // Method to fetch Room_Id values that match the given Building_Id list
            public List<int> GetRoomIdsByBuildingIds(List<int> buildingIds)
            {
                try
                {
                    List<int> roomIds = new List<int>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Convert buildingIds to a comma-separated string for SQL IN clause
                        string buildingIdsParam = string.Join(",", buildingIds);

                        string query = $"SELECT Room_Id FROM rooms WHERE Building_Id IN ({buildingIdsParam})";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int roomId = reader.GetInt32("Room_Id");
                                    roomIds.Add(roomId); // Add each Room_Id to the list
                                }
                            }
                        }
                    }

                    return roomIds;
                }
                catch (Exception)
                {
                    MessageBox.Show("error room id fetcher");
                    throw;
                } // Return the list of Room_Id values
            }
        }

        public class RoomWeightSelector
        {
            private Random random = new Random();

            // Function to sort the list using bubble sort
            private void BubbleSort(List<(int Room_Id, int Weight)> transformedList)
            {
                int n = transformedList.Count;
                bool swapped;
                for (int i = 0; i < n - 1; i++)
                {
                    swapped = false;
                    for (int j = 0; j < n - i - 1; j++)
                    {
                        if (transformedList[j].Weight < transformedList[j + 1].Weight)
                        {
                            // Swap the elements if the weight of the current one is less than the next
                            var temp = transformedList[j];
                            transformedList[j] = transformedList[j + 1];
                            transformedList[j + 1] = temp;
                            swapped = true;
                        }
                    }
                    // If no elements were swapped in this inner loop, the list is sorted
                    if (!swapped) break;
                }
            }

            public int ChooseRoomWithHighestWeight(List<(int Room_Id, int Weight)> transformedList)
            {
                // Sort the list using bubble sort based on the weight (descending)
                BubbleSort(transformedList);

                // Find the highest weight (the first element after sorting will have the highest weight)
                int highestWeight = transformedList[0].Weight;

                // Get all Room_Ids with the highest weight
                var highestWeightRooms = transformedList
                    .Where(x => x.Weight == highestWeight)
                    .Select(x => x.Room_Id)
                    .ToList();

                // If there are multiple rooms with the highest weight, randomly select one
                if (highestWeightRooms.Count > 1)
                {
                    int randomIndex = random.Next(highestWeightRooms.Count);
                    return highestWeightRooms[randomIndex];
                }
                else
                {
                    // Return the only Room_Id with the highest weight
                    return highestWeightRooms.First();
                }
            }
        }


        public class DateTimeAssigner
        {
            private string connectionString;

            public DateTimeAssigner(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<(int RoomId, string Day, TimeSpan StartTime, TimeSpan EndTime)> AssignDateTimes(int selectedRoom, int subjectId, int employeeId_num)
            {
                try
                {
                    List<(int, string, TimeSpan, TimeSpan)> scheduleList = new List<(int, string, TimeSpan, TimeSpan)>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Units from the subjects table
                        string subjectQuery = "SELECT Units FROM subjects WHERE Subject_Id = @subjectId";
                        MySqlCommand subjectCommand = new MySqlCommand(subjectQuery, connection);
                        subjectCommand.Parameters.AddWithValue("@subjectId", subjectId);

                    // Query to get the Start_Time using Internal_Employee_Id from instructor_availability table
                    string availabilityQuery = "SELECT Start_Time FROM instructor_availability WHERE Internal_Employee_Id = @employeeId";
                    MySqlCommand availabilityCommand = new MySqlCommand(availabilityQuery, connection);
                    availabilityCommand.Parameters.AddWithValue("@employeeId", employeeId_num);

                        // Query to get the Start_Time using Internal_Employee_Id from instructor_availability table
                        string availabilityQuery = "SELECT Start_Time FROM instructor_availability WHERE Internal_Employee_Id = @employeeId";
                        MySqlCommand availabilityCommand = new MySqlCommand(availabilityQuery, connection);
                        availabilityCommand.Parameters.AddWithValue("@employeeId", employeeId_num);

                        // Get the Start_Time from the result
                        TimeSpan startTime = (TimeSpan)availabilityCommand.ExecuteScalar();
                        string[] days = { "Monday", "Wednesday" }; // Use Monday and Wednesday as default days

                        if (units == 1)
                        {
                            // Assign a single 1-hour slot
                            TimeSpan endTime = startTime.Add(TimeSpan.FromHours(1));
                            scheduleList.Add((selectedRoom, days[0], startTime, endTime));
                        }
                        else if (units > 1)
                        {
                            // Calculate evenly split times
                            TimeSpan partDuration = TimeSpan.FromHours(units / 2.0);

                            // Assign first part
                            TimeSpan endTime = startTime.Add(partDuration);
                            scheduleList.Add((selectedRoom, days[0], startTime, endTime));

                            // Move to the next day
                            startTime = (TimeSpan)availabilityCommand.ExecuteScalar(); // Reset to the same Start_Time for the next day
                            endTime = startTime.Add(partDuration);
                            scheduleList.Add((selectedRoom, days[1], startTime, endTime));
                        }
                    }

                    return scheduleList;
                }
                catch (Exception)
                {
                    MessageBox.Show("error date time assigner");
                    throw;
                }
            }
        }

        public class ClassScheduler
        {
            private string connectionString;

            public ClassScheduler(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void InsertScheduleIntoDatabase(int subjectId, int employeeId, List<(int RoomId, string Day, TimeSpan StartTime, TimeSpan EndTime)> scheduleList)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        string query = @"INSERT INTO class (Subject_Id, Internal_Employee_Id, Room_Id, Class_Day, Start_Time, End_Time)
                                 VALUES (@SubjectId, @EmployeeId, @RoomId, @ClassDay, @StartTime, @EndTime)";

                        foreach (var schedule in scheduleList)
                        {
                            string query = @"INSERT INTO class (Subject_Id, Internal_Employee_Id, Room_Id, Class_Day, Start_Time, End_Time)
                                 VALUES (@SubjectId, @EmployeeId, @RoomId, @ClassDay, @StartTime, @EndTime)";

                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@SubjectId", subjectId);
                                command.Parameters.AddWithValue("@EmployeeId", employeeId);
                                command.Parameters.AddWithValue("@RoomId", schedule.RoomId);
                                command.Parameters.AddWithValue("@ClassDay", schedule.Day);
                                command.Parameters.AddWithValue("@StartTime", schedule.StartTime);
                                command.Parameters.AddWithValue("@EndTime", schedule.EndTime);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("error class scheduler");
                    throw;
                }
            }
        }

        public class TimeSpanClassifier
        {
            public static List<(TimeSpan, string)> ClassifyTimeSpans(List<TimeSpan> timeSpans)
            {
                List<(TimeSpan, string)> result = new List<(TimeSpan, string)>();

                TimeSpan threeHours = new TimeSpan(3, 0, 0);
                TimeSpan oneHourThirty = new TimeSpan(1, 30, 0);
                TimeSpan oneHour = new TimeSpan(1, 0, 0);

                foreach (var timeSpan in timeSpans)
                {
                    string weekday;
                    // Check for multiples of 3:00:00
                    if (timeSpan.Ticks % threeHours.Ticks == 0)
                    {
                        weekday = "MTWTHF"; // Multiple of 3 hours
                    }
                    // Check for multiples of 1:30:00, except those that are multiples of 3:00:00
                    else if (timeSpan.Ticks % oneHourThirty.Ticks == 0 && timeSpan.Ticks % threeHours.Ticks != 0)
                    {
                        weekday = "TTH"; // Multiple of 1 hour 30 minutes, but not 3 hours
                    }
                    // Check for multiples of 1:00:00 that are not multiples of 1:30:00
                    else if (timeSpan.Ticks % oneHour.Ticks == 0 && timeSpan.Ticks % oneHourThirty.Ticks != 0)
                    {
                        weekday = "MWF"; // Multiple of 1 hour but not 1 hour 30 minutes
                    }
                    else
                    {
                        weekday = "None"; // Does not match any condition
                    }

                    // Add the TimeSpan and its corresponding weekday to the result list
                    result.Add((timeSpan, weekday));
                }

                return result;
            }
        }

        public class TimeSlotGenerator
        {
            private string connectionString;
            private int employeeId_num;

            // Constructor to accept the connection string
            public TimeSlotGenerator(string connectionString, int employeeId)
            {
                this.connectionString = connectionString;
                this.employeeId_num = employeeId;
            }

            public bool IsStubCodeExistsForDay(int stubCode, string day)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to check if the Stub_Code exists for the given day
                        string query = "SELECT COUNT(*) FROM class WHERE Stub_Code = @StubCode AND Class_Day = @Day";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@StubCode", stubCode);
                        cmd.Parameters.AddWithValue("@Day", day);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        // Return true if a record with the same Stub_Code exists for the day, otherwise false
                        return count > 0;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(" error timeslot generator");
                    throw;
                }
            }

            public List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> GenerateAvailableTimeSlots(TimeSpan interval, string days, int selectedRoom, int stubCode)
            {
                List<(TimeSpan, TimeSpan, string, int)> availableSlots = new List<(TimeSpan, TimeSpan, string, int)>();
                List<string> dayList = new List<string>();

                // Populate dayList based on the input 'days' (e.g., MTWTHF, TTH, MWF)
                switch (days)
                {
                    case "MTWTHF":
                        dayList.AddRange(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" });
                        break;
                    case "TTH":
                        dayList.AddRange(new[] { "Tuesday", "Thursday" });
                        break;
                    case "MWF":
                        dayList.AddRange(new[] { "Monday", "Wednesday", "Friday" });
                        break;
                    default:
                        return availableSlots;
                }

                // Loop through each day to find available slots
                foreach (var day in dayList)
                {
                    // Check if the Stub_Code already exists for this day
                    if (IsStubCodeExistsForDay(stubCode, day))
                    {
                        // If Stub_Code exists, skip to the next day
                        continue;
                    }

                    // Otherwise, proceed with generating the time slots
                    List<(TimeSpan startTime, TimeSpan endTime)> dayTimeSlots = GetTimeSlotsForDay(interval, day);

                    // Remove slots occupied by Room_Id
                    RemoveOccupiedSlotsByRoom(ref dayTimeSlots, day, selectedRoom);

                    // Remove slots occupied by Internal_Employee_Id
                    RemoveOccupiedSlotsByEmployee(ref dayTimeSlots, day);

                    // Check instructor availability (Internal_Employee_Id in instructor_availability)
                    List<(TimeSpan startTime, TimeSpan endTime)> filteredSlots = CheckInstructorAvailability(dayTimeSlots);

                    // If any slots are available within the instructor's availability, add them
                    if (filteredSlots.Count > 0)
                    {
                        foreach (var slot in filteredSlots)
                        {
                            availableSlots.Add((slot.startTime, slot.endTime, day, 1)); // Adds 1 as the additionalNumber
                        }

                        break; // Stop if available slots are found for the current day
                    }
                }

                return availableSlots;
            }

            private List<(TimeSpan startTime, TimeSpan endTime)> GetTimeSlotsForDay(TimeSpan interval, string day)
            {
                List<(TimeSpan, TimeSpan)> timeSlots = new List<(TimeSpan, TimeSpan)>();

                // First time block (7:00 to 17:30)
                GenerateSlotsForDay(interval, day, new TimeSpan(7, 0, 0), new TimeSpan(17, 30, 0), timeSlots);

                // Second time block (17:40 to 20:40)
                GenerateSlotsForDay(interval, day, new TimeSpan(17, 40, 0), new TimeSpan(20, 40, 0), timeSlots);

                return timeSlots;
            }

            private void GenerateSlotsForDay(TimeSpan interval, string day, TimeSpan start, TimeSpan end, List<(TimeSpan startTime, TimeSpan endTime)> timeSlots)
            {
                TimeSpan currentTime = start;

                while (currentTime.Add(interval) <= end)
                {
                    timeSlots.Add((currentTime, currentTime.Add(interval)));
                    currentTime = currentTime.Add(interval);
                }
            }

            private void RemoveOccupiedSlotsByRoom(ref List<(TimeSpan startTime, TimeSpan endTime)> availableSlots, string day, int selectedRoom)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to find the occupied time slots for the room on the given day
                        string query = "SELECT Start_Time, End_Time FROM class WHERE Room_Id = @RoomId AND Class_Day = @Day";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@RoomId", selectedRoom);
                        cmd.Parameters.AddWithValue("@Day", day);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TimeSpan startTime = (TimeSpan)reader["Start_Time"];
                                TimeSpan endTime = (TimeSpan)reader["End_Time"];

                                // Remove occupied slots from availableSlots
                                availableSlots.RemoveAll(slot => slot.startTime < endTime && slot.endTime > startTime);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("error remove occupied slots by room");
                    throw;
                }
            }

            private void RemoveOccupiedSlotsByEmployee(ref List<(TimeSpan startTime, TimeSpan endTime)> availableSlots, string day)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                    // Query to find the occupied time slots for the employee on the given day
                    string query = "SELECT Start_Time, End_Time FROM class WHERE Internal_Employee_Id = @EmployeeId AND Class_Day = @Day";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);
                    cmd.Parameters.AddWithValue("@Day", day);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TimeSpan startTime = (TimeSpan)reader["Start_Time"];
                                TimeSpan endTime = (TimeSpan)reader["End_Time"];

                                // Remove occupied slots from availableSlots
                                availableSlots.RemoveAll(slot => slot.startTime < endTime && slot.endTime > startTime);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("error remove occupied slots by employee");
                    throw;
                }
            }

            private List<(TimeSpan startTime, TimeSpan endTime)> CheckInstructorAvailability(List<(TimeSpan startTime, TimeSpan endTime)> availableSlots)
            {
                try
                {
                    connection.Open();

                    // Query to find the availability for the employee (instructor_availability table)
                    string query = "SELECT Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to find the availability for the employee (instructor_availability table)
                        string query = "SELECT Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TimeSpan availableStartTime = (TimeSpan)reader["Start_Time"];
                                TimeSpan availableEndTime = (TimeSpan)reader["End_Time"];

                                // Keep slots that fall within the instructor's availability
                                foreach (var slot in availableSlots)
                                {
                                    if (slot.startTime >= availableStartTime && slot.endTime <= availableEndTime)
                                    {
                                        filteredSlots.Add(slot);
                                    }
                                }
                            }
                        }
                    }

                    return filteredSlots;
                }
                catch (Exception)
                {
                    MessageBox.Show("error check instructor availability");
                    throw;
                }
            }
        }


        public class ClassInserter
        {
            private string connectionString;

            public ClassInserter(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void InsertClass(TimeSpan startTime, TimeSpan endTime, string day, string mode, int stubCode, int employeeId, int subjectId, int roomId)
            {
                string query = @"INSERT INTO class (Internal_Employee_Id, Subject_Id, Room_Id, Start_Time, End_Time, Class_Day, Class_Mode, Stub_Code)
                         VALUES (@Internal_Employee_Id, @Subject_Id, @Room_Id, @Start_Time, @End_Time, @Class_Day, @Class_Mode, @Stub_Code);";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = @"INSERT INTO class (Internal_Employee_Id, Subject_Id, Room_Id, Start_Time, End_Time, Class_Day, Class_Mode, Stub_Code)
                         VALUES (@Internal_Employee_Id, @Subject_Id, @Room_Id, @Start_Time, @End_Time, @Class_Day, @Class_Mode, @Stub_Code);";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@Internal_Employee_Id", employeeId);
                            cmd.Parameters.AddWithValue("@Subject_Id", subjectId);
                            cmd.Parameters.AddWithValue("@Room_Id", roomId);
                            cmd.Parameters.AddWithValue("@Start_Time", startTime);
                            cmd.Parameters.AddWithValue("@End_Time", endTime);
                            cmd.Parameters.AddWithValue("@Class_Day", day);
                            cmd.Parameters.AddWithValue("@Class_Mode", mode); // Insert mode
                            cmd.Parameters.AddWithValue("@Stub_Code", stubCode); // Insert stubCode

                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Class successfully inserted.");
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("error insert class");
                    throw;
                }
            }
        }

        public class RoomWeightSorter
        {
            // Method to sort the list by weight in descending order and return only roomIds
            public List<int> SortByWeightDescending(List<(int roomId, int weight)> transformedList)
            {
                // Sort by weight in descending order and select only roomIds
                return transformedList.OrderByDescending(item => item.weight)
                                      .Select(item => item.roomId)
                                      .ToList();
            }
        }


        public class SubjectMode
        {
            private string connectionString;

            // Constructor to initialize the connection string
            public SubjectMode(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public string GetSubjectMode(int subjectId_num)
            {
                try
                {
                    string mode = string.Empty;

                    // Query to get Online_Capable from the subjects table
                    string query = "SELECT Online_Capable FROM subjects WHERE Subject_Id = @SubjectId";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@SubjectId", subjectId_num);

                        try
                        {
                            connection.Open();
                            var result = command.ExecuteScalar();

                            if (result != null)
                            {
                                int onlineCapable = Convert.ToInt32(result);

                                // If Online_Capable is 0, return "Face_To_Face", if 1, return "Online"
                                mode = (onlineCapable == 0) ? "Face_To_Face" : "Online";
                            }
                            else
                            {
                                mode = "Subject not found";
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions
                            mode = "Error: " + ex.Message;
                        }
                    }

                    return mode;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get subject mode");
                    throw;
                }
            }
        }

        public class SubjectLoader
        {
            private string connectionString;

            // Constructor to initialize the connection string
            public SubjectLoader(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<(string employeeId, string subjectId)> GetSubjectsByEmployeeId(string employeeId)
            {
                List<(string, string)> employeeSubjectPairs = new List<(string, string)>();

                // Updated SQL query to include only records with Status = 'Waiting'
                string query = "SELECT ID, Subject_Id FROM subject_load WHERE Internal_Employee_Id = @EmployeeId AND Status = 'Waiting'";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    List<(string, string)> employeeSubjectPairs = new List<(string, string)>();

                    // Updated SQL query to include only records with Status = 'Waiting'
                    string query = "SELECT ID, Subject_Id FROM subject_load WHERE Internal_Employee_Id = @EmployeeId AND Status = 'Waiting'";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        using (MySqlCommand cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                Dictionary<string, int> subjectCount = new Dictionary<string, int>();

                                while (reader.Read())
                                {
                                    string subjectId = reader["Subject_Id"].ToString();

                                    if (subjectCount.ContainsKey(subjectId))
                                    {
                                        subjectCount[subjectId]++;
                                    }
                                    else
                                    {
                                        subjectCount[subjectId] = 1;
                                    }
                                }

                                foreach (var subject in subjectCount)
                                {
                                    for (int i = 0; i < subject.Value; i++)
                                    {
                                        employeeSubjectPairs.Add((employeeId, subject.Key));
                                    }
                                }
                            }
                        }
                    }

                    return employeeSubjectPairs;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get subjects by employee id");
                    throw;
                }
            }

            public void UpdateSubjectStatus(string employeeId, string subjectId)
            {
                try
                {
                    connection.Open();

                    string updateQuery = "UPDATE subject_load SET Status = 'Assigned' WHERE Internal_Employee_Id = @EmployeeId AND Subject_Id = @SubjectId";

                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        connection.Open();

                        string updateQuery = "UPDATE subject_load SET Status = 'Assigned' WHERE Internal_Employee_Id = @EmployeeId AND Subject_Id = @SubjectId";

                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                            updateCmd.Parameters.AddWithValue("@SubjectId", subjectId);

                            try
                            {
                                updateCmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("An error occurred while updating status: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("error update subject status");
                    throw;
                }
            }
        }

        public class SubjectHours
        {
            private int subjectId_num;
            private string connectionString;

            // Constructor to initialize subjectId_num and connection string
            public SubjectHours(int subjectId, string connectionString)
            {
                subjectId_num = subjectId;
                this.connectionString = connectionString;
            }

            // Method to get the Units attribute from the 'subjects' table and return a List of TimeSpans
            public List<TimeSpan> GetSubjectHours()
            {
                int units = GetUnitsFromDatabase();
                List<TimeSpan> timeSpans = new List<TimeSpan>();

                // Apply the time creation rules based on the units value
                if (units == 1)
                {
                    timeSpans.Add(TimeSpan.FromHours(1)); // 1:00:00
                }
                else if (units == 2)
                {
                    timeSpans.Add(TimeSpan.FromHours(2)); // 2:00:00
                }
                else if (units == 3)
                {
                    timeSpans.Add(TimeSpan.FromHours(1.5)); // 1:30:00
                    timeSpans.Add(TimeSpan.FromHours(1.5)); // 1:30:00
                }
                else if (units % 2 == 0) // Even number of units > 2
                {
                    timeSpans.Add(TimeSpan.FromHours(units / 2)); // Split into two equal parts
                    timeSpans.Add(TimeSpan.FromHours(units / 2));
                }
                else // Odd number of units > 3
                {
                    int lower = units / 2;
                    int higher = lower + 1;
                    timeSpans.Add(TimeSpan.FromHours(higher)); // Higher part
                    timeSpans.Add(TimeSpan.FromHours(lower)); // Lower part
                }

                return timeSpans;
            }

            // Method to fetch the Units value from the database based on subjectId_num
            private int GetUnitsFromDatabase()
            {
                try
                {
                    int units = 0;

                    // Query to fetch the Units value
                    string query = "SELECT Units FROM subjects WHERE Subject_Id = @SubjectId";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@SubjectId", subjectId_num);

                        try
                        {
                            connection.Open();
                            MySqlDataReader reader = command.ExecuteReader();

                            if (reader.Read())
                            {
                                units = Convert.ToInt32(reader["Units"]);
                            }

                            reader.Close();
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions (log, rethrow, etc.)
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    }

                    return units;
                }
                catch (Exception)
                {
                    MessageBox.Show("error get units from database");
                    throw;
                }
            }
        }

        public class InstructorAvailabilityChecker
        {
            private string connectionString;

            public InstructorAvailabilityChecker(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> CheckAvailability(
                List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> availableSlots,
                int employeeId_num)
            {
                // Create a list to store the updated slots
                List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> updatedSlots = new List<(TimeSpan, TimeSpan, string, int)>();

                // SQL query to get the instructor's availability
                string query = "SELECT Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    // Create a list to store the updated slots
                    List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> updatedSlots = new List<(TimeSpan, TimeSpan, string, int)>();

                    // SQL query to get the instructor's availability
                    string query = "SELECT Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@EmployeeId", employeeId_num);
                        connection.Open();

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // Create a list to hold the instructor's available times
                            List<(TimeSpan startTime, TimeSpan endTime)> instructorSlots = new List<(TimeSpan, TimeSpan)>();

                            while (reader.Read())
                            {
                                TimeSpan startTime = reader.GetTimeSpan(0);
                                TimeSpan endTime = reader.GetTimeSpan(1);
                                instructorSlots.Add((startTime, endTime));
                            }

                            // Check each available slot against the instructor's availability
                            foreach (var slot in availableSlots)
                            {
                                int additionalNumber = slot.additionalNumber;

                                foreach (var instructorSlot in instructorSlots)
                                {
                                    if (slot.startTime >= instructorSlot.startTime && slot.endTime <= instructorSlot.endTime)
                                    {
                                        additionalNumber += 5;
                                        break; // No need to check further once we find a match
                                    }
                                }

                                // Add the updated slot to the new list
                                updatedSlots.Add((slot.startTime, slot.endTime, slot.day, additionalNumber));
                            }
                        }
                    }

                    return updatedSlots;
                }
                catch (Exception)
                {
                    MessageBox.Show("error instructor availability checker");
                    throw;
                }
            }
        }

        public class DataProcessor
        {
            private string subjectId;
            private string employeeId;
            private int employeeId_num;
            private int subjectId_num;

            // Constructor that accepts subjectId and employeeId as variables
            public DataProcessor(string subjectId, string employeeId)
            {
                this.subjectId = subjectId;
                this.employeeId = employeeId;

                // Parse employeeId and subjectId to integers
                if (int.TryParse(employeeId, out employeeId_num) && int.TryParse(subjectId, out subjectId_num))
                {
                    employeeId_num = int.Parse(employeeId);
                    subjectId_num = int.Parse(subjectId);
                }
                else
                {
                    throw new ArgumentException("Both employeeId and subjectId must be valid integers.");
                }
            }
            // Method to process the data or perform some operation
            public void ProcessData()
            {
                try
                {
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Store RoomId into roomList
                    RoomFetcher fetcher = new RoomFetcher(connectionString);
                    List<int> roomList = fetcher.GetRoomIds();
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Fetch Dept_Id from Subject_Id
                    DeptFetcher deptFetcher = new DeptFetcher(connectionString);
                    int deptId_1 = deptFetcher.GetDeptIdBySubject(subjectId_num);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Get the Building_Id
                    BuildingFetcher buildingFetcher = new BuildingFetcher(connectionString);
                    List<int> buildingId_1 = buildingFetcher.GetBuildingIdsByDeptId(deptId_1);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Get the rooms
                    RoomIdFetcher roomIdFetcher = new RoomIdFetcher(connectionString);
                    List<int> roomIds = roomIdFetcher.GetRoomIdsByBuildingIds(buildingId_1);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    // Storing items into roomList_1
                    List<(int, int)> roomList_1 = new List<(int, int)>();
                    Dictionary<int, string> roomCodeByIdentifier = new Dictionary<int, string>();
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Convert roomIds list to a comma-separated string for SQL IN clause
                        string roomIdsParam = string.Join(",", roomIds);

                        // Query to get Room_Id and Max_Seat from rooms table where Room_Id is in the roomIds list
                        string query = $"SELECT Room_Id, Room_Code, Max_Seat FROM rooms WHERE Room_Id IN ({roomIdsParam})";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string roomCode = reader.GetString("Room_Code");
                                int roomId = reader.GetInt32("Room_Id");
                                int maxSeat = reader.GetInt32("Max_Seat");

                                // Add Room_Id and Max_Seat to the list
                                roomList_1.Add((roomId, maxSeat));
                                roomCodeByIdentifier[roomId] = roomCode;
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    // Storing items into subject_max_student
                    List<int> subject_max_student = new List<int>();
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Max_Student from subjects table where Subject_Id matches
                        string query = "SELECT Max_Student FROM subject_load WHERE Subject_Id = @SubjectId";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@SubjectId", subjectId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int maxStudent = reader.GetInt32("Max_Student");
                                subject_max_student.Add(maxStudent);
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    // Use MatchMinimum class to process the data
                    MatchMinimum matchCalculator = new MatchMinimum();
                    Dictionary<int, int> matchCountByIdentifier = matchCalculator.CalculateMatchCounts(roomList_1, subject_max_student);

                    int maxMatches = matchCalculator.FindMaxMatches(matchCountByIdentifier);

                    // Collect all identifiers with the maximum match count
                    List<int> roomList_2 = matchCountByIdentifier
                        .Where(pair => pair.Value == maxMatches)
                        .Select(pair => pair.Key)
                        .ToList();

                    // Get the Room_Codes corresponding to the Room_Ids with the highest matches
                    List<string> roomCodes = roomList_2
                        .Select(id => roomCodeByIdentifier[id])
                        .ToList();

                    // Create a new list to store Room_Id and Room_Floor                
                    List<(int, string)> roomList_test = new List<(int, string)>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Room_Id and Room_Type from rooms table where Room_Id is in the list roomList_2
                        string query = @"SELECT Room_Id, Room_Type FROM rooms 
                     WHERE Room_Id IN (" + string.Join(",", roomList_2) + ")";

                        MySqlCommand command = new MySqlCommand(query, connection);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomId = reader.GetInt32("Room_Id");
                                string RoomType = reader.GetString("Room_Type");

                                // Add Room_Id and Room_Floor to the list
                                roomList_test.Add((roomId, RoomType));
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Filtering floors by lecture or lab
                    RoomFinder roomFinder = new RoomFinder(connectionString);
                    // Call the method to find matching Room_Id values based on the subjectId and roomList_test
                    List<int> matchingRoomIds = roomFinder.FindRooms(subjectId, roomList_test);

                    List<(int, int)> roomList_3 = new List<(int, int)>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Room_Id and Room_Floor from rooms table where Room_Id is in the list roomList_2
                        string query = @"SELECT Room_Id, Room_Floor FROM rooms 
                     WHERE Room_Id IN (" + string.Join(",", matchingRoomIds) + ")";

                        MySqlCommand command = new MySqlCommand(query, connection);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomId = reader.GetInt32("Room_Id");
                                int roomFloor = reader.GetInt32("Room_Floor");

                                // Add Room_Id and Room_Floor to the list
                                roomList_3.Add((roomId, roomFloor));
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Filtering Floors if there is a disability
                    FilterFloor roomFilter = new FilterFloor(connectionString);

                    // Filter the room list based on the disability status of the employee
                    List<(int, int)> filteredRooms = roomFilter.FilterRoomsByDisability(employeeId, roomList_3);

                    // Show filtered Room_Ids
                    List<int> roomList_4 = filteredRooms.Select(r => r.Item1).ToList();

                    // Get the Room_Codes corresponding to the Room_Ids
                    List<string> filteredRoomCodes = roomList_4
                        .Select(id => roomCodeByIdentifier[id])
                        .ToList();

                    // Create a new list to store Room_Id and Room_Floor
                    List<(int, string)> roomList_5 = new List<(int, string)>();

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to get Room_Id and Room_Type from rooms table where Room_Id is in the list roomList_2
                        string query = @"SELECT Room_Id, Room_Type FROM rooms 
                     WHERE Room_Id IN (" + string.Join(",", roomList_4) + ")";

                        MySqlCommand command = new MySqlCommand(query, connection);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomId = reader.GetInt32("Room_Id");
                                string Room_Type = reader.GetString("Room_Type");

                                // Add Room_Id and Room_Type to the list
                                roomList_5.Add((roomId, Room_Type));
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Converting to weights
                    ConvertToWeight transformer = new ConvertToWeight();

                    List<(int, int)> transformedList = transformer.TransformRoomList(matchingRoomIds);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    RoomWeightSelector selector = new RoomWeightSelector();
                    int selectedRoom = selector.ChooseRoomWithHighestWeight(transformedList);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Sorting from highest value to lowest
                    RoomWeightSorter sorter = new RoomWeightSorter();
                    List<int> sortedRoomIds = sorter.SortByWeightDescending(transformedList);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Units to hours conversion
                    SubjectHours subjectHours = new SubjectHours(subjectId_num, connectionString);

                    // Get the list of TimeSpan objects
                    List<TimeSpan> timeSpans = subjectHours.GetSubjectHours();

                    // Build the result string to display
                    string result = "Subject hours:\n";
                    foreach (TimeSpan span in timeSpans)
                    {
                        result += $"{span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}\n";
                    }
                    // Show the result in a message box
                    MessageBox.Show(result, "Subject Hours");

                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Length of time for each subject
                    List<(TimeSpan, string)> resultDays = TimeSpanClassifier.ClassifyTimeSpans(timeSpans);

                    foreach (var item in resultDays)
                    {
                        MessageBox.Show($"{item.Item1} -> {item.Item2}");
                        Console.WriteLine($"{item.Item1} -> {item.Item2}");
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Get the mode
                    var subjectMode = new SubjectMode(connectionString);
                    // Get the subject mode (Face_To_Face or Online)
                    string mode = subjectMode.GetSubjectMode(subjectId_num);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    // Create an instance of TimeSlotGenerator with the connection string and employee ID
                    TimeSlotGenerator generator = new TimeSlotGenerator(connectionString, employeeId_num);

                    // Initialize the stubCode
                    int stubCode = 0;

                    // Open a connection to get the maximum Stub_Code from the class table
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT MAX(Stub_Code) FROM class";
                        MySqlCommand cmd = new MySqlCommand(query, connection);

                        var results = cmd.ExecuteScalar();
                        if (results != DBNull.Value)
                        {
                            stubCode = Convert.ToInt32(results) + 1; // Increment the maximum Stub_Code by 1
                        }
                        else
                        {
                            stubCode = 1; // Start from 1 if no records found.
                        }
                    }

                    // Process each (TimeSpan, string) pair individually
                    foreach (var (interval, days) in resultDays)
                    {
                        // Include stubCode in the call to GenerateAvailableTimeSlots
                        List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> currentSlots = generator.GenerateAvailableTimeSlots(interval, days, selectedRoom, stubCode);

                        var checker = new InstructorAvailabilityChecker(connectionString);
                        List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> availableSlots = checker.CheckAvailability(currentSlots, employeeId_num);

                        // Prepare the message for this specific (TimeSpan, string) pair
                        string message = $"Available slots for {days}:\n\n";

                        // Check if there are any available slots
                        if (availableSlots.Count == 0)
                        {
                            message = $"No available time slots for {days}.";
                            MessageBox.Show(message, "Time Slots");
                            continue; // Skip to the next interval
                        }

                        // Use try-catch to handle potential errors when finding maxAdditionalNumber
                        try
                        {
                            // Find the maximum additionalNumber from availableSlots
                            var maxAdditionalNumber = availableSlots.Max(slot => slot.additionalNumber);

                            // Filter slots that have the maximum additionalNumber
                            var highestSlots = availableSlots.Where(slot => slot.additionalNumber == maxAdditionalNumber).ToList();

                            // Check if there are any highest slots to insert
                            if (highestSlots.Count > 0)
                            {
                                // Select the first highest slot for insertion
                                var highestSlot = highestSlots.First();
                                message += $"{highestSlot.day}: {highestSlot.startTime.ToString(@"hh\:mm")} - {highestSlot.endTime.ToString(@"hh\:mm")}\n";

                                // Insert the highest slot into the class, including the mode
                                var classInserter = new ClassInserter(connectionString);
                                classInserter.InsertClass(highestSlot.startTime, highestSlot.endTime, highestSlot.day, mode, stubCode, employeeId_num, subjectId_num, selectedRoom);
                            }
                            else
                            {
                                message += $"No highest slots available for insertion for {days}.";
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Handle the error if availableSlots is empty or if maxAdditionalNumber cannot be calculated
                            MessageBox.Show("Unable to calculate maximum available slots. Please check the schedules.", "Error");
                            continue; // Skip to the next interval
                        }

                        // Show the message for this specific list
                        MessageBox.Show(message, "Available Time Slots");
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error in Processing: " + ex.Message);
                }
            }
        }

        private void assign_btn_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = EmployeeId.ToString();

            // Initialize the SubjectLoader with the connection string
            SubjectLoader subjectLoader = new SubjectLoader(connectionString);
            List<(string employeeId, string subjectId)> employeeSubjectPairs = subjectLoader.GetSubjectsByEmployeeId(employeeId);

            if (employeeSubjectPairs.Count == 0)
            {
                MessageBox.Show("No subjects with status 'Waiting' found.");
                return;
            }

            // Message box to show the pairs with 'Waiting' status
            MessageBox.Show("Processing Pairs with 'Waiting' status: " + string.Join(", ", employeeSubjectPairs.Select(p => $"({p.employeeId}, {p.subjectId})")));

            // Loop through each pair in the list
            foreach (var pair in employeeSubjectPairs)
            {
                string currentEmployeeId = pair.employeeId;
                string subjectId = pair.subjectId;

                try
                {
                    // Create a new instance of DataProcessor with the current pair
                    DataProcessor processor = new DataProcessor(subjectId, currentEmployeeId);
                    processor.ProcessData();  // Call the ProcessData method

                    // Output to show successful processing (optional)
                    Console.WriteLine($"Processed (Employee: {currentEmployeeId}, Subject: {subjectId}) successfully.");

                    // Now update the status to 'Assigned' for this pair
                    subjectLoader.UpdateSubjectStatus(currentEmployeeId, subjectId);
                }
                catch (ArgumentException ex)
                {
                    // Handle any ArgumentExceptions thrown by DataProcessor
                    Console.WriteLine($"Error processing (Employee: {currentEmployeeId}, Subject: {subjectId}): {ex.Message}");
                }
            }

            // Indicate that processing is finished
            Console.WriteLine("Finished processing all pairs.");
        }


        private void assingAll_btn_Click(object sender, RoutedEventArgs e)
        {

        }



        #endregion

        private void debug_btn_Click(object sender, RoutedEventArgs e)
        {
        }

    }
}

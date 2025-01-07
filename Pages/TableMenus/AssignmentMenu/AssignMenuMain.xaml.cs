using Info_module.Pages.TableMenus.AssignmentMenu;
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

namespace Info_module.Pages.TableMenus.Assignment
{
    /// <summary>
    /// Interaction logic for AssignMenuMain.xaml
    /// </summary>
    public partial class AssignMenuMain : Page
    {

        string connectionString = App.ConnectionString;

        public int DepartmentId { get; set; }

        public int CurriculumId { get; set; }

        public int BlockSectionId { get; set; }

        public AssignMenuMain()
        {
            InitializeComponent();

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Assignment Menu", TopBar_BackButtonClicked);

            LoadCurriculum();
            LoadDepartmentitems();


        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new MainMenu());
        }

        #region ui

        private void schedule_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ScheduleMenuMain());
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

        #region Block Section

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

        #endregion

        #region //algorithm

        //------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------
        public class MatchMinimum
        {
            public Dictionary<int, int> CalculateMatchCounts(List<(int, int)> roomList, List<int> parameter)
            {
                // Define a dictionary to count the matches for each identifier
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

            public int FindMaxMatches(Dictionary<int, int> matchCountByIdentifier)
            {
                // Find the maximum total match count
                return matchCountByIdentifier.Values.Max();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        public class MatchDefinite
        {
            public Dictionary<int, int> CalculateMatchCounts(List<(int, int)> roomList, List<int> parameter)
            {
                // Define a dictionary to count the matches for each identifier
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

            public List<int> FindRooms(int subjectId, List<(int, string)> roomList_test)
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

            private List<int> FindLectureRooms(List<(int, string)> roomList_test)
            {
                List<int> lectureRoomIds = new List<int>();

                // Loop through the room list and find rooms with Room_Type containing 'Lecture'
                foreach (var room in roomList_test)
                {
                    if (room.Item2.Contains("LEC"))
                    {
                        lectureRoomIds.Add(room.Item1);
                    }
                }

                return lectureRoomIds;
            }

            private List<int> FindLaboratoryOrAvrRooms(List<(int, string)> roomList_test)
            {
                List<int> matchingRoomIds = new List<int>();

                // Loop through the room list and find rooms with Room_Type containing 'Laboratory' or 'AVR'
                foreach (var room in roomList_test)
                {
                    if (room.Item2.Contains("LAB"))
                    {
                        matchingRoomIds.Add(room.Item1);
                    }
                }

                return matchingRoomIds;
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

                return buildingIds; // Return the list of Building_Id values
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

                return roomIds; // Return the list of Room_Id values
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

        public class ClassScheduler
        {
            private string connectionString;

            public ClassScheduler(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void InsertScheduleIntoDatabase(int subjectId, int employeeId, List<(int RoomId, string Day, TimeSpan StartTime, TimeSpan EndTime)> scheduleList)
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

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
            //Assign Variable NOTED
            private string connectionString;
            private int employeeId_num;
            private int blockSectionId;

            // Constructor to accept the connection string
            public TimeSlotGenerator(string connectionString, int employeeId, int blockSectionId)
            {
                this.connectionString = connectionString;
                this.employeeId_num = employeeId;
                this.blockSectionId = blockSectionId;
            }

            public bool IsStubCodeExistsForDay(int stubCode, string day)
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

            private List<string> GetAvailableDaysForInstructor()
            {
                List<string> availableDays = new List<string>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to get all available days for the specified instructor
                    string query = "SELECT Day_Of_Week FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dayOfWeek = reader["Day_Of_Week"].ToString();
                            availableDays.Add(dayOfWeek);
                        }
                    }
                }

                return availableDays;
            }

            private void RemoveUnavailableDays(ref List<string> dayList)
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Day_Of_Week, Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dayOfWeek = reader["Day_Of_Week"].ToString();
                            TimeSpan startTime = TimeSpan.Parse(reader["Start_Time"].ToString());
                            TimeSpan endTime = TimeSpan.Parse(reader["End_Time"].ToString());

                            // If both Start_Time and End_Time are 00:00:00, remove the day from dayList
                            if (startTime == TimeSpan.Zero && endTime == TimeSpan.Zero)
                            {
                                dayList.Remove(dayOfWeek);
                            }
                        }
                    }
                }
            }

            public List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> GenerateAvailableTimeSlots(TimeSpan interval, string days, int selectedRoom, int stubCode)
            {
                List<(TimeSpan, TimeSpan, string, int)> availableSlots = new List<(TimeSpan, TimeSpan, string, int)>();
                List<string> dayList = new List<string>();

                // Populate dayList based on the input 'days' and remove unavailable days
                switch (days)
                {
                    case "MTWTHF":
                        dayList.AddRange(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" });
                        RemoveUnavailableDays(ref dayList);
                        break;
                    case "TTH":
                        dayList.AddRange(new[] { "Tuesday", "Thursday" });
                        RemoveUnavailableDays(ref dayList);
                        break;
                    case "MWF":
                        dayList.AddRange(new[] { "Monday", "Wednesday", "Friday" });
                        RemoveUnavailableDays(ref dayList);
                        break;
                    default:
                        return availableSlots;
                }


                // Flag to keep track if a valid day was found
                // Flag to keep track if a valid day was found
                bool foundDayWithSlots = false;

                // Generate slots for the available days
                foreach (var day in dayList)
                {
                    if (IsStubCodeExistsForDay(stubCode, day))
                    {
                        // If there is only one day in the list, use the current day instead of skipping
                        if (dayList.Count == 1)
                        {
                            foundDayWithSlots = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    List<(TimeSpan startTime, TimeSpan endTime)> dayTimeSlots = GetTimeSlotsForDay(interval, day);
                    AdjustForRestPeriods(ref dayTimeSlots, day);
                    RemoveOccupiedSlotsByRoom(ref dayTimeSlots, day, selectedRoom);
                    RemoveOccupiedSlotsBySection(ref dayTimeSlots, day, blockSectionId);
                    RemoveOccupiedSlotsByEmployee(ref dayTimeSlots, day);
                    List<(TimeSpan startTime, TimeSpan endTime)> filteredSlots = CheckInstructorAvailability(dayTimeSlots, day);

                    if (filteredSlots.Count > 0)
                    {
                        foreach (var slot in filteredSlots)
                        {
                            availableSlots.Add((slot.startTime, slot.endTime, day, 1));
                        }

                        foundDayWithSlots = true;
                        break;
                    }
                }

                // If no day with available slots was found and there is only one day, try the single day
                if (!foundDayWithSlots && dayList.Count == 1)
                {
                    string singleDay = dayList[0];
                    List<(TimeSpan startTime, TimeSpan endTime)> singleDayTimeSlots = GetTimeSlotsForDay(interval, singleDay);
                    AdjustForRestPeriods(ref singleDayTimeSlots, singleDay);
                    RemoveOccupiedSlotsByRoom(ref singleDayTimeSlots, singleDay, selectedRoom);
                    RemoveOccupiedSlotsBySection(ref singleDayTimeSlots, singleDay, blockSectionId);
                    RemoveOccupiedSlotsByEmployee(ref singleDayTimeSlots, singleDay);
                    List<(TimeSpan startTime, TimeSpan endTime)> filteredSlots = CheckInstructorAvailability(singleDayTimeSlots, singleDay);

                    if (filteredSlots.Count > 0)
                    {
                        foreach (var slot in filteredSlots)
                        {
                            availableSlots.Add((slot.startTime, slot.endTime, singleDay, 1));
                        }
                    }
                }
                return availableSlots;
            }


            private void AdjustForRestPeriods(ref List<(TimeSpan startTime, TimeSpan endTime)> dayTimeSlots, string day)
            {
                List<(TimeSpan startTime, TimeSpan endTime)> employeeTimeBlocks = new List<(TimeSpan startTime, TimeSpan endTime)>();

                // Query to find the occupied time slots for the employee on the given day
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

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
                            employeeTimeBlocks.Add((startTime, endTime));
                        }
                    }
                }

                // Determine break duration based on the day
                TimeSpan breakDuration = (day == "Monday" || day == "Wednesday" || day == "Friday")
                    ? TimeSpan.FromHours(1)  // 1 hour break
                    : TimeSpan.FromHours(1.5);  // 1 hour 30 minutes for Tuesday and Thursday

                // Adjust the time slots if consecutive blocks exceed two hours
                for (int i = 0; i < employeeTimeBlocks.Count - 1; i++)
                {
                    var currentBlock = employeeTimeBlocks[i];
                    var nextBlock = employeeTimeBlocks[i + 1];

                    // Check if the consecutive blocks exceed two hours
                    if (nextBlock.startTime - currentBlock.endTime <= TimeSpan.FromHours(2))
                    {
                        // Move all time slots that follow this time block by the breakDuration
                        TimeSpan previousEnd = currentBlock.endTime;

                        for (int j = i + 1; j < dayTimeSlots.Count; j++)
                        {
                            var originalSlot = dayTimeSlots[j];

                            // Adjust start time only if it's after the previous block's end time
                            if (originalSlot.startTime >= previousEnd)
                            {
                                TimeSpan newStartTime = originalSlot.startTime.Add(breakDuration);
                                TimeSpan newEndTime = originalSlot.endTime.Add(breakDuration);

                                // Update the slot
                                dayTimeSlots[j] = (newStartTime, newEndTime);

                                // Update previousEnd for the next iteration
                                previousEnd = newEndTime;
                            }
                        }

                        // Remove any slots between 17:30:00 and 17:40:00
                        dayTimeSlots.RemoveAll(slot => slot.startTime >= new TimeSpan(17, 30, 0) && slot.endTime <= new TimeSpan(17, 40, 0));

                        // Remove any slots that exceed 21:40:00
                        dayTimeSlots.RemoveAll(slot => slot.endTime > new TimeSpan(21, 40, 0));

                        break;  // Apply the rest period adjustment only once per day
                    }
                }
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

            //To be processed NOTED
            private void RemoveOccupiedSlotsBySection(ref List<(TimeSpan startTime, TimeSpan endTime)> availableSlots, string day, int blockSectionId)
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to find the occupied time slots for the room on the given day
                    string query = "SELECT Start_Time, End_Time FROM class WHERE Block_Section_Id = @BlockSection AND Class_Day = @Day";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@BlockSection", blockSectionId);
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

            private void RemoveOccupiedSlotsByEmployee(ref List<(TimeSpan startTime, TimeSpan endTime)> availableSlots, string day)
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

            private List<(TimeSpan startTime, TimeSpan endTime)> CheckInstructorAvailability(List<(TimeSpan startTime, TimeSpan endTime)> availableSlots, string day)
            {
                List<(TimeSpan startTime, TimeSpan endTime)> filteredSlots = new List<(TimeSpan startTime, TimeSpan endTime)>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to find the availability for the employee on the specific day
                    string query = "SELECT Start_Time, End_Time FROM instructor_availability WHERE Internal_Employee_Id = @EmployeeId AND Day_Of_Week = @Day";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId_num);
                    cmd.Parameters.AddWithValue("@Day", day);

                    TimeSpan availableStartTime = new TimeSpan(7, 0, 0); // Default start time: 7:00 AM
                    TimeSpan availableEndTime = new TimeSpan(17, 30, 0); // Default end time: 5:30 PM

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Override default values if availability exists
                            availableStartTime = (TimeSpan)reader["Start_Time"];
                            availableEndTime = (TimeSpan)reader["End_Time"];
                        }
                    }

                    // Filter available slots based on the determined availability
                    foreach (var slot in availableSlots)
                    {
                        if (slot.startTime >= availableStartTime && slot.endTime <= availableEndTime)
                        {
                            filteredSlots.Add(slot);
                        }
                    }
                }

                return filteredSlots;
            }
        }


        public class ClassInserter
        {
            private string connectionString;

            public ClassInserter(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void InsertClass(TimeSpan startTime, TimeSpan endTime, string day, string mode, int stubCode, int employeeId, int subjectId, int roomId, int blockSectionId)
            {
                string query = @"INSERT INTO class (internal_employee_id, subject_id, room_id, start_time, end_time, class_day, class_mode, stub_code, block_section_id)
                         VALUES (@internal_employee_id, @subject_id, @room_id, @start_time, @end_time, @class_day, @class_mode, @stub_code, @block_section_id);";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@internal_employee_id", employeeId);
                            cmd.Parameters.AddWithValue("@subject_id", subjectId);
                            cmd.Parameters.AddWithValue("@room_id", roomId);
                            cmd.Parameters.AddWithValue("@start_time", startTime);
                            cmd.Parameters.AddWithValue("@end_time", endTime);
                            cmd.Parameters.AddWithValue("@class_day", day);
                            cmd.Parameters.AddWithValue("@class_mode", mode);
                            cmd.Parameters.AddWithValue("@stub_code", stubCode);
                            cmd.Parameters.AddWithValue("@block_section_id", blockSectionId);

                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Class successfully inserted.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
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

            // Updated method to update both subject_load and block_subject_list
            public void UpdateSubjectStatus(int subjectId, int blockSectionId, int limit)
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update block_subject_list
                            string queryBlockSubject = @"UPDATE block_subject_list 
                     SET status = 'assigned' 
                     WHERE subjectId = @subjectId 
                       AND blockSectionId = @blockSectionId 
                       AND status = 'waiting' 
                     LIMIT @limit";

                            using (var commandBlockSubject = new MySqlCommand(queryBlockSubject, connection, transaction))
                            {
                                commandBlockSubject.Parameters.AddWithValue("@subjectId", subjectId);
                                commandBlockSubject.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                                commandBlockSubject.Parameters.AddWithValue("@limit", limit);

                                int rowsAffectedBlockSubject = commandBlockSubject.ExecuteNonQuery();
                                Console.WriteLine($"Updated {rowsAffectedBlockSubject} rows in block_subject_list for Subject_Id: {subjectId}, BlockSectionId: {blockSectionId}.");
                            }

                            // Update subject_load
                            string querySubjectLoad = @"UPDATE subject_load 
                     SET Status = 'assigned' 
                     WHERE Subject_Id = @subjectId 
                       AND Status = 'waiting' 
                     LIMIT @limit";

                            using (var commandSubjectLoad = new MySqlCommand(querySubjectLoad, connection, transaction))
                            {
                                commandSubjectLoad.Parameters.AddWithValue("@subjectId", subjectId);
                                commandSubjectLoad.Parameters.AddWithValue("@limit", limit);

                                int rowsAffectedSubjectLoad = commandSubjectLoad.ExecuteNonQuery();
                                Console.WriteLine($"Updated {rowsAffectedSubjectLoad} rows in subject_load for Subject_Id: {subjectId}.");
                            }

                            // Commit transaction
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction in case of error
                            transaction.Rollback();
                            Console.WriteLine("Transaction failed: " + ex.Message);
                            throw;
                        }
                    }
                }
            }


            // Existing method to retrieve subjects with 'Waiting' status
            public List<(string employeeId, string subjectId)> GetSubjectsByEmployeeIdAndSubjectId(int subjectId)
            {
                List<(string employeeId, string subjectId)> results = new List<(string, string)>();

                using (var connection = new MySqlConnection(connectionString))
                {
                    string query = @"SELECT Internal_Employee_Id, Subject_Id 
                             FROM subject_load 
                             WHERE Subject_Id = @subjectId AND Status = 'waiting'";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@subjectId", subjectId);
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string employeeId = reader["Internal_Employee_Id"].ToString();
                                string subId = reader["Subject_Id"].ToString();
                                results.Add((employeeId, subId));
                            }
                        }
                    }
                }

                return results;
            }
        }

        public class EmployeeIdRetriever
        {
            private readonly string connectionString;

            public EmployeeIdRetriever(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public List<int> GetUniqueEmployeeIds()
            {
                List<int> employeeIds = new List<int>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT Internal_Employee_Id FROM subject_load";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employeeIds.Add(reader.GetInt32(0)); // Use GetInt32 to retrieve the integer value
                        }
                    }
                }

                return employeeIds;
            }
        }

        // Helper method to retrieve SubjectIds by BlockSectionId
        private List<int> GetSubjectIdsByBlockSectionId(int blockSectionId)
        {
            List<int> subjectIds = new List<int>();

            string query = "SELECT subjectId FROM block_subject_list WHERE blockSectionId = @BlockSectionId";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@BlockSectionId", blockSectionId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            subjectIds.Add(reader.GetInt32("subjectId"));
                        }
                    }
                }
            }

            return subjectIds;
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

            // Method to get the hours attribute from the 'subjects' table and return a List of TimeSpans
            public List<TimeSpan> GetSubjectHours()
            {
                int hours = GetUnitsFromDatabase();
                List<TimeSpan> timeSpans = new List<TimeSpan>();

                // Apply the time creation rules based on the hours value
                if (hours == 1)
                {
                    timeSpans.Add(TimeSpan.FromHours(1)); // 1:00:00
                }
                else if (hours == 2)
                {
                    timeSpans.Add(TimeSpan.FromHours(2)); // 2:00:00
                }
                else if (hours == 3)
                {
                    timeSpans.Add(TimeSpan.FromHours(1.5)); // 1:30:00
                    timeSpans.Add(TimeSpan.FromHours(1.5)); // 1:30:00
                }
                else if (hours % 2 == 0) // Even number of hours > 2
                {
                    timeSpans.Add(TimeSpan.FromHours(hours / 2)); // Split into two equal parts
                    timeSpans.Add(TimeSpan.FromHours(hours / 2));
                }
                else // Odd number of hours > 3
                {
                    int lower = hours / 2;
                    int higher = lower + 1;
                    timeSpans.Add(TimeSpan.FromHours(higher)); // Higher part
                    timeSpans.Add(TimeSpan.FromHours(lower)); // Lower part
                }

                return timeSpans;
            }

            // Method to fetch the hours value from the database based on subjectId_num
            private int GetUnitsFromDatabase()
            {
                int hours = 0;

                // Query to fetch the hours value
                string query = "SELECT Hours FROM subjects WHERE Subject_Id = @SubjectId";

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
                            hours = Convert.ToInt32(reader["Hours"]);
                        }

                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions (log, rethrow, etc.)
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }

                return hours;
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
        }

        public class DataProcessor
        {
            private string subjectId;
            private string employeeId;
            private int employeeId_num;
            private int subjectId_num;
            private int blockSectionId;
            private string connectionString;

            // Constructor that accepts subjectId and employeeId as variables
            public DataProcessor(string subjectId, string employeeId, int blockSectionId, string connectionString)
            {
                this.subjectId = subjectId;
                this.employeeId = employeeId;
                this.blockSectionId = blockSectionId;
                this.connectionString = connectionString;

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

                    List<(int, string)> roomList_test = new List<(int, string)>();

                    try
                    {
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
                                    string roomType = reader.GetString("Room_Type");

                                    // Add Room_Id and Room_Type to the list
                                    roomList_test.Add((roomId, roomType));
                                }
                            }
                        }

                        // Display the list after retrieving the data
                        if (roomList_test.Count > 0)
                        {
                            string resultLab = "Room List:\n";
                            foreach (var room in roomList_test)
                            {
                                resultLab += $"Room ID: {room.Item1}, Room Type: {room.Item2}\n";
                            }
                            // Also write the result to the console
                            Console.WriteLine(resultLab);
                        }
                        else
                        {
                            MessageBox.Show("No rooms found.");
                            Console.WriteLine("No rooms found.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        // Handle SQL exceptions such as connection issues or query errors
                        MessageBox.Show("Error Getting Room Type May Not Exist: " + ex.Message);
                        Console.WriteLine("Database error: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Handle general exceptions
                        MessageBox.Show("Error Getting Room Type May Not Exist: " + ex.Message);
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }

                    //------------------------------------------------------------------------------------------------------------------------------------
                    List<int> matchingRoomIds = new List<int>(); // Declare the list outside the try-catch block
                    List<(int, int)> roomList_3 = new List<(int, int)>(); // Declare roomList_3 outside the try-catch block

                    try
                    {
                        RoomFinder roomFinder = new RoomFinder(connectionString);

                        // Call the method to find matching Room_Id values based on the subjectId and roomList_test
                        matchingRoomIds = roomFinder.FindRooms(subjectId_num, roomList_test);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error Getting Room Type May Not Exist: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error Getting Room Type May Not Exist: " + ex.Message);
                    }

                    // Now check for matching room IDs
                    if (matchingRoomIds.Count > 0)
                    {
                        // Construct a message with all matching room IDs
                        string message = "Matching rooms found:\n";
                        foreach (int roomId in matchingRoomIds)
                        {
                            message += roomId + "\n";
                        }

                        // Show the message in a message box
                        Console.WriteLine(message, "Matching Rooms");

                        // Proceed to retrieve room floor information
                        try
                        {
                            using (MySqlConnection connection = new MySqlConnection(connectionString))
                            {
                                connection.Open();

                                // Query to get Room_Id and Room_Floor from rooms table where Room_Id is in the list matchingRoomIds
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
                        }
                        catch (MySqlException ex)
                        {
                            // Handle SQL exceptions such as connection issues or query errors
                            MessageBox.Show("Error Getting a Lecture or Laboratory Room Type may not exist for this subject or employee: " + ex.Message);
                            Console.WriteLine("Database error: " + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            // Handle general exceptions
                            MessageBox.Show("Error Getting a Lecture or Laboratory Room Type may not exist for this subject or employee: " + ex.Message);
                            Console.WriteLine("An error occurred: " + ex.Message);
                        }
                    }
                    else
                    {
                        // Show a message if no rooms were found
                        MessageBox.Show("No matching rooms found subject may not have a viable lecture or laboratory room.", "Info");
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
                    //hours to hours conversion
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
                    Console.WriteLine(result, "Subject Hours");

                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Length of time for each subject
                    List<(TimeSpan, string)> resultDays = TimeSpanClassifier.ClassifyTimeSpans(timeSpans);

                    foreach (var item in resultDays)
                    {
                        Console.WriteLine($"{item.Item1} -> {item.Item2}");
                    }
                    //------------------------------------------------------------------------------------------------------------------------------------
                    //Get the mode
                    var subjectMode = new SubjectMode(connectionString);
                    // Get the subject mode (Face_To_Face or Online)
                    string mode = subjectMode.GetSubjectMode(subjectId_num);
                    //------------------------------------------------------------------------------------------------------------------------------------
                    // Create an instance of TimeSlotGenerator with the connection string and employee ID
                    TimeSlotGenerator generator = new TimeSlotGenerator(connectionString, employeeId_num, blockSectionId);

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
                        bool slotsFound = false; // Flag to check if any slots were found for this interval
                        foreach (int currentRoom in sortedRoomIds) // Rename to 'currentRoom' to avoid conflict with outer scope
                        {
                            // Include stubCode in the call to GenerateAvailableTimeSlots
                            List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> currentSlots = generator.GenerateAvailableTimeSlots(interval, days, currentRoom, stubCode);

                            var checker = new InstructorAvailabilityChecker(connectionString);
                            List<(TimeSpan startTime, TimeSpan endTime, string day, int additionalNumber)> availableSlots = checker.CheckAvailability(currentSlots, employeeId_num);

                            // Prepare the message for this specific (TimeSpan, string) pair
                            string message = $"Available slots for {days} in Room {currentRoom}:\n\n";

                            // Check if there are any available slots
                            if (availableSlots.Count == 0)
                            {
                                // Move to the next room if no available slots are found
                                message = $"No available time slots for {days} in Room {currentRoom}. Trying next room...\n";
                                Console.WriteLine(message, "Time Slots");
                                continue; // Try the next room in sortedRoomIds
                            }

                            // If available slots are found, set the flag and break out of the loop
                            slotsFound = true;

                            // Use try-catch to handle potential errors when finding maxAdditionalNumber
                            try
                            {
                                // Find the maximum additionalNumber from availableSlots
                                var maxAdditionalNumber = availableSlots.Max(slot => slot.additionalNumber);

                                //============================================================================================================
                                // Area to add weights for time

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
                                    classInserter.InsertClass(highestSlot.startTime, highestSlot.endTime, highestSlot.day, mode, stubCode, employeeId_num, subjectId_num, currentRoom, blockSectionId);
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
                            Console.WriteLine(message, "Available Time Slots");

                            // Break out of the room loop as we found available slots for this interval
                            break;
                        }

                        // If no slots were found after trying all rooms, output a message
                        if (!slotsFound)
                        {
                            MessageBox.Show($"No available time slots for subject:{subjectId_num} in any of the specified rooms or employee:{employeeId_num} schedule full.");
                            Console.WriteLine($"No available time slots for {days} in any of the specified rooms.", "Time Slots");
                        }
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
            try
            {
                // Retrieve the blockSectionId from the textbox
                int blockSectionId = int.Parse(blocksection_Id_txt.Text);

                // Retrieve the list of SubjectIds associated with the given blockSectionId
                List<int> subjectIds = GetSubjectIdsByBlockSectionId(blockSectionId);

                if (subjectIds.Count == 0)
                {
                    MessageBox.Show($"No subjects found for Block Section ID: {blockSectionId}.");
                    return;
                }

                // Initialize the SubjectLoader
                SubjectLoader subjectLoader = new SubjectLoader(connectionString);

                // Group SubjectIds and their counts based on the subject_list table
                var subjectIdCounts = subjectIds
                    .GroupBy(sid => sid)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var entry in subjectIdCounts)
                {
                    int currentSubjectId = entry.Key;
                    int requiredCount = entry.Value; // Number of times this subjectId should be updated

                    // Retrieve subjects with 'Waiting' status for the current SubjectId
                    List<(string employeeId, string subjectId)> waitingSubjects =
                        subjectLoader.GetSubjectsByEmployeeIdAndSubjectId(currentSubjectId);

                    if (waitingSubjects.Count == 0)
                    {
                        Console.WriteLine($"No 'Waiting' subjects found for Subject ID: {currentSubjectId}. Using default values.");
                        waitingSubjects.Add(("0", currentSubjectId.ToString())); // Add default entry with 0 as employeeId
                    }

                    // Limit processing to the required count
                    int countToProcess = Math.Min(requiredCount, waitingSubjects.Count);

                    for (int i = 0; i < countToProcess; i++)
                    {
                        var pair = waitingSubjects[i];
                        string currentEmployeeId = pair.employeeId;
                        string currentSubjectIdToProcess = pair.subjectId;

                        try
                        {
                            // Process data for the pair
                            DataProcessor processor = new DataProcessor(currentSubjectIdToProcess, currentEmployeeId, blockSectionId, connectionString);
                            processor.ProcessData();

                            // Output to show successful processing
                            Console.WriteLine($"Processed (Employee: {currentEmployeeId}, Subject: {currentSubjectIdToProcess}) successfully.");
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Error processing (Employee: {currentEmployeeId}, Subject: {currentSubjectIdToProcess}): {ex.Message}");
                        }
                    }

                    // Update only the processed rows' status to 'Assigned'
                    subjectLoader.UpdateSubjectStatus(currentSubjectId, blockSectionId, countToProcess);
                }

                // Indicate processing completion
                MessageBox.Show("Finished processing all subjects.");
                Console.WriteLine("Finished processing all subjects.");
            }
            catch (Exception ex)
            {
                // Handle errors
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Method to fetch blockSectionIds by Curriculum_Id
        private List<int> GetBlockSectionIdsByCurriculumId(int curriculumId)
        {
            List<int> blockSectionIds = new List<int>();

            string query = "SELECT blockSectionId FROM block_section WHERE curriculumId = @CurriculumId AND status = 1";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CurriculumId", curriculumId);

                conn.Open();
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        blockSectionIds.Add(reader.GetInt32(0));
                    }
                }
            }

            return blockSectionIds;
        }


        private void assingAll_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Retrieve the Curriculum_Id from the textbox
                int curriculumId = int.Parse(curriculum_Id_txt.Text);

                // Retrieve the list of blockSectionIds associated with the given Curriculum_Id
                List<int> blockSectionIds = GetBlockSectionIdsByCurriculumId(curriculumId);

                if (blockSectionIds.Count == 0)
                {
                    MessageBox.Show($"No block sections found for Curriculum ID: {curriculumId}.");
                    return;
                }

                // Initialize the SubjectLoader
                SubjectLoader subjectLoader = new SubjectLoader(connectionString);

                foreach (int blockSectionId in blockSectionIds)
                {
                    // Retrieve the list of SubjectIds associated with the current blockSectionId
                    List<int> subjectIds = GetSubjectIdsByBlockSectionId(blockSectionId);

                    if (subjectIds.Count == 0)
                    {
                        Console.WriteLine($"No subjects found for Block Section ID: {blockSectionId}.");
                        continue;
                    }

                    // Group SubjectIds and their counts based on the subject_list table
                    var subjectIdCounts = subjectIds
                        .GroupBy(sid => sid)
                        .ToDictionary(g => g.Key, g => g.Count());

                    foreach (var entry in subjectIdCounts)
                    {
                        int currentSubjectId = entry.Key;
                        int requiredCount = entry.Value; // Number of times this subjectId should be updated

                        // Retrieve subjects with 'Waiting' status for the current SubjectId
                        List<(string employeeId, string subjectId)> waitingSubjects =
                            subjectLoader.GetSubjectsByEmployeeIdAndSubjectId(currentSubjectId);

                        if (waitingSubjects.Count == 0)
                        {
                            Console.WriteLine($"No 'Waiting' subjects found for Subject ID: {currentSubjectId}.");
                            continue;
                        }

                        // Limit processing to the required count
                        int countToProcess = Math.Min(requiredCount, waitingSubjects.Count);

                        for (int i = 0; i < countToProcess; i++)
                        {
                            var pair = waitingSubjects[i];
                            string currentEmployeeId = pair.employeeId;
                            string currentSubjectIdToProcess = pair.subjectId;

                            try
                            {
                                // Process data for the pair
                                DataProcessor processor = new DataProcessor(currentSubjectIdToProcess, currentEmployeeId, blockSectionId, connectionString);
                                processor.ProcessData();

                                // Output to show successful processing
                                Console.WriteLine($"Processed (Employee: {currentEmployeeId}, Subject: {currentSubjectIdToProcess}) successfully.");
                            }
                            catch (ArgumentException ex)
                            {
                                Console.WriteLine($"Error processing (Employee: {currentEmployeeId}, Subject: {currentSubjectIdToProcess}): {ex.Message}");
                            }
                        }

                        // Update only the processed rows' status to 'Assigned'
                        subjectLoader.UpdateSubjectStatus(currentSubjectId, blockSectionId, countToProcess);
                    }
                }

                // Indicate processing completion
                MessageBox.Show("Finished processing all subjects.");
                Console.WriteLine("Finished processing all subjects.");
            }
            catch (Exception ex)
            {
                // Handle errors
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        #endregion

        
    }
}

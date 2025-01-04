using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorMenuTimePref.xaml
    /// </summary>
    public partial class InstructorMenuTimePref : Window
    {
        string connectionString = App.ConnectionString;

        public int InstructorId { get; set; }
        public string EmployeeId { get; set; }
        public string InstructorLastName {  get; set; }
        public string InstructorFirstName { get; set; }
        public string InstructorMiddleName { get; set; }

        public InstructorMenuTimePref(int instructorId,string employeeId, string instructorLastname, string instructorMiddleName, string instructorFirstName)
        {
            InitializeComponent();
            InstructorId = instructorId;
            EmployeeId = employeeId;
            InstructorLastName = instructorLastname;
            InstructorMiddleName = instructorMiddleName;
            InstructorFirstName = instructorFirstName;
            loadEmployee();
            LoadSortedAvailabilityToDataGrid(instructorId);
        }

        public void loadEmployee()
        {
            employeeId_txt.Text = EmployeeId;
            string FullName = $"{InstructorFirstName} {InstructorMiddleName} {InstructorLastName}".Trim();
            Name_txt.Text = FullName;

        }

        // Dictionary to hold the start and end times for each day of the week
        private Dictionary<string, (string Start, string End)> availability = new Dictionary<string, (string Start, string End)>();

        public class Availability : INotifyPropertyChanged
        {
            private string day;
            private string start;
            private string end;

            public string Day
            {
                get => day;
                set
                {
                    day = value;
                    OnPropertyChanged(nameof(Day));
                }
            }

            public string Start
            {
                get => start;
                set
                {
                    start = value;
                    OnPropertyChanged(nameof(Start));
                }
            }

            public string End
            {
                get => end;
                set
                {
                    end = value;
                    OnPropertyChanged(nameof(End));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<Availability> availabilityList = new ObservableCollection<Availability>();

        private Dictionary<string, int> dayOrder = new Dictionary<string, int>
        {
            { "Monday", 1 },
            { "Tuesday", 2 },
            { "Wednesday", 3 },
            { "Thursday", 4 },
            { "Friday", 5 },
            { "Saturday", 6 },
            { "Sunday", 7 }
        };

        public void RetrieveInstructorAvailability(int internalEmployeeId)
        {
            // Clear the dictionary before loading new data
            availability.Clear();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to select availability for the specified employee, ordered by Day_Of_Week
                    string query = @"
                SELECT
                    Day_Of_Week as day,
                    DATE_FORMAT(Start_Time, '%H:%i') as start,
                    DATE_FORMAT(End_Time, '%H:%i') as end
                FROM instructor_availability
                WHERE Internal_Employee_Id = @InternalEmployeeId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@InternalEmployeeId", internalEmployeeId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Iterate through the DataTable and populate the dictionary
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string day = row["day"].ToString();
                        string start = row["start"].ToString();
                        string end = row["end"].ToString();

                        // Store the start and end times in the dictionary
                        availability[day] = (start, end);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<string> CheckForMissingWeekdays()
        {
            // List of all weekdays
            List<string> allWeekdays = new List<string>
            {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};

            // List to hold missing weekdays
            List<string> missingWeekdays = new List<string>();

            // Check for missing weekdays
            foreach (var day in allWeekdays)
            {
                if (!availability.ContainsKey(day))
                {
                    // Add missing weekday with null or -1 values
                    availability[day] = (null, null); // or use (-1, -1) if preferred
                    missingWeekdays.Add(day);
                }
            }

            return missingWeekdays;
        }

        public List<Availability> GetAvailabilityList()
        {
            List<Availability> availabilityList = new List<Availability>();

            foreach (var entry in availability)
            {
                availabilityList.Add(new Availability
                {
                    Day = entry.Key,
                    Start = entry.Value.Start ?? "N/A", // Use "N/A" for null values
                    End = entry.Value.End ?? "N/A"      // Use "N/A" for null values
                });
            }
            availabilityList = availabilityList.OrderBy(a => dayOrder[a.Day]).ToList();

            return availabilityList;
        }

        public void LoadSortedAvailabilityToDataGrid(int internalEmployeeId)
        {
            RetrieveInstructorAvailability(internalEmployeeId);
            List<string> missingDays = CheckForMissingWeekdays();
            availabilityList.Clear(); // Clear previous data

            foreach (var entry in availability)
            {
                availabilityList.Add(new Availability
                {
                    Day = entry.Key,
                    Start = entry.Value.Start ?? "00:00",
                    End = entry.Value.End ?? "00:00"
                });
            }

            // Sort the list based on the day order
            var sortedList = availabilityList.OrderBy(a => dayOrder[a.Day]).ToList();
            availabilityList.Clear();
            foreach (var item in sortedList)
            {
                availabilityList.Add(item);
            }

            timePref_data.ItemsSource = availabilityList;
        }



        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void timePref_data_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Get the edited item
                var editedItem = e.Row.Item as Availability;
                if (editedItem != null)
                {
                    // Here you can access the updated values
                    string updatedStart = editedItem.Start;
                    string updatedEnd = editedItem.End;

                }
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validate the data in the DataGrid
            foreach (var item in availabilityList)
            {
                // Check if Start is set but End is null
                if (!string.IsNullOrEmpty(item.Start) && string.IsNullOrEmpty(item.End))
                {
                    MessageBox.Show($"End time cannot be null for {item.Day} if Start time is set.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Exit the method if validation fails
                }

                // Check if End is set but Start is null
                if (!string.IsNullOrEmpty(item.End) && string.IsNullOrEmpty(item.Start))
                {
                    MessageBox.Show($"Start time cannot be null for {item.Day} if End time is set.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Exit the method if validation fails
                }

                // Validate time format
                if (!IsValidTimeFormat(item.Start))
                {
                    MessageBox.Show($"Start time '{item.Start}' for {item.Day} is not in the correct format (HH:mm).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Exit the method if validation fails
                }

                if (!IsValidTimeFormat(item.End))
                {
                    MessageBox.Show($"End time '{item.End}' for {item.Day} is not in the correct format (HH:mm).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Exit the method if validation fails
                }
            }

            // If validation passes, save the data to the database
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (var item in availabilityList)
                    {
                        // Skip the upload if both Start and End are null
                        if (string.IsNullOrEmpty(item.Start) && string.IsNullOrEmpty(item.End))
                        {
                            continue; // Skip this iteration
                        }

                        // Prepare the SQL command to update the availability
                        string query = @"
                    INSERT INTO instructor_availability (Internal_Employee_Id, Day_Of_Week, Start_Time, End_Time)
                    VALUES (@InternalEmployeeId, @Day, @Start, @End)
                    ON DUPLICATE KEY UPDATE Start_Time = @Start, End_Time = @End";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@InternalEmployeeId", InstructorId); // Replace with the actual employee ID
                            command.Parameters.AddWithValue("@Day", item.Day);
                            command.Parameters.AddWithValue("@Start", item.Start ?? (object)DBNull.Value); // Use DBNull.Value for null
                            command.Parameters.AddWithValue("@End", item.End ?? (object)DBNull.Value); // Use DBNull.Value for null

                            command.ExecuteNonQuery(); // Execute the command
                        }
                    }
                }

                MessageBox.Show("Availability saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error saving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Method to validate time format
        private bool IsValidTimeFormat(string time)
        {
            // Check if the time is in HH:mm format
            return TimeSpan.TryParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture, out _);
        }
    }
}

using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using Info_module.Pages.TableMenus.CollegeMenu;
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

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorMenuMain.xaml
    /// </summary>
    public partial class InstructorMenuMain : Page
    {
        public int DepartmentId { get; set; }

        public int InternalEmployeeId { get; set; }

        string connectionString = App.ConnectionString;

        public InstructorMenuMain(int departmentId)
        {
            InitializeComponent();
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Instructor Menu", TopBar_BackButtonClicked);

            DepartmentId = departmentId;
            InternalEmployeeId = 0;
            LoadDepartmentDetails();
            LoadInstructors();
        }
        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigateBack("Instructor");
        }

        private void LoadDepartmentDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Name, Logo_Image FROM departments WHERE Dept_Id = @deptId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            collegeName_txt.Text = departmentName;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading department details: " + ex.Message);
            }
        }

        private void LoadInstructors(string statusFilter = "Active")
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
                WHERE i.Dept_Id = @deptId";

                    // Add a condition for filtering by status
                    if (statusFilter == "Active")
                    {
                        query += " AND i.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND i.Status = 0";
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    instructor_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedStatus = "Active";
            if (Status_btn == null)
            {
                return;
            }

            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadInstructors(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    Status_btn.Content = "Deactivate"; // For active departments
                    Status_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    Status_btn.Content = "Activate"; // For inactive departments
                    Status_btn.FontSize = 12;
                }
                else
                {
                    Status_btn.Content = "Switch Status"; // Default text for "All"
                    Status_btn.FontSize = 10;
                }
            }
        }
        private void instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructor_data.SelectedItem != null)
            {
                DataRowView selectedRow = instructor_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    InternalEmployeeId = (int)selectedRow["Internal_Employee_Id"];
                }
            }
        }

        private void Status_btn_Click(object sender, RoutedEventArgs e)
        {
            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Get the current status from the selected row
                        bool isActive = selectedRow["Status"].ToString() == "Active";

                        // If the instructor is active, set status to 0 (deactivate); if inactive, set to 1 (activate)
                        string query = isActive
                            ? "UPDATE instructor SET Status = 0 WHERE Internal_Employee_Id = @InternalEmployeeId"
                            : "UPDATE instructor SET Status = 1 WHERE Internal_Employee_Id = @InternalEmployeeId";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@InternalEmployeeId", (int)selectedRow["Internal_Employee_Id"]);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Provide appropriate feedback based on the action performed
                            string message = isActive ? "Instructor deactivated successfully." : "Instructor activated successfully.";
                            MessageBox.Show(message);

                            // Refresh the DataGrid to reflect the updated status
                            LoadInstructors();
                        }
                        else
                        {
                            MessageBox.Show("Error updating instructor status.");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error updating instructor status: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select an instructor to change their status.");
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (InternalEmployeeId == null || InternalEmployeeId == 0)
            {
                MessageBox.Show("Please select an Instructor ", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                InstructorMenuEdit instructorMenuEditWindow = new InstructorMenuEdit(InternalEmployeeId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                instructorMenuEditWindow.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                InternalEmployeeId = 0;
                LoadInstructors();
            }

        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                InstructorMenuAdd instructorMenuAddWindow = new InstructorMenuAdd(DepartmentId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                instructorMenuAddWindow.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                LoadInstructors();
            }

        }

        private void timePreference_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void csv_btn_Click(object sender, RoutedEventArgs e)
        {
            InstructorCSV instructorCSV = new InstructorCSV(DepartmentId);
            NavigationService.Navigate(instructorCSV);
        }
    }
}

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

namespace Info_module.Pages.TableMenus.CollegeMenu
{
    /// <summary>
    /// Interaction logic for CollegeMenuMain.xaml
    /// </summary>
    public partial class CollegeMenuMain : Page
    {
        public string PageTitle { get; set; }
        public int departmentId;

        string connectionString = App.ConnectionString;


        public CollegeMenuMain()
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Colleges Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
            LoadDepartmentData();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        private void LoadDepartmentData(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    d.Dept_Id AS 'Department_ID',
                    d.Building_Id,
                    b.Building_Code AS 'Building_Code',
                    d.Dept_Code AS 'Department_Code',
                    d.Dept_Name AS 'Department_Name',
                    CASE 
                        WHEN d.Status = 1 THEN 'Active'
                        ELSE 'Inactive'
                    END AS 'Status'
                FROM departments d
                INNER JOIN buildings b ON d.Building_Id = b.Building_Id";

                    // Apply filter based on the status
                    if (statusFilter == "Active")
                    {
                        query += " WHERE d.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " WHERE d.Status = 0";
                    }
                    // No WHERE clause for "All" to show all departments

                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    department_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Load the curriculum data filtered by the selected status
                LoadDepartmentData(selectedStatus);

                // Dynamically change the button content based on the selected status
                if (selectedStatus == "Active")
                {
                    Status_btn.Content = "Deactivate";
                    Status_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    Status_btn.Content = "Activate";
                    Status_btn.FontSize = 12;
                }
                else
                {
                    Status_btn.Content = "Switch Status";
                    Status_btn.FontSize = 10;
                }
            }
        }


        private void department_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (department_data.SelectedItem != null)
            {
                DataRowView selectedRow = department_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {

                    departmentId = Convert.ToInt32(selectedRow["Department_ID"]);
                }
            }
        }

        private void statusDepartment_btn_Click(object sender, RoutedEventArgs e)
        {
            if (department_data.SelectedItem != null)
            {
                DataRowView selectedRow = department_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    int departmentId = Convert.ToInt32(selectedRow["Department_ID"]);
                    string currentStatus = selectedRow["Status"].ToString(); // Assuming "Status" column is displayed as "Active" or "Inactive"

                    // Determine the new status value
                    int newStatus = (currentStatus == "Active") ? 0 : 1; // Toggle status

                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "UPDATE departments SET Status = @NewStatus WHERE Dept_Id = @Dept_Id";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@NewStatus", newStatus);
                                command.Parameters.AddWithValue("@Dept_Id", departmentId);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Refresh departments data after update
                        LoadDepartmentData(); // Assuming this method reloads department_data DataGrid

                        string message = (newStatus == 0) ? "Department set to Inactive successfully." : "Department set to Active successfully.";
                        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error updating department status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a department to change its status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void editDepartment_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addDepartment_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

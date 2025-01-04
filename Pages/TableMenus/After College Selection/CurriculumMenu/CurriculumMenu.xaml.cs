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

namespace Info_module.Pages.TableMenus.After_College_Selection.CurriculumMenu
{
    /// <summary>
    /// Interaction logic for CurriculumMenu.xaml
    /// </summary>
    public partial class CurriculumMenu : Page
    {
        public int DepartmentId { get; set; }
        public string CurriculumStatus { get; set; }

        public int CurriculumId = -1;

        string connectionString = App.ConnectionString;


        public CurriculumMenu(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;
            LoadUI();

        }

        //ui
        #region UI

        // Method to set up the TopBar
        private void LoadUI()
        {
            TopBar topBar = new TopBar();
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Curriculum Menu", TopBar_BackButtonClicked);

            LoadDepartmentDetails();
            LoadCurriculum();
        }

        // Event handler for the TopBar back button
        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigateBack("Curriculum");
        }

        // Method to navigate back to the previous page
        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }

        private void LoadDepartmentDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Name FROM departments WHERE Dept_Id = @deptId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId); // Assuming departmentId is defined elsewhere

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            DeptName_txt.Text = departmentName;

                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading department details: " + ex.Message);
            }
        }

        #endregion

        //datagrid
        #region Datagrid
        private void LoadCurriculum(string selectedStatus = "Active")
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
                    CONCAT(Year_Effective_In, '-', Year_Effective_Out) AS Year_Effective,
                    CASE 
                        WHEN Status = 1 THEN 'Active' 
                        ELSE 'Inactive' 
                    END AS Status
                FROM curriculum
                WHERE Dept_Id = @departmentID";

                    if (selectedStatus != "All")
                    {
                        query += " AND Status = @statusFilter";
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentID", DepartmentId);

                        if (selectedStatus != "All")
                        {
                            int statusFilter = selectedStatus == "Active" ? 1 : 0;
                            command.Parameters.AddWithValue("@statusFilter", statusFilter);
                        }

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        curriculumDataGrid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedStatus = "Active";
            if (curriculumStatus_btn == null)
            {
                return;
            }

            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                // Load the curriculum data filtered by the selected status
                LoadCurriculum(selectedStatus);

                // Dynamically change the button content based on the selected status
                if (selectedStatus == "Active")
                {
                    curriculumStatus_btn.Content = "Deactivate";
                    curriculumStatus_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    curriculumStatus_btn.Content = "Activate";
                    curriculumStatus_btn.FontSize = 12;
                }
                else
                {
                    curriculumStatus_btn.Content = "Switch Status";
                    curriculumStatus_btn.FontSize = 10;
                }
            }
        }




        #endregion

        //forms
        #region Forms
        private void curriculumDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculumDataGrid.SelectedItem is DataRowView selectedRow)
            {
                CurriculumId = (int)selectedRow["Curriculum_Id"];
                CurriculumStatus = selectedRow["Status"].ToString();
            }
        }


        private void CurriculumStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            if (CurriculumId == -1)
            {
                MessageBox.Show("Please select a Curriculum.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE curriculum 
                             SET Status = @Status
                             WHERE Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Curriculum_Id", CurriculumId);

                        // Set the status based on the current status
                        if (CurriculumStatus == "Active")
                        {
                            command.Parameters.AddWithValue("@Status", 0); // Assuming 0 means Active
                        }
                        else if (CurriculumStatus == "Inactive")
                        {
                            command.Parameters.AddWithValue("@Status", 1); // Assuming 1 means Inactive
                        }
                        else
                        {
                            MessageBox.Show("Invalid status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return; // Exit if the status is not valid
                        }

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Refresh the DataGrid
                            LoadCurriculum();
                            MessageBox.Show("Status switched successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            CurriculumId = -1;
                        }
                        else
                        {
                            MessageBox.Show("No curriculum found with the specified ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating curriculum status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) // Catch any other exceptions
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        




        #endregion

        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                CurriculumMenuPreAddEddit collegeMenuEditWindow = new CurriculumMenuPreAddEddit(DepartmentId,CurriculumId, false)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                collegeMenuEditWindow.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                CurriculumId = -1;
                LoadCurriculum();
            }

        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (CurriculumId == -1)
            {
                MessageBox.Show("Please select a Curriculum ", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                CurriculumMenuPreAddEddit collegeMenuEditWindow = new CurriculumMenuPreAddEddit(DepartmentId,CurriculumId, true)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                collegeMenuEditWindow.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                CurriculumId = -1;
                LoadCurriculum();
            }
        }

        private void configureSubjects_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CurriculumConfiguration(DepartmentId, -1, 1));
        }

        private void config_Curriculum_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if the Curriculum_Id textbox is empty
            if (CurriculumId == -1)
            {
                MessageBox.Show("Please select a Curriculum.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new CurriculumConfiguration(DepartmentId, CurriculumId, 0));
        }
    }
}


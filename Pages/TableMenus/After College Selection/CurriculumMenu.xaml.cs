using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using Info_module.ViewModels;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace Info_module.Pages.TableMenus.After_College_Selection
{
    /// <summary>
    /// Interaction logic for CurriculumMenu.xaml
    /// </summary>
    public partial class CurriculumMenu : Page
    {
        public int DepartmentId { get; set; }
        public string CurriculumStatus { get; set; }

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
                    string query = "SELECT Dept_Name, Logo_Image FROM departments WHERE Dept_Id = @deptId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId); // Assuming departmentId is defined elsewhere

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            DeptName_txt.Text = departmentName;

                            // Load logo image
                            if (!(reader["Logo_Image"] is DBNull))
                            {
                                byte[] imgData = (byte[])reader["Logo_Image"];
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = new MemoryStream(imgData);
                                bitmap.EndInit();
                                Logo_img.Source = bitmap;
                            }
                            else
                            {
                                // Handle case where no image is found
                                Logo_img.Source = null;
                            }
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
                curriculumId_txt.Text = selectedRow["Curriculum_Id"].ToString();
                curriculumRevision_txt.Text = selectedRow["Curriculum_Revision"].ToString();
                curriculumDescription_txt.Text = selectedRow["Curriculum_Description"].ToString();
                CurriculumStatus = selectedRow["status"].ToString();

                // Split the "Year_Effective" column if it contains the combined year range
                string yearEffective = selectedRow["Year_Effective"].ToString();
                if (yearEffective.Contains('-'))
                {
                    var years = yearEffective.Split('-');
                    yearEffectiveIn_txt.Text = years[0];
                    yearEffectiveOut_txt.Text = years[1];
                }
                else
                {
                    yearEffectiveIn_txt.Text = yearEffective;
                    yearEffectiveOut_txt.Text = string.Empty;
                }
            }
        }

        private void ClearTextboxes()
        {
            curriculumId_txt.Clear();
            curriculumRevision_txt.Clear();
            curriculumDescription_txt.Clear();
            yearEffectiveIn_txt.Clear();
            yearEffectiveOut_txt.Clear();
        }


        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    curriculumId_txt.Text = "";
                    connection.Open();

                    // Insert new curriculum and retrieve the Curriculum_Id
                    string insertCurriculumQuery = @"
                INSERT INTO curriculum (Dept_id, Curriculum_Revision, Curriculum_Description, Year_Effective_In, Year_Effective_Out)
                VALUES (@Dept_id, @Curriculum_Revision, @Curriculum_Description, @Year_Effective_In, @Year_Effective_Out);
                SELECT LAST_INSERT_ID();";

                    int newCurriculumId = 0;

                    using (MySqlCommand command = new MySqlCommand(insertCurriculumQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Dept_id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut_txt.Text);

                        // Execute and retrieve the new Curriculum_Id
                        newCurriculumId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Check if Curriculum_Id was retrieved successfully
                    if (newCurriculumId > 0)
                    {
                        // Insert semesters for 1st, 2nd, Summer semesters for each year (up to 4th year)
                        string insertSemesterQuery = @"
                    INSERT INTO semester (curriculum_id, year_level, semester) 
                    VALUES (@Curriculum_Id, @Year_Level, @Semester)";

                        using (MySqlCommand semesterCommand = new MySqlCommand(insertSemesterQuery, connection))
                        {
                            // Loop through each year (1 to 4) and each semester for the year
                            for (int year = 1; year <= 4; year++)
                            {
                                foreach (string sem in new string[] { "1", "2", "Summer" })
                                {
                                    semesterCommand.Parameters.Clear();
                                    semesterCommand.Parameters.AddWithValue("@Curriculum_Id", newCurriculumId);
                                    semesterCommand.Parameters.AddWithValue("@Year_Level", year);
                                    semesterCommand.Parameters.AddWithValue("@Semester", sem);
                                    semesterCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

                // Reload curriculum data and clear the input fields
                LoadCurriculum();
                ClearTextboxes();
                MessageBox.Show("Curriculum and its semesters were successfully added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Edit_btn_Click(object sender, RoutedEventArgs e)
        {
            int curriculumId;
            if (!int.TryParse(curriculumId_txt.Text, out curriculumId))
            {
                MessageBox.Show("Please enter a valid Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(curriculumRevision_txt.Text) ||
                    string.IsNullOrWhiteSpace(curriculumDescription_txt.Text) ||
                    !int.TryParse(yearEffectiveIn_txt.Text, out int yearEffectiveIn) ||
                    !int.TryParse(yearEffectiveOut_txt.Text, out int yearEffectiveOut))
                {
                    MessageBox.Show("Please fill in all fields with valid data.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"UPDATE curriculum 
                             SET Dept_Id = @Course_Id, 
                                 Curriculum_Revision = @Curriculum_Revision, 
                                 Curriculum_Description = @Curriculum_Description, 
                                 Year_Effective_In = @Year_Effective_In, 
                                 Year_Effective_Out = @Year_Effective_Out 
                             WHERE Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Course_Id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut);
                        command.Parameters.AddWithValue("@Curriculum_Id", curriculumId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Refresh the DataGrid
                            LoadCurriculum();
                            ClearTextboxes();
                            MessageBox.Show("Curriculum updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Error updating curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurriculumStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if the Curriculum ID is valid
            int curriculumId;
            if (!int.TryParse(curriculumId_txt.Text, out curriculumId))
            {
                MessageBox.Show("Please enter a valid Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE curriculum 
                             SET Status = @Status
                             Where Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                        if (CurriculumStatus == "Active")
                        {
                            command.Parameters.AddWithValue("@Status", 0);
                        }
                        else if (CurriculumStatus == "Inactive")
                        {
                            command.Parameters.AddWithValue("@Status", 1);
                        }
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Refresh the DataGrid
                            LoadCurriculum();
                            ClearTextboxes();
                            MessageBox.Show("Status Switched successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
        }



        private void config_Curriculum_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if the Curriculum_Id textbox is empty
            if (string.IsNullOrWhiteSpace(curriculumId_txt.Text))
            {
                MessageBox.Show("Please enter a Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int.TryParse(curriculumId_txt.Text, out int curriculumId);
            NavigationService.Navigate(new CurriculumConfiguration(DepartmentId, curriculumId));
        }




        #endregion
    }
}

using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using Info_module.Pages.TableMenus.Buildings;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
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

namespace Info_module.Pages.TableMenus.BlockSectionMenu
{
    /// <summary>
    /// Interaction logic for BlockSectionMenuMain.xaml
    /// </summary>
    public partial class BlockSectionMenuMain : Page
    {
        int DepartmentId { get; set; }

        int CurriculumId { get; set; }

        int BlockSectionId;

        string BlockSectionName;

        int BlockYear;

        int? CurriculumIdEdit;



        string connectionString = App.ConnectionString;

        public BlockSectionMenuMain(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Block Sections", TopBar_BackButtonClicked);
            LoadCreateTabUI();
            LoadBlockSectionDataGrid();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigateBack("BlockSection");
        }

        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }


        #region //Create Tab

        private int SelectedCurrirculumId = -1;
        private string CurriculumDescription;
        private string SelectedYearLevel;
        private string SelectedSemester;

        private void LoadCreateTabUI()
        {
            LoadCurriculumDataGrid();
        }


        private void LoadCurriculumDataGrid()
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
                    CONCAT(Year_Effective_In, '-', Year_Effective_Out) AS Year
                FROM curriculum
                WHERE Dept_Id = @departmentID AND Status = 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentID", DepartmentId);

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSectionCurriculum_data.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        private void BlockSection_Curriculum_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if there is a selected item
            if (BlockSectionCurriculum_data.SelectedItem is DataRowView selectedRow)
            {
                // Retrieve the CurriculumId from the selected row
                SelectedCurrirculumId = Convert.ToInt32(selectedRow["Curriculum_Id"]); // Adjust the column name as necessary
                CurriculumDescription = selectedRow["Curriculum_Description"].ToString();


            }
        }

        private void ViewSemester_btn_Click(object sender, RoutedEventArgs e)
        {

            SelectedYearLevel = yearLevel_cmbx.SelectedValue?.ToString();
            SelectedSemester = semester_cmbx.SelectedValue?.ToString();
            // Validate Year Level

            if (SelectedCurrirculumId == -1)
            {
                MessageBox.Show("Please select a Curriculum", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(SelectedYearLevel))
            {
                MessageBox.Show("Please select a year level.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Semester
            if (string.IsNullOrEmpty(SelectedSemester))
            {
                ;
                MessageBox.Show("Please select a semester.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                BlockSectionMenuSemester windowMenu = new BlockSectionMenuSemester(SelectedCurrirculumId, SelectedYearLevel, SelectedSemester, CurriculumDescription)
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
            }
        }

        public int? GetSemesterId(int curriculumId, int yearLevel, string semester)
        {
            int? semesterId = null; // Nullable int to handle cases where no result is found

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
            SELECT semester_id 
            FROM semester 
            WHERE curriculum_id = @curriculumId 
            AND year_level = @yearLevel 
            AND semester = @semester";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@curriculumId", curriculumId);
                        command.Parameters.AddWithValue("@yearLevel", yearLevel);
                        command.Parameters.AddWithValue("@semester", semester);

                        // Execute the command and retrieve the semester_id
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            semesterId = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Handle any SQL exceptions
                MessageBox.Show("Error retrieving semester ID: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                MessageBox.Show("An unexpected error occurred: " + ex.Message);
            }

            return semesterId; // Return the semester_id or null if not found
        }

        private void CreateBlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            SelectedYearLevel = yearLevel_cmbx.SelectedValue?.ToString();
            SelectedSemester = semester_cmbx.SelectedValue?.ToString();
            BlockYear = Convert.ToInt32(year_txt);
            

            int curriculumId = SelectedCurrirculumId; // Ensure this is set correctly
            string semester = SelectedSemester; // Ensure this is set correctly
            int yearLevel = Convert.ToInt32(SelectedYearLevel); // Ensure this is valid
            string year = year_txt.Text;


            // Get the semester ID
            int? semesterId = GetSemesterId(curriculumId, yearLevel, semester);

            if (string.IsNullOrWhiteSpace(year))
            {
                MessageBox.Show("Year cannot be empty.");
                return;
            }

            if (!int.TryParse(year, out int yearValue))
            {
                MessageBox.Show("Year must be a valid number.");
                return;
            }

            // Check for valid curriculum ID
            if (curriculumId == 0)
            {
                MessageBox.Show("No Curriculum Selected.");
                return;
            }

            // Check for valid semester ID
            if (!semesterId.HasValue)
            {
                MessageBox.Show("No Semester Selected.");
                return;
            }

            

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Step 2: Count existing block sections for the same year and semester
                    int identifier = 1;
                    string countBlockSectionsQuery = @"
            SELECT COUNT(*) 
            FROM block_section 
            WHERE year_level = @yearLevel 
            AND semester = @semester 
            AND curriculumId = @curriculumId";

                    using (MySqlCommand countCmd = new MySqlCommand(countBlockSectionsQuery, connection))
                    {
                        countCmd.Parameters.AddWithValue("@yearLevel", yearLevel);
                        countCmd.Parameters.AddWithValue("@semester", semester);
                        countCmd.Parameters.AddWithValue("@curriculumId", curriculumId);

                        identifier = Convert.ToInt32(countCmd.ExecuteScalar()) + 1;
                    }

                    // Step 3: Create the block section name
                    string formattedSemester = FormatSemester(semester);
                    string blockSectionName = $"Block Section {yearLevel}-{formattedSemester}-{identifier}-{yearValue}"; // Include year

                    // Step 4: Insert new block section
                    string blockSectionQuery = @"
            INSERT INTO block_section (blockSectionName, curriculumId, semester, year_level, status) 
            VALUES (@blockSectionName, @curriculumId, @semester, @yearLevel, b'1');
            SELECT LAST_INSERT_ID();";

                    int newBlockSectionId = 0;

                    using (MySqlCommand blockSectionCmd = new MySqlCommand(blockSectionQuery, connection))
                    {
                        blockSectionCmd.Parameters.AddWithValue("@blockSectionName", blockSectionName);
                        blockSectionCmd.Parameters.AddWithValue("@curriculumId", curriculumId);
                        blockSectionCmd.Parameters.AddWithValue("@semester", semester);
                        blockSectionCmd.Parameters.AddWithValue("@yearLevel", yearLevel);

                        newBlockSectionId = Convert.ToInt32(blockSectionCmd.ExecuteScalar());
                    }

                    // Step 5: Insert subjects for the new block section
                    List<int> subjectIds = new List<int>();
                    string getSubjectsQuery = @"
            SELECT subject_id 
            FROM semester_subject_list 
            WHERE semester_id = @semesterId";

                    using (MySqlCommand getSubjectsCmd = new MySqlCommand(getSubjectsQuery, connection))
                    {
                        getSubjectsCmd.Parameters.AddWithValue("@semesterId", semesterId.Value); // Use Value since it's not null

                        using (MySqlDataReader reader = getSubjectsCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int subjectId = reader.GetInt32("subject_id");
                                subjectIds.Add(subjectId);
                            }
                        }
                    }

                    foreach (int subjectId in subjectIds)
                    {
                        string insertSubjectQuery = @"
                INSERT INTO block_subject_list (blockSectionId, subjectId, status) 
                VALUES (@blockSectionId, @subjectId, 'waiting')";

                        using (MySqlCommand insertSubjectCmd = new MySqlCommand(insertSubjectQuery, connection))
                        {
                            insertSubjectCmd.Parameters.AddWithValue("@blockSectionId", newBlockSectionId);
                            insertSubjectCmd.Parameters.AddWithValue("@subjectId", subjectId);
                            insertSubjectCmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"Block Section '{blockSectionName}' and Subjects have been successfully created.");
                    LoadBlockSectionDataGrid();

                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error creating Block Section: " + ex.Message);
            }
        }

        private string FormatSemester(string semester)
        {
            switch (semester.Trim().ToLower())
            {
                case "1":
                case "1st":
                    return "1stSem";
                case "2":
                case "2nd":
                    return "2ndSem";
                case "summer":
                    return "Summer";
                default:
                    return semester;
            }
        }

        private void IsTextInt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        #endregion

        #region //edit BLock Section

        private void LoadBlockSectionDataGrid(int? curriculumId = null, string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Start building the query
                    string query = @"
                SELECT 
                    blockSectionId AS Block_Section_Id, 
                    blockSectionName AS Block_Section_Name,
                    year_level AS Block_Section_Year,
                    semester AS Block_Section_Semester,
                    School_Year AS BlockYear,
                    CASE
                        WHEN Status = 1 THEN 'Active'
                        ELSE 'Inactive'
                    END AS Status
                FROM block_section
                WHERE 1=1"; // This allows for easy appending of conditions

                    // Add optional curriculumId filter
                    if (curriculumId.HasValue && curriculumId.Value != -1)
                    {
                        query += " AND curriculumId = @curriculumId";
                    }

                    // Add optional status filter
                    if (statusFilter == "Active")
                    {
                        query += " AND Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND Status = 0";
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters only if curriculumId is provided
                        if (curriculumId.HasValue && curriculumId.Value != -1)
                        {
                            command.Parameters.AddWithValue("@curriculumId", curriculumId.Value);
                        }

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSection_grid.ItemsSource = dataTable.DefaultView; // Use a different DataGrid for Block Sections
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
            }
        }

        #endregion

        string selectedStatus = "Active";
        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (status_btn == null)
            {
                return;
            }

            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadBlockSectionDataGrid(CurriculumIdEdit, selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    status_btn.Content = "Deactivate"; // For active departments
                    status_btn.FontSize = 11;
                }
                else if (selectedStatus == "Inactive")
                {
                    status_btn.Content = "Activate"; // For inactive departments
                    status_btn.FontSize = 12;
                }
                else
                {
                    status_btn.Content = "Switch Status"; // Default text for "All"
                    status_btn.FontSize = 10;
                }
            }
        }

        private void status_btn_Click(object sender, RoutedEventArgs e)
        {
            if (BlockSection_grid.SelectedItems.Count > 0)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        foreach (DataRowView rowView in BlockSection_grid.SelectedItems)
                        {
                            DataRow row = rowView.Row;
                            int blockSectionId = BlockSectionId;
                            int currentStatus = 0;
                            if (row["Status"].ToString() == "Active")
                            {
                                currentStatus = 1;
                            }
                            else if (row["Status"].ToString() == "Inactive")
                            {
                                currentStatus = 0;
                            }

                            int newStatus = (currentStatus == 1) ? 0 : 1;

                            string query = "UPDATE block_section SET status = @Status WHERE blockSectionId = @blockSectionId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Status", newStatus);
                                command.Parameters.AddWithValue("@blockSectionId", BlockSectionId);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadBlockSectionDataGrid(CurriculumIdEdit, selectedStatus); // Refresh data after updating status
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one entry to update status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BlockSection_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlockSection_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSectionId = selectedRow["Block_Section_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Block_Section_Id"]) : 0;
                BlockSectionName = selectedRow["Block_Section_Name"] == DBNull.Value ? null : selectedRow["Block_Section_Name"].ToString();

            }
        }

        private void curriculum_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                BlockSectionCurriculum windowMenu = new BlockSectionCurriculum(DepartmentId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

          
                windowMenu.ShowDialog();

                    // Retrieve the curriculum ID from the dialog
                    CurriculumIdEdit = windowMenu.CurriculumId;
                    curriculumDescription_txt.Text = windowMenu.CurriculumDescription;
                    LoadBlockSectionDataGrid(CurriculumIdEdit, selectedStatus);
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            BlockSectionId = -1;

            if (BlockSection_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSectionId = selectedRow["Block_Section_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Block_Section_Id"]) : 0;
                BlockSectionName = selectedRow["Block_Section_Name"] == DBNull.Value ? null : selectedRow["Block_Section_Name"].ToString();
                BlockYear = selectedRow["BlockYear"] != DBNull.Value ? Convert.ToInt32(selectedRow["BlockYear"]) : 0;

            }

            if (BlockSectionId == -1) 
            {
                MessageBox.Show("Please select a Curriculum.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NavigationService.Navigate(new BlockSectionConfig(BlockSectionId, BlockSectionName, BlockYear, DepartmentId));

            
        }
    }

}

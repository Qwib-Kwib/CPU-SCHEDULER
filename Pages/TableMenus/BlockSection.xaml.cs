using Info_module.ViewModels;
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
namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for BlockSection.xaml
    /// </summary>
    public partial class BlockSection : Page
    {
        int DepartmentId { get; set; }

        int CurriculumId { get; set; }

        string connectionString = App.ConnectionString;


        public BlockSection(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;
            LoadUI();
        }
        private void LoadUI()
        {
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Block Sections", TopBar_BackButtonClicked);

            LoadCurriculumDataGrid();
            Load_Curriculum_ComboBox_Item();
            Load_BlockSection_Grid();
            Load_Curriculum_Subject_Grid();

            
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

        #region Block Section Data Grids

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
                WHERE Dept_Id = @departmentID";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentID", DepartmentId);

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSection_Curriculum_grid.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }


        private void LoadSemesterDataGrid(int curriculumId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    semester_id AS Semester_Id, 
                    year_level AS Semester_Year, 
                    semester as Semester
                FROM semester
                WHERE curriculum_id = @curriculumId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@curriculumId", curriculumId);

                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        BlockSection_Semester_grid.ItemsSource = dataTable.DefaultView; // Make sure this is the correct DataGrid
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
            }
        }



        private void LoadBlockSectionDataGrid(int curriculumId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    blockSectionId AS Block_Section_Id, 
                    blockSectionName AS Block_Section_Name,
                    year_level AS Block_Section_Year,
                    semester AS Block_Section_Semester
                FROM block_section
                WHERE curriculumId = @curriculumId AND status = 1;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@curriculumId", curriculumId);

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


        int BlockSection_Selected_Id;
        string BlockSection_Selected_Name;

        private void BlockSection_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlockSection_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSection_Selected_Id = Convert.ToInt32(selectedRow["Block_Section_Id"]);
                BlockSection_Selected_Name = Convert.ToString(selectedRow["Block_Section_Name"]);
                BlockSection_Id_txt.Text = BlockSection_Selected_Id.ToString();
                BlockSection_Name_txt.Text =BlockSection_Selected_Name.ToString();
            }

        }


        int BlockSection_Semester_Selected_Id;

        private void BlockSection_Semester_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlockSection_Semester_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSection_Semester_Selected_Id = selectedRow["Semester_Id"] != DBNull.Value? Convert.ToInt32(selectedRow["Semester_Id"]): 0;

                BlockSection_Semester_Id_txt.Text = BlockSection_Semester_Selected_Id.ToString();
            }

        }

        int BlockSection_Curriculum_Selected_Id;

        private void BlockSection_Curriculum_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlockSection_Curriculum_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSection_Curriculum_Selected_Id = Convert.ToInt32(selectedRow["Curriculum_Id"]);

                BlockSection_Curriculum_txt.Text = BlockSection_Curriculum_Selected_Id.ToString();
                CurriculumId = BlockSection_Curriculum_Selected_Id;

                LoadBlockSectionDataGrid(CurriculumId);
                LoadSemesterDataGrid(CurriculumId);

            }
                
        }



        #endregion

        #region Block Section Forms

        private void ClearForms()
        {
            BlockSection_Id_txt.Clear();
            BlockSection_Name_txt.Clear();
            BlockSection_Curriculum_txt.Clear();
            BlockSection_Semester_Id_txt.Clear();
            BlockSection_Semester_Selected_Id = 0;
            BlockSection_Curriculum_Selected_Id = 0;
            BlockSection_Selected_Id = 0;

        }

        private void create_BlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            if (BlockSection_Curriculum_Selected_Id == 0) 
            {
                MessageBox.Show("No Curriculum Selected ");
                return;
            }
            if (BlockSection_Semester_Selected_Id == 0)
            {
                MessageBox.Show("No Semester Selected");
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    int selectedSemesterId = BlockSection_Semester_Selected_Id;
                    int curriculumId = BlockSection_Curriculum_Selected_Id;

                    // Step 1: Retrieve semester and year level
                    string semesterDetailsQuery = @"
                SELECT semester, year_level 
                FROM semester 
                WHERE semester_id = @semesterId";

                    string semester = "";
                    int semesterYearLevel = 0;

                    using (MySqlCommand semesterCmd = new MySqlCommand(semesterDetailsQuery, connection))
                    {
                        semesterCmd.Parameters.AddWithValue("@semesterId", selectedSemesterId);

                        using (MySqlDataReader reader = semesterCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                semester = reader.GetString("semester");
                                semesterYearLevel = reader.GetInt32("year_level");
                            }
                            else
                            {
                                MessageBox.Show("No matching semester found.");
                                return;
                            }
                        }
                    }

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
                        countCmd.Parameters.AddWithValue("@yearLevel", semesterYearLevel);
                        countCmd.Parameters.AddWithValue("@semester", semester);
                        countCmd.Parameters.AddWithValue("@curriculumId", curriculumId);

                        identifier = Convert.ToInt32(countCmd.ExecuteScalar()) + 1;
                    }

                    // Step 3: Create the block section name
                    string formattedSemester = FormatSemester(semester);
                    string blockSectionName = $"Block Section {semesterYearLevel}-{formattedSemester}-{identifier}";

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
                        blockSectionCmd.Parameters.AddWithValue("@yearLevel", semesterYearLevel);

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
                        getSubjectsCmd.Parameters.AddWithValue("@semesterId", selectedSemesterId);

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
                    LoadBlockSectionDataGrid(BlockSection_Curriculum_Selected_Id);
                    Load_BlockSection_Grid();
                    ClearForms();
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


        private void disable_BlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Assuming you have a DataGrid named 'blockSection_dataGrid'
                if (BlockSection_grid.SelectedItem == null)
                {
                    MessageBox.Show("Please select a block section to disable.");
                    return;
                }

                // Extract the selected block section's ID (assuming it's stored in a property called "blockSectionId")
                DataRowView selectedRow = (DataRowView)BlockSection_grid.SelectedItem;
                int blockSectionId = Convert.ToInt32(selectedRow["Block_Section_Id"]); // Adjust 'blockSectionId' to the actual column name

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to update the status to 0
                    string updateQuery = @"
                UPDATE block_section 
                SET status = 0 
                WHERE blockSectionId = @blockSectionId";

                    using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Block Section has been successfully disabled.");
                        }
                        else
                        {
                            MessageBox.Show("Failed to disable the Block Section.");
                        }
                    }
                }

                // Optionally, refresh the DataGrid to show the updated status
                LoadBlockSectionDataGrid(CurriculumId); // Assuming LoadBlockSections() reloads the block section data
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error disabling the Block Section: " + ex.Message);
            }
        }

        #endregion

        #region Modify

        #region UI
        private void Load_Curriculum_ComboBox_Item()
        {
            try
            {
                // Clear existing items in the ComboBox
                Curriculum_cmbx.Items.Clear();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get Curriculum ID and Curriculum Revision for the selected department
                    string query = "SELECT Curriculum_Id, Curriculum_Revision FROM curriculum WHERE Dept_Id = @Dept_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Bind @Dept_Id parameter to the DepartmentId variable
                        command.Parameters.AddWithValue("@Dept_Id", DepartmentId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Get Curriculum_Id and Curriculum_Revision from the database
                                int curriculumId = reader.GetInt32("Curriculum_Id");
                                string curriculumRevision = reader.GetString("Curriculum_Revision");

                                // Create a ComboBox item
                                ComboBoxItem item = new ComboBoxItem
                                {
                                    Content = curriculumRevision,  // Display Curriculum Revision
                                    Tag = curriculumId             // Tag as Curriculum ID
                                };

                                // Add the item to the ComboBox
                                Curriculum_cmbx.Items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading curriculum items: " + ex.Message);
            }
        }
        private void Load_BlockSection_Grid(string statusFilter = "Active", int? curriculumId = null)
{
    try
    {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // SQL query to load block sections with status and curriculum filtering
            string query = @"
        SELECT 
            blockSectionId AS Block_Section_Id, 
            blockSectionName AS Block_Section_Name,
            year_level AS Block_Section_Year,
            semester AS Block_Section_Semester,
            CASE
                WHEN status = 1 THEN 'Active'
                ELSE 'Inactive'
            END AS Block_Section_Status -- This is the text representation of status
        FROM 
            block_section 
        WHERE 1 = 1"; // This is to make appending WHERE conditions easier

            // Apply status filtering based on the `statusFilter` parameter
            if (statusFilter == "Active")
            {
                query += " AND status = 1";
            }
            else if (statusFilter == "Inactive")
            {
                query += " AND status = 0";
            }

            // Apply Curriculum filtering if curriculumId is provided
            if (curriculumId.HasValue)
            {
                query += " AND curriculumId = @curriculumId";
            }

            // Add ordering to make results consistent
            query += " ORDER BY blockSectionName";

            MySqlCommand cmd = new MySqlCommand(query, connection);

            // Add parameter for CurriculumId if provided
            if (curriculumId.HasValue)
            {
                cmd.Parameters.AddWithValue("@curriculumId", curriculumId.Value);
            }

            // Create and fill DataTable
            MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);

            // Bind the DataTable to the DataGrid (Modify_BlockSection_grid)
            Modify_BlockSection_grid.ItemsSource = dt.DefaultView;
        }
    }
    catch (MySqlException ex)
    {
        MessageBox.Show("Error loading block sections: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
        private void Load_BlockSection_Subjects(int blockSectionId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get all subject details for a specific block section
                    string query = @"
            SELECT 
                s.Subject_Id,
                s.Subject_Code,
                d.Dept_Name AS Serving_Department, 
                s.Subject_Title,
                s.Subject_Type,
                s.Lecture_Lab,
                s.Hours AS Hours,
                s.Units AS Units
            FROM 
                block_subject_list bsl
            INNER JOIN 
                subjects s ON bsl.subjectId = s.Subject_Id
            INNER JOIN 
                departments d ON s.Dept_Id = d.Dept_Id
            WHERE 
                bsl.blockSectionId = @blockSectionId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    // Create and fill DataTable
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Bind the DataTable to the DataGrid (BlockSection_grid2)
                    Block_Subject_List_data.ItemsSource = dt.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects for the block section: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Load_Curriculum_Subject_Grid()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with CurriculumId and active status
                    string query = @"
                    SELECT 
                        Subject_Id AS Subject_ID, 
                        Subject_Code AS Subject_Code, 
                        Subject_Title AS Subject_Tittle 
                    FROM 
                        subjects 
                    WHERE 
                        Status = 1
                    ORDER BY Subject_Code";

                    MySqlCommand cmd = new MySqlCommand(query, connection);


                    // Create and fill DataTable
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Assuming you're using a DataGrid to display the subject list
                    Block_Subject_data.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void subject_add_to_block_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a block section and subject are selected
                int blockSectionId = BlockSection_Selected_Id; // Replace with logic to get selected block section ID
                int subjectId = Modify_Selected_SubjectId; // Replace with logic to get selected subject ID

                if (blockSectionId == 0 || subjectId == 0)
                {
                    MessageBox.Show("Please select both a block section and a subject.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if the subject is already assigned to the block section
                if (IsSubjectAssignedToBlock(blockSectionId, subjectId))
                {
                    MessageBox.Show("This subject is already assigned to the selected block section.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Insert new subject into block_subject_list
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO block_subject_list (blockSectionId, subjectId, status)
                VALUES (@blockSectionId, @subjectId, 'assigned')";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    cmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject successfully added to the block section.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to add subject to the block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optional: Refresh the DataGrid or update UI after adding the subject
                Load_BlockSection_Subjects(BlockSection_Selected_Id); // Example function to refresh the block section list
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void remove_subeject_from_block_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a block section and subject are selected
                int blockSectionId = BlockSection_Selected_Id; // Replace with logic to get selected block section ID
                int subjectId = Modify_Selected_SubjectId; // Replace with logic to get selected subject ID

                if (blockSectionId == 0 || subjectId == 0)
                {
                    MessageBox.Show("Please select both a block section and a subject to remove.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if the subject is assigned to the block section
                if (!IsSubjectAssignedToBlock(blockSectionId, subjectId))
                {
                    MessageBox.Show("This subject is not assigned to the selected block section.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Remove the subject from the block_subject_list
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                DELETE FROM block_subject_list
                WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    cmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject successfully removed from the block section.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to remove subject from the block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optional: Refresh the DataGrid or update UI after removing the subject
                Load_BlockSection_Subjects(BlockSection_Selected_Id); // Example function to refresh the block section list
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private bool IsSubjectAssignedToBlock(int blockSectionId, int subjectId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM block_subject_list WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                cmd.Parameters.AddWithValue("@subjectId", subjectId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }


        private void Modify_BlockSection_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Modify_BlockSection_grid.SelectedItem is DataRowView selectedRow)
            {
                BlockSection_Selected_Id = selectedRow["Block_Section_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Block_Section_Id"]) : 0;
                BlockSection_Selected_Name = selectedRow["Block_Section_Name"] == DBNull.Value ? null : selectedRow["Block_Section_Name"].ToString();

                Modify_BlockSectionId_txt.Text = BlockSection_Selected_Id.ToString();
                Modify_BlockSectionName_txt.Text = BlockSection_Selected_Name.ToString();

                Load_BlockSection_Subjects(BlockSection_Selected_Id);
            }

        }

        int Modify_Selected_SubjectId;
        private void Block_Subject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Block_Subject_data.SelectedItem is DataRowView selectedRow)
            {
                 Modify_Selected_SubjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                 blockSection_subjectCode_txt.Text = selectedRow["Subject_Code"].ToString();
            }
        }

        private void status_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a row is selected from the DataGrid
                if (Modify_BlockSection_grid.SelectedItem == null)
                {
                    MessageBox.Show("Please select a block section to update its status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the selected block section's ID and current status
                DataRowView selectedRow = (DataRowView)Modify_BlockSection_grid.SelectedItem;

                // Safely retrieve and convert Block_Section_Id
                int blockSectionId = selectedRow["Block_Section_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Block_Section_Id"]) : 0;

                // Safely retrieve and convert Status (use default value 0 if null)
                int currentStatus = 0;
                if (selectedRow["Block_Section_Status"] != DBNull.Value)
                {
                    if (int.TryParse(selectedRow["Block_Section_Status"].ToString(), out int parsedStatus))
                    {
                        currentStatus = parsedStatus;
                    }
                    else
                    {
                        // If Status is a string like "Active" or "Inactive", convert accordingly
                        currentStatus = selectedRow["Block_Section_Status"].ToString().ToLower() == "active" ? 1 : 0;
                    }
                }

                // Toggle status (1->0 or 0->1)
                int newStatus = currentStatus == 1 ? 0 : 1;

                // Update status in the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
UPDATE block_section 
SET Status = @newStatus 
WHERE blockSectionId = @blockSectionId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@newStatus", newStatus);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Block Section status updated successfully to {(newStatus == 1 ? "Active" : "Inactive")}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update block section status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optional: Refresh the DataGrid after updating the status
                Load_BlockSection_Grid();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        int selectedCurriculumId;
        private void Curriculum_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Curriculum_cmbx.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Curriculum_cmbx.SelectedItem;
                selectedCurriculumId = Convert.ToInt32(selectedItem.Tag);
                MessageBox.Show("Selected Curriculum ID: " + selectedCurriculumId);
                CurriculumId = selectedCurriculumId;
                Load_BlockSection_Grid(selectedStatus, selectedCurriculumId);
            }

        }

        string selectedStatus;
        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                if (Curriculum_cmbx.SelectedItem != null)
                {
                    // Pass the selected status to filter the data
                    Load_BlockSection_Grid(selectedStatus, selectedCurriculumId);
                }
                else
                {
                    Load_BlockSection_Grid(selectedStatus);
                }

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {

                    status_btn.Content = "Deactivate";
                    status_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {

                    status_btn.Content = "Activate";
                    status_btn.FontSize = 12;
                }
                else
                {

                    status_btn.Content = "Switch Status";
                    status_btn.FontSize = 8;
                }
            }

        }

        
    }
}

using CsvHelper;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using CsvHelper.Configuration;

using static Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu.InstructorMenuMain;
using ZstdSharp.Unsafe;
using System.Windows.Controls.Primitives;
using Org.BouncyCastle.Security.Certificates;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static Info_module.Pages.TableMenus.Assignment;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Info_module.ViewModels;

namespace Info_module.Pages.TableMenus.After_College_Selection.CSVMenu
{
    /// <summary>
    /// Interaction logic for CurriculumCSV.xaml
    /// </summary>
    public partial class CurriculumCSV : Page
    {
        public int DepartmentId { get; set; }
        public int CurriculumId { get; set; }

        private int selectedBlockSectionID;
        private string selectedBlockSection;
        private int selectedSubjectId;
        private string selectedServingDept;
        private int selectedYear;
        private string selectedSemester;
        private string selectedSubjectCode;
        private string selectedSubjectTittle;
        private string selectedSubjectType;
        private string selectedLABorLEC;
        private int selectedHour;
        private int selectedUnit;
        private int selectedStatus;

        string connectionString = App.ConnectionString;


        public CurriculumCSV(int departmentId, int curriculumId)
        {
            InitializeComponent();

            DepartmentId = departmentId;
            CurriculumId = curriculumId;

            LoadUI();
           

        }

        // to do
        // Show done
        // Add
        ////add forms
        ////add block section
        //// add csv
        // Edit
        // Remove

        #region ui

        private void LoadUI()
        {
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Curriculum Menu", TopBar_BackButtonClicked);


            //curriculum revision text block
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to fetch Curriculum_Revision from curriculum table
                    string curriculumQuery = @"
                SELECT Curriculum_Revision 
                FROM curriculum 
                WHERE Curriculum_Id = @curriculumId";

                    MySqlCommand curriculumCommand = new MySqlCommand(curriculumQuery, connection);
                    curriculumCommand.Parameters.AddWithValue("@curriculumId", CurriculumId);


                    // Execute the first query to get Curriculum_Revision
                    string curriculumRevision = null;
                    using (MySqlDataReader curriculumReader = curriculumCommand.ExecuteReader())
                    {
                        if (curriculumReader.Read())
                        {
                            curriculumRevision = curriculumReader["Curriculum_Revision"].ToString();
                        }
                    }

                    // Set the curriculum revision text box
                    curriculumRevision_txt.Text = curriculumRevision;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }

            LoadCurriculum();
            LoadDepartmentitems();
            LoadBlockSectionItems();
            Load_Subject_List_for_Block_Section_Grid();
            Load_Subject_List_Main_DataGrid();
            LoadGrid(blocksection_grid);

            subject_viewbox.Margin = new Thickness(10, 10, 10, 34);
            subject_viewbox.Visibility = Visibility.Collapsed;

        }
        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigationService.Navigate(new CurriculumMenu(DepartmentId));
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

                    servingDepartment_cmbx.ItemsSource = departments;
                    servingDepartment_cmbx.DisplayMemberPath = "DepartmentCodes";
                    servingDepartment_cmbx.SelectedValuePath = "DepartmentIds";
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading Departments: " + ex.Message);
            }
        }

        public class BlockSection
        {
            public int BlockSectionId { get; set; }
            public string BlockSectionName { get; set; }
        }
        private void LoadBlockSectionItems()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to fetch block sections
                    string query = "SELECT blockSectionId, blockSectionName FROM block_section";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    List<BlockSection> blockSections = new List<BlockSection>();

                    // Add "No Block Section" as the first item
                    blockSections.Add(new BlockSection
                    {
                        BlockSectionId = 0, // Value is blank or 0 to signify no selection
                        BlockSectionName = "No Block Section"
                    });

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            blockSections.Add(new BlockSection
                            {
                                BlockSectionId = reader.GetInt32("blockSectionId"),
                                BlockSectionName = reader.GetString("blockSectionName")
                            });
                        }
                    }

                    //for CSV Block Section combobox
                    CSVblockSection_cmbx.ItemsSource = blockSections;
                    CSVblockSection_cmbx.DisplayMemberPath = "BlockSectionName"; // Display block section name
                    CSVblockSection_cmbx.SelectedValuePath = "BlockSectionId";  // Use block section ID as the value

                    // Optionally, set "No Block Section" as the default selected item
                    CSVblockSection_cmbx.SelectedIndex = 0;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading Block Sections: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // function for araging grids (subject, blocksection, csv)
        private void LoadGrid(Grid activeGrid)
        {
            // List of all grids to manage
            var grids = new List<Grid> { subject_grid, blocksection_grid, csv_grid };

            // Hide all grids
            foreach (var grid in grids)
            {
                grid.Visibility = Visibility.Collapsed;
            }

            // Show the selected grid and set its margin
            if (activeGrid != null)
            {
                activeGrid.Visibility = Visibility.Visible;
                activeGrid.Margin = new Thickness(10, 0, 10, 0);
                Load_Subject_List_for_Block_Section_Grid();
            }
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadGrid(blocksection_grid);
            curriculum_viewbox.Visibility = Visibility.Visible;
            subject_viewbox.Visibility = Visibility.Collapsed;
            status_filter_viewbox.Visibility = Visibility.Visible;
        }


        #endregion

        #region Data Grid

        private void LoadCurriculum(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load block sections and their respective subjects, including the department
                    string query = @"
                SELECT 
                    bs.blockSectionId AS BlockSectionId,
                    bs.blockSectionName AS BlockSection,
                    bs.year_level AS Year_Level,
                    bs.semester AS Semester,
                    s.Subject_Id AS Subject_Id,
                    d.Dept_Code AS Serving_Department,
                    s.Subject_Code AS Subject_Code,
                    s.Subject_Title AS Subject_Title,
                    s.Subject_Type AS Subject_Type,
                    s.Lecture_Lab AS Lecture_Lab,
                    s.Hours AS Hours,
                    s.Units AS Units,
                    CASE
                        WHEN bs.Status = 1 THEN 'Active'
                        ELSE 'Inactive'
                    END AS 'Status'
                FROM block_section bs
                LEFT JOIN subject_list sl ON bs.blockSectionId = sl.blockSectionId
                LEFT JOIN subjects s ON sl.subjectId = s.Subject_Id
                LEFT JOIN departments d ON s.Dept_Id = d.Dept_Id
                WHERE bs.curriculumId = @curriculumId";

                    // Apply status filtering for block section status
                    if (statusFilter == "Active")
                    {
                        query += " AND bs.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND bs.Status = 0";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    DataView dv = dt.DefaultView;
                    // Sort first by Year_Level (ascending), then Semester (ascending), then Block Section (ascending)
                    dv.Sort = "Year_Level ASC, Semester ASC, BlockSection ASC";

                    curriculum_data.ItemsSource = dv;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void curriculum_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculum_data.SelectedItem is DataRowView selectedRow)
            {
                selectedBlockSectionID = selectedRow["BlockSectionID"] != DBNull.Value ? Convert.ToInt32(selectedRow["BlockSectionID"]) : 0;
                selectedBlockSection = selectedRow["BlockSection"] != DBNull.Value ? selectedRow["BlockSection"].ToString() : string.Empty;
                selectedSubjectId = selectedRow["Subject_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Subject_Id"]) : 0;
                selectedServingDept = selectedRow["Serving_Department"] != DBNull.Value ? selectedRow["Serving_Department"].ToString() : string.Empty;
                selectedYear = selectedRow["Year_Level"] != DBNull.Value ? Convert.ToInt32(selectedRow["Year_Level"]) : 0;
                selectedSemester = selectedRow["Semester"] != DBNull.Value ? selectedRow["Semester"].ToString() : string.Empty;
                selectedSubjectCode = selectedRow["Subject_Code"] != DBNull.Value ? selectedRow["Subject_Code"].ToString() : string.Empty;
                selectedSubjectTittle = selectedRow["Subject_Title"] != DBNull.Value ? selectedRow["Subject_Title"].ToString() : string.Empty;
                selectedSubjectType = selectedRow["Subject_Type"] != DBNull.Value ? selectedRow["Subject_Type"].ToString() : string.Empty;
                selectedLABorLEC = selectedRow["Lecture_Lab"] != DBNull.Value ? selectedRow["Lecture_Lab"].ToString() : string.Empty;
                selectedHour = selectedRow["Hours"] != DBNull.Value ? Convert.ToInt32(selectedRow["Hours"]) : 0;
                selectedUnit = selectedRow["Units"] != DBNull.Value ? Convert.ToInt32(selectedRow["Units"]) : 0;
                selectedStatus = selectedRow["Status"] != DBNull.Value && selectedRow["Status"].ToString() == "Active" ? 1 : 0;



                //fill up text boxes
                blockSectionId_txt.Text = selectedBlockSectionID.ToString();
                blockSectionName_txt.Text = selectedBlockSection.ToString();

                subjectId_txt.Text = selectedSubjectId.ToString();
                subjectCode_txt.Text = selectedSubjectCode.ToString();
                subjectTitle_txt.Text = selectedSubjectTittle.ToString();
                hours_txt.Text = selectedHour.ToString();
                units_txt.Text = selectedUnit.ToString();

                //match combobox

                SetSelectedDepartment(selectedServingDept);


                foreach (ComboBoxItem item in yearLevelBlockSection_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedYear.ToString())
                    {
                        yearLevelBlockSection_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedSemesterText = selectedSemester == "1" ? "1st Semester"
                           : selectedSemester == "2" ? "2nd Semester"
                           : "Summer";


                foreach (ComboBoxItem item in semester_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedSemesterText)
                    {
                        semester_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedSubjectTypeText = selectedSubjectType == "major" ? "Major" : "minor";
                foreach (ComboBoxItem item in subjectType_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedSubjectTypeText)
                    {
                        subjectType_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedLecLabText = selectedLABorLEC == "LEC" ? "Lecture" : "Laboratory";
                foreach (ComboBoxItem item in lecLab_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedLecLabText)
                    {
                        lecLab_cmbx.SelectedItem = item;
                        break;
                    }
                }

            }

        }
        private void SetSelectedDepartment(string selectedServingDept)
        {
            // Find and set the matching department code
            var department = servingDepartment_cmbx.Items.OfType<Department>()
                                .FirstOrDefault(d => d.DepartmentCodes == selectedServingDept);

            if (department != null)
            {
                servingDepartment_cmbx.SelectedItem = department;
            }
        }

        private void Load_Subject_List_Main_DataGrid(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with CurriculumId and active status
                    string query = @"
            SELECT 
                s.Subject_Id AS Subject_Id, 
                s.Subject_Code AS Subject_Code,
                d.Dept_Code AS Subject_Department,
                s.Subject_Title AS Subject_Title,
                s.Subject_Type AS Subject_Type,
                s.Lecture_Lab AS LEC_LAB,
                s.Hours AS Hours,
                s.Units AS Units,
                CASE
                    WHEN s.Status = 1 THEN 'Active'
                    ELSE 'Inactive'
                END AS Status
            FROM 
                subjects s 
            JOIN 
                departments d ON s.Dept_Id = d.Dept_Id";

                    // Apply status filtering based on the `statusFilter` parameter
                    if (statusFilter == "Active")
                    {
                        query += " WHERE s.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " WHERE s.Status = 0";
                    }

                    // Add ordering to make results consistent
                    query += " ORDER BY s.Subject_Code";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    // Create and fill DataTable
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Assuming you're using a DataGrid to display the subject list
                    subject_data.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsSubjectListToggled = false;

        //if pressed block section options are disabled
        private void subjectList_btn_Click(object sender, RoutedEventArgs e)
        {
            IsSubjectListToggled = !IsSubjectListToggled;

            if (IsSubjectListToggled)
            {
                subject_viewbox.Visibility = Visibility.Visible;
                status_filter_viewbox.Visibility = Visibility.Visible;
            }
            else
            {
                subject_viewbox.Visibility = Visibility.Collapsed;
                status_filter_viewbox.Visibility = Visibility.Collapsed;

            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                string selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                Load_Subject_List_Main_DataGrid(selectedStatus);
                LoadCurriculum(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    blocksectionStatus_btn.Content = "Deactivate"; // For active departments
                    blocksectionStatus_btn.FontSize = 12;

                    remove_btn.Content = "Deactivate";
                    remove_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    blocksectionStatus_btn.Content = "Activate"; // For inactive departments
                    blocksectionStatus_btn.FontSize = 12;

                    remove_btn.Content = "Activate";
                    remove_btn.FontSize = 12;
                }
                else
                {
                    blocksectionStatus_btn.Content = "Switch Status"; // Default text for "All"
                    blocksectionStatus_btn.FontSize = 8;

                    remove_btn.Content = "Switch Status";
                    blocksectionStatus_btn.FontSize = 8;
                }
            }
        }
        private void subject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subject_data.SelectedItem is DataRowView selectedRow)
            {
                selectedSubjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                selectedServingDept = selectedRow["Subject_Department"].ToString();
                selectedSubjectCode = selectedRow["Subject_Code"].ToString();
                selectedSubjectTittle = selectedRow["Subject_Title"].ToString();
                selectedSubjectType = selectedRow["Subject_Type"].ToString();
                selectedLABorLEC = selectedRow["LEC_LAB"].ToString();
                selectedHour = Convert.ToInt32(selectedRow["Hours"]);
                selectedUnit = Convert.ToInt32(selectedRow["Units"]);
                selectedStatus = (selectedRow["Status"].ToString() == "Active") ? 1 : 0;


                //fill up text boxes
                subjectId_txt.Text = selectedSubjectId.ToString();
                subjectCode_txt.Text = selectedSubjectCode.ToString();
                subjectTitle_txt.Text = selectedSubjectTittle.ToString();
                hours_txt.Text = selectedHour.ToString();
                units_txt.Text = selectedUnit.ToString();

                //match combobox

                SetSelectedDepartment(selectedServingDept);

                string selectedSubjectTypeText = selectedSubjectType == "major" ? "Major" : "minor";
                foreach (ComboBoxItem item in subjectType_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedSubjectTypeText)
                    {
                        subjectType_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedLecLabText = selectedLABorLEC == "LEC" ? "Lecture" : "Laboratory";
                foreach (ComboBoxItem item in lecLab_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedLecLabText)
                    {
                        lecLab_cmbx.SelectedItem = item;
                        break;
                    }
                }

            }

        }
        #endregion

        #region Subject

        private void hours_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void units_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void ClearSubjectInputs()
        {
            subjectCode_txt.Clear();
            subjectTitle_txt.Clear();
            servingDepartment_cmbx.SelectedIndex = -1;
            subjectType_cmbx.SelectedIndex = -1;
            lecLab_cmbx.SelectedIndex = -1;
            hours_txt.Clear();
            units_txt.Clear();
        }


        private void subject_add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get input values from textboxes and comboboxes (modify input control names accordingly)
                if (servingDepartment_cmbx.SelectedValue == null)
                {
                    MessageBox.Show("Please select a Department.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int deptId = (int)servingDepartment_cmbx.SelectedValue;

                string subjectCode = subjectCode_txt.Text.Trim();
                if (string.IsNullOrEmpty(subjectCode))
                {
                    MessageBox.Show("Please enter a valid Subject Code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subjectTitle = subjectTitle_txt.Text.Trim();
                if (string.IsNullOrEmpty(subjectTitle))
                {
                    MessageBox.Show("Please enter a valid Subject Title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subjectType = subjectType_cmbx.SelectedValue.ToString();
                if (string.IsNullOrEmpty(subjectType))
                {
                    MessageBox.Show("Please select a Subject Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string lectureLab = lecLab_cmbx.SelectedValue.ToString();
                if (lectureLab != "LEC" && lectureLab != "LAB")
                {
                    MessageBox.Show("Please select a valid Lecture/Lab type (LEC or LAB).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int hours;
                if (!int.TryParse(hours_txt.Text.Trim(), out hours) || hours <= 0)
                {
                    MessageBox.Show("Please enter valid Hours (greater than 0).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int units;
                if (!int.TryParse(units_txt.Text.Trim(), out units) || units < 0)
                {
                    MessageBox.Show("Please enter valid Units (0 or higher).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if Subject Code already exists in the database
                    string checkQuery = "SELECT COUNT(*) FROM subjects WHERE Subject_Code = @SubjectCode";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@SubjectCode", subjectCode);

                    int subjectCodeCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (subjectCodeCount > 0)
                    {
                        MessageBox.Show("This Subject Code already exists. Please enter a unique Subject Code.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Insert the new subject into the database
                    string query = @"
            INSERT INTO subjects 
            (Dept_Id, Subject_Code, Subject_Title, Subject_Type, Lecture_Lab, Hours, Units) 
            VALUES 
            (@DeptId, @SubjectCode, @SubjectTitle, @SubjectType, @LectureLab, @Hours, @Units)";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.Parameters.AddWithValue("@SubjectCode", subjectCode);
                    cmd.Parameters.AddWithValue("@SubjectTitle", subjectTitle);
                    cmd.Parameters.AddWithValue("@SubjectType", subjectType);
                    cmd.Parameters.AddWithValue("@LectureLab", lectureLab);
                    cmd.Parameters.AddWithValue("@Hours", hours);
                    cmd.Parameters.AddWithValue("@Units", units);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Load_Subject_List_Main_DataGrid();
                        ClearSubjectInputs(); // Optional: Clear input fields after successful addition
                    }
                    else
                    {
                        MessageBox.Show("Failed to add the subject. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void update_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation: Ensure all fields are filled
                if (servingDepartment_cmbx.SelectedValue == null)
                {
                    MessageBox.Show("Please select a Department.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(subjectId_txt.Text) || !int.TryParse(subjectId_txt.Text.Trim(), out int subjectId))
                {
                    MessageBox.Show("Invalid Subject ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int deptId = (int)servingDepartment_cmbx.SelectedValue;

                string subjectCode = subjectCode_txt.Text.Trim();
                if (string.IsNullOrEmpty(subjectCode))
                {
                    MessageBox.Show("Please enter a valid Subject Code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subjectTitle = subjectTitle_txt.Text.Trim();
                if (string.IsNullOrEmpty(subjectTitle))
                {
                    MessageBox.Show("Please enter a valid Subject Title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string subjectType = subjectType_cmbx.SelectedValue?.ToString();
                if (string.IsNullOrEmpty(subjectType))
                {
                    MessageBox.Show("Please select a Subject Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string lectureLab = lecLab_cmbx.SelectedValue?.ToString();
                if (lectureLab != "LEC" && lectureLab != "LAB")
                {
                    MessageBox.Show("Please select a valid Lecture/Lab type (LEC or LAB).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(hours_txt.Text.Trim(), out int hours) || hours <= 0)
                {
                    MessageBox.Show("Please enter valid Hours (greater than 0).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(units_txt.Text.Trim(), out int units) || units < 0)
                {
                    MessageBox.Show("Please enter valid Units (0 or higher).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the subject in the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
            UPDATE subjects 
            SET Dept_Id = @DeptId, 
                Subject_Code = @SubjectCode, 
                Subject_Title = @SubjectTitle, 
                Subject_Type = @SubjectType, 
                Lecture_Lab = @LectureLab, 
                Hours = @Hours, 
                Units = @Units
            WHERE Subject_Id = @SubjectId";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@DeptId", deptId);
                        cmd.Parameters.AddWithValue("@SubjectCode", subjectCode);
                        cmd.Parameters.AddWithValue("@SubjectTitle", subjectTitle);
                        cmd.Parameters.AddWithValue("@SubjectType", subjectType);
                        cmd.Parameters.AddWithValue("@LectureLab", lectureLab);
                        cmd.Parameters.AddWithValue("@Hours", hours);
                        cmd.Parameters.AddWithValue("@Units", units);
                        cmd.Parameters.AddWithValue("@SubjectId", subjectId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Subject updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            Load_Subject_List_Main_DataGrid();
                            ClearSubjectInputs(); // Clear inputs after a successful update
                        }
                        else
                        {
                            MessageBox.Show("Failed to update the subject. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearSubjectInputs();
        }

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a row is selected from the DataGrid
                if (subject_data.SelectedItem == null)
                {
                    MessageBox.Show("Please select a subject to update its status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the selected subject's ID and current status
                DataRowView selectedRow = (DataRowView)subject_data.SelectedItem;

                // Safely retrieve and convert Subject_Id
                int subjectId = selectedRow["Subject_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Subject_Id"]) : 0;

                // Safely retrieve and convert Status (use default value 0 if null)
                int currentStatus = 0;
                if (selectedRow["Status"] != DBNull.Value)
                {
                    if (int.TryParse(selectedRow["Status"].ToString(), out int parsedStatus))
                    {
                        currentStatus = parsedStatus;
                    }
                    else
                    {
                        // If Status is a string like "Active" or "Inactive", convert accordingly
                        currentStatus = selectedRow["Status"].ToString().ToLower() == "active" ? 1 : 0;
                    }
                }

                // Toggle status (1->0 or 0->1)
                int newStatus = currentStatus == 1 ? 0 : 1;

                // Update status in the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
        UPDATE subjects 
        SET Status = @newStatus 
        WHERE Subject_Id = @subjectId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@newStatus", newStatus);
                    cmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Subject status updated successfully to {(newStatus == 1 ? "Active" : "Inactive")}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update subject status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optional: Refresh the DataGrid after updating the status
                Load_Subject_List_Main_DataGrid();
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

        #region CSV

        bool CSVmode = false;

        private void CurriculumCSV_btn_Click(object sender, RoutedEventArgs e)
        {
            CSVmode = true;

            MessageBox.Show("Please ensure the CSV columns are: Block Section, Year Level, Semester, Subject Code, Serving Department, Subject Title, Subject Type, Lecture Lab, Hours, and Units.",
                            "CSV Format Information", MessageBoxButton.OK, MessageBoxImage.Information);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadCurriculumCsvFile(filePath);
                    if (dataTable != null)
                    {
                        curriculum_data.ItemsSource = dataTable.DefaultView;

                        // Show confirmation message after the CSV is uploaded and loaded successfully
                        MessageBox.Show("CSV file uploaded successfully.",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        save_btn.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void SubjectCSV_btn_Click(object sender, RoutedEventArgs e)
        {
            CSVmode = false;

            MessageBox.Show("Please ensure the CSV columns are: Subject Code, Serving Department, Subject Title, Subject Type, Lecture Lab, Hours, and Units.",
                            "CSV Format Information", MessageBoxButton.OK, MessageBoxImage.Information);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadSubjectCsvFile(filePath);
                    if (dataTable != null)
                    {
                        curriculum_data.ItemsSource = dataTable.DefaultView;

                        // Show confirmation message after the CSV is uploaded and loaded successfully
                        MessageBox.Show("CSV file uploaded successfully.",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        save_btn.Visibility = Visibility.Visible;
                        assign_to_blockSection_grid.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // Method to read CSV file into DataTable

        private DataTable ReadCurriculumCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns for the curriculum CSV
                    csvData.Columns.Add("BlockSection", typeof(string));
                    csvData.Columns.Add("Year_Level", typeof(string));
                    csvData.Columns.Add("Semester", typeof(string));
                    csvData.Columns.Add("Subject_Code", typeof(string));
                    csvData.Columns.Add("Serving_Department", typeof(string));
                    csvData.Columns.Add("Subject_Title", typeof(string));
                    csvData.Columns.Add("Subject_Type", typeof(string));
                    csvData.Columns.Add("Lecture_Lab", typeof(string));
                    csvData.Columns.Add("Hours", typeof(int));
                    csvData.Columns.Add("Units", typeof(int));

                    sr.ReadLine(); // Skip header

                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        if (rows.Length != 10)
                        {
                            MessageBox.Show("Error: Curriculum CSV must have exactly 10 columns.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr[0] = rows[0].Trim(); // Block Section
                            dr[1] = rows[1].Trim(); // Year Level
                            dr[2] = rows[2].Trim(); // Semester
                            dr[3] = rows[3].Trim().ToUpper(); // Subject Code
                            dr[4] = rows[4].Trim().ToUpper(); // Serving Department
                            dr[5] = rows[5].Trim(); // Subject Title
                            dr[6] = rows[6].Trim(); // Subject Type
                            dr[7] = rows[7].Trim().ToUpper(); // Lecture Lab
                            dr[8] = Convert.ToInt32(rows[8].Trim()); // Hours
                            dr[9] = Convert.ToInt32(rows[9].Trim()); // Units
                            csvData.Rows.Add(dr);
                        }
                        catch (FormatException ex)
                        {
                            MessageBox.Show($"Error parsing row: {string.Join(",", rows)}\n\n{ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Curriculum CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            return csvData;
        }


        private DataTable ReadSubjectCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns for the subject CSV
                    csvData.Columns.Add("Subject_Code", typeof(string));
                    csvData.Columns.Add("Serving_Department", typeof(string));
                    csvData.Columns.Add("Subject_Title", typeof(string));
                    csvData.Columns.Add("Subject_Type", typeof(string));
                    csvData.Columns.Add("Lecture_Lab", typeof(string));
                    csvData.Columns.Add("Hours", typeof(int));
                    csvData.Columns.Add("Units", typeof(int));

                    sr.ReadLine(); // Skip header

                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        if (rows.Length != 7)
                        {
                            MessageBox.Show("Error: Subject CSV must have exactly 7 columns.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr[0] = rows[0].Trim().ToUpper();
                            dr[1] = rows[1].Trim().ToUpper();
                            dr[2] = rows[2].Trim();
                            dr[3] = rows[3].Trim();
                            dr[4] = rows[4].Trim().ToUpper();
                            dr[5] = Convert.ToInt32(rows[5].Trim());
                            dr[6] = Convert.ToInt32(rows[6].Trim());
                            csvData.Rows.Add(dr);
                        }
                        catch (FormatException ex)
                        {
                            MessageBox.Show($"Error parsing row: {string.Join(",", rows)}\n\n{ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Subject CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            return csvData;
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    if (CSVmode) // Curriculum Mode
                    { 
                        foreach (DataRowView item in curriculum_data.Items) 
                        { 
                            DataRow row = item.Row; 
                            string blockSection = row["blockSection"].ToString().Trim(); 
                            int yearLevel = Convert.ToInt32(row["Year_Level"]); 
                            int semester = Convert.ToInt32(row["Semester"]); 
                            
                            // Check if Block Section exists, add if it doesn't
                            int blockSectionId = GetOrInsertBlockSection(connection, blockSection, yearLevel, semester); 
                            ProcessSubject(row, connection, blockSectionId);
                            LoadCurriculum();
                        }
                    }
                    else
                    {

                        foreach (DataRowView item in curriculum_data.Items)
                        {

                            DataRow row = item.Row;
                            int blockSectionId = Convert.ToInt32(CSVblockSection_cmbx.SelectedValue);
                            ProcessSubject(row, connection, blockSectionId);
                            LoadCurriculum();

                        }
                    }

                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessSubject(DataRow row, MySqlConnection connection, int blockSectionId)
        {
            try
            {
                // 🔍 Extract values from the row
                string departmentCode = row["Serving_Department"].ToString().Trim();
                string subjectCode = row["Subject_Code"].ToString().Trim();
                string subjectTitle = row["Subject_Title"].ToString().Trim();
                string subjectType = row["Subject_Type"].ToString().Trim();
                string lectureLab = row["Lecture_Lab"].ToString().Trim();
                int hours = Convert.ToInt32(row["Hours"]);
                int units = Convert.ToInt32(row["Units"]);

                // 1️⃣ Get Dept_Id from department using Department_Code
                int deptId = GetDepartmentId(connection, departmentCode);
                if (deptId == -1)
                {
                    MessageBox.Show($"Department '{departmentCode}' not found. Skipping subject '{subjectCode}'.",
                            "Missing Department", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // Exit the function
                }

                // 2️⃣ Check if the subject exists, and insert it if it doesn't
                int subjectId = GetOrInsertSubject(connection, deptId, subjectCode, subjectTitle, subjectType, lectureLab, hours, units);


                // 3️⃣ Insert into subject_list if blockSectionId is provided
                if (blockSectionId != 0)
                {
                    InsertSubjectList(connection, blockSectionId, subjectId, subjectCode);
                }


            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error processing subject: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetDepartmentId(MySqlConnection connection, string departmentCode)
        {
            string query = "SELECT Dept_Id FROM departments WHERE Dept_Code = @Department_Code;";
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Department_Code", departmentCode);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
            }
        }

        private int GetOrInsertSubject(MySqlConnection connection, int deptId, string subjectCode,
                       string subjectTitle, string subjectType, string lectureLab, int hours, int units)
        {
            // Check if subject already exists
            string checkQuery = "SELECT Subject_Id FROM subjects WHERE Subject_Code = @Subject_Code;";
            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                object result = checkCmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result); // Return existing Subject_Id
                }
            }

            // Insert new subject if it does not exist
            string insertQuery = @"
INSERT INTO subjects (Dept_Id, Subject_Code, Subject_Title, Subject_Type, Lecture_Lab, Hours, Units) 
VALUES (@Dept_Id, @Subject_Code, @Subject_Title, @Subject_Type, @Lecture_Lab, @Hours, @Units);
SELECT LAST_INSERT_ID();";
            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@Dept_Id", deptId);
                insertCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                insertCmd.Parameters.AddWithValue("@Subject_Title", subjectTitle);
                insertCmd.Parameters.AddWithValue("@Subject_Type", subjectType);
                insertCmd.Parameters.AddWithValue("@Lecture_Lab", lectureLab);
                insertCmd.Parameters.AddWithValue("@Hours", hours);
                insertCmd.Parameters.AddWithValue("@Units", units);

                object newSubjectIdObj = insertCmd.ExecuteScalar();
                int newSubjectId = Convert.ToInt32(newSubjectIdObj);

                Console.WriteLine($"Inserted Subject ID: {newSubjectId}");
                return newSubjectId;
            }
        }

        private void InsertSubjectList(MySqlConnection connection, int blockSectionId, int subjectId, string subjectCode)
        {
            // Check if the blockSectionId + subjectId combination already exists
            string checkQuery = @"
        SELECT subjectList_Id FROM subject_list 
        WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId;";
            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                checkCmd.Parameters.AddWithValue("@subjectId", subjectId);
                object result = checkCmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    MessageBox.Show($"Subject '{subjectCode}' is already linked to Block Section {blockSectionId}.",
                                    "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Insert new combination if it does not exist
            string insertQuery = @"
        INSERT INTO subject_list (blockSectionId, subjectId) 
        VALUES (@blockSectionId, @subjectId);";
            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                insertCmd.Parameters.AddWithValue("@subjectId", subjectId);
                int rowsAffected = insertCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {

                }
                else
                {
                    MessageBox.Show("Failed to link subject to block section.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int GetOrInsertBlockSection(MySqlConnection connection, string blockSection, int yearLevel, int semester)
        {
            

                // Check if block section exists
                string checkQuery = "SELECT blockSectionId FROM block_section WHERE blockSectionName = @BlockSection AND year_level = @YearLevel AND Semester = @Semester;";
            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@BlockSection", blockSection);
                checkCmd.Parameters.AddWithValue("@YearLevel", yearLevel);
                checkCmd.Parameters.AddWithValue("@Semester", semester);

                object result = checkCmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result); // Return the existing BlockSectionId
                }
            }
            


            // Insert new block section if not exists
            string insertQuery = @"
            INSERT INTO block_section (blockSectionName, curriculumId, year_level, Semester) 
            VALUES (@BlockSection, @curriculumId, @YearLevel, @Semester);
            SELECT LAST_INSERT_ID();";
            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@BlockSection", blockSection);
                insertCmd.Parameters.AddWithValue("@curriculumId", CurriculumId); // Replace CurriculumId with the actual curriculumId variable
                insertCmd.Parameters.AddWithValue("@YearLevel", yearLevel);
                insertCmd.Parameters.AddWithValue("@Semester", semester);
                int newBlockSectionId = Convert.ToInt32(insertCmd.ExecuteScalar());
                return newBlockSectionId;
            }
        }





#endregion

#region Block Section

private void Load_Subject_List_for_Block_Section_Grid()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with CurriculumId and active status
                    string query = @"
                    SELECT 
                        Subject_Id AS BsSubject_Id, 
                        Subject_Code AS BsSubject_Code, 
                        Subject_Title AS BsSubject_Title 
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
                    BsSubjectList_data.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        int BsSelectedSubjectId;
        string BsSelectedSubjectCode;

        private void BsSubjectList_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BsSubjectList_data.SelectedItem is DataRowView selectedRow)
            {
                BsSelectedSubjectId = Convert.ToInt32(selectedRow["BsSubject_Id"]);
                BsSelectedSubjectCode = selectedRow["BsSubject_Code"].ToString();

                blockSection_subjectCode_txt.Text = BsSelectedSubjectCode;
                
            }
        }

        private void addBlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Retrieve input values from UI
                string blockSectionName = blockSectionName_txt.Text.Trim();

                // Use int.TryParse to safely parse year level
                int yearLevel = 0;
                if (!int.TryParse(yearLevelBlockSection_cmbx.SelectedValue?.ToString(), out yearLevel))
                {
                    MessageBox.Show("Please select a valid year level.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if semester is null or empty
                string semester = semester_cmbx.SelectedValue?.ToString();
                if (string.IsNullOrEmpty(semester))
                {
                    MessageBox.Show("Please select a semester.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int curriculumId = CurriculumId;

                // Validate inputs
                if (string.IsNullOrEmpty(blockSectionName))
                {
                    MessageBox.Show("Block section name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Insert block section into the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
        INSERT INTO block_section (blockSectionName, year_level, semester, curriculumId)
        VALUES (@blockSectionName, @yearLevel, @semester, @curriculumId)";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionName", blockSectionName);
                    cmd.Parameters.AddWithValue("@yearLevel", yearLevel);
                    cmd.Parameters.AddWithValue("@semester", semester);
                    cmd.Parameters.AddWithValue("@curriculumId", curriculumId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Block section added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCurriculum();
                    }
                    else
                    {
                        MessageBox.Show("Failed to add block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid input format. Please ensure all fields are filled out correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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


        private void updateBlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Retrieve input values from UI
                int blockSectionId = Convert.ToInt32(blockSectionId_txt.Text);
                string blockSectionName = blockSectionName_txt.Text.Trim();
                int yearLevel = int.Parse(yearLevelBlockSection_cmbx.SelectedValue.ToString());
                string semester = ((ComboBoxItem)semester_cmbx.SelectedItem).Tag.ToString();
                int curriculumId = CurriculumId;

                // Assuming you're selecting a row from a DataGrid to get the Block_Section_Id
                if (curriculum_data.SelectedItem == null)
                {
                    MessageBox.Show("Please select a block section to update.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate inputs
                if (string.IsNullOrEmpty(blockSectionName))
                {
                    MessageBox.Show("Block section name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the block section in the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
            UPDATE block_section 
            SET 
                blockSectionName = @blockSectionName, 
                year_level = @yearLevel, 
                semester = @semester, 
                curriculumId = @curriculumId 
            WHERE 
                blockSectionId = @blockSectionId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionName", blockSectionName);
                    cmd.Parameters.AddWithValue("@yearLevel", yearLevel);
                    cmd.Parameters.AddWithValue("@semester", semester);
                    cmd.Parameters.AddWithValue("@curriculumId", curriculumId);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId); // Primary key for update

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Block section updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCurriculum();
                    }
                    else
                    {
                        MessageBox.Show("Failed to update block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid input format. Please ensure all fields are filled out correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void blocksectionStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a row is selected from the DataGrid
                if (curriculum_data.SelectedItem == null)
                {
                    MessageBox.Show("Please select a block section to update its status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the selected block section's ID and current status
                DataRowView selectedRow = (DataRowView)curriculum_data.SelectedItem;

                // Safely retrieve and convert BlockSectionId
                int blockSectionId = selectedRow["BlockSectionId"] != DBNull.Value ? Convert.ToInt32(selectedRow["BlockSectionId"]) : 0;

                // Safely retrieve and convert Status (use default value 0 if null)
                int currentStatus = 0;
                if (selectedRow["Status"] != DBNull.Value)
                {
                    if (int.TryParse(selectedRow["Status"].ToString(), out int parsedStatus))
                    {
                        currentStatus = parsedStatus;
                    }
                    else
                    {
                        // If Status is a string like "Active" or "Inactive", convert accordingly
                        currentStatus = selectedRow["Status"].ToString().ToLower() == "active" ? 1 : 0;
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
                SET status = @newStatus 
                WHERE blockSectionId = @blockSectionId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@newStatus", newStatus);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Block section status updated successfully to {(newStatus == 1 ? "Active" : "Inactive")}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update block section status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optional: Refresh the DataGrid after updating the status
                LoadCurriculum();
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



        private void clearBlockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            blockSectionId_txt.Clear();
            blockSectionName_txt.Clear();
            yearLevelBlockSection_cmbx.SelectedItem = null;
            semester_cmbx.SelectedItem = null;

        }

        private void subjectConfigure_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadGrid(subject_grid);
            ClearSubjectInputs();
            Load_Subject_List_Main_DataGrid();
            subject_viewbox.Visibility = Visibility.Visible;
            curriculum_viewbox.Visibility = Visibility.Collapsed;
        }

        private void csv_btn_Click(object sender, RoutedEventArgs e)
        {

            LoadGrid(csv_grid);
            status_filter_viewbox.Visibility = Visibility.Collapsed;
            save_btn.Visibility = Visibility.Collapsed;
            assign_to_blockSection_grid.Visibility = Visibility.Collapsed;
        }
        private void add_subject_to_blockSection_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Assume these come from user inputs like ComboBox or TextBox
                int blockSectionId = selectedBlockSectionID;
                int subjectId = BsSelectedSubjectId;

                // Validate and override BlockSectionId if user input exists and is valid
                if (int.TryParse(blockSectionId_txt.Text.Trim(), out int inputBlockSectionId) && inputBlockSectionId > 0)
                {
                    blockSectionId = inputBlockSectionId;
                }
                else if (blockSectionId <= 0)
                {
                    MessageBox.Show("Please enter a valid Block Section ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate Subject ID (assume subjectId is assigned from selection)
                if (subjectId <= 0)
                {
                    MessageBox.Show("Please enter a valid Subject ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate entry in the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM subject_list 
                WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId";

                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    checkCmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (existingCount > 0)
                    {
                        MessageBox.Show("This subject is already linked to the selected block section.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Insert new subject into the subject_list table
                    string insertQuery = @"
                INSERT INTO subject_list (blockSectionId, subjectId) 
                VALUES (@blockSectionId, @subjectId)";

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    insertCmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = insertCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject successfully added to the block section.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to add subject to block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optionally, reload the list of subjects linked to the block section
                LoadCurriculum();
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


        private void remove_subject_from_blockSection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get initial values from selection
                int blockSectionId = selectedBlockSectionID;
                int subjectId = selectedSubjectId;
                string subjectName = selectedSubjectCode; // This will store the subject name for the confirmation message
                string blockSectionName = selectedBlockSection; // This will store the block section name for the confirmation message

                // Validate and override BlockSectionId if user input exists
                if (int.TryParse(blockSectionId_txt.Text.Trim(), out int inputBlockSectionId) && inputBlockSectionId > 0)
                {
                    blockSectionId = inputBlockSectionId;
                }
                else if (blockSectionId <= 0)
                {
                    MessageBox.Show("Please enter a valid Block Section ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate Subject ID
                if (subjectId <= 0)
                {
                    MessageBox.Show("Please enter a valid Subject ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get Subject Name and Block Section Name from the database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    (SELECT Subject_Title FROM subjects WHERE Subject_Id = @subjectId) AS SubjectName, 
                    (SELECT blockSectionName FROM block_section WHERE blockSectionId = @blockSectionId) AS BlockSectionName";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@subjectId", subjectId);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            subjectName = reader["SubjectName"] != DBNull.Value ? reader["SubjectName"].ToString() : "Unknown Subject";
                            blockSectionName = reader["BlockSectionName"] != DBNull.Value ? reader["BlockSectionName"].ToString() : "Unknown Block Section";
                        }
                        else
                        {
                            MessageBox.Show("Subject or Block Section not found in the system.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // Confirmation message before deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to remove Subject '{subjectName}' from Block Section '{blockSectionName}'?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                {
                    return; // If the user selects "No", do nothing
                }

                // Delete the record from the subject_list table
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                DELETE FROM subject_list 
                WHERE blockSectionId = @blockSectionId 
                AND subjectId = @subjectId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                    cmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject removed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCurriculum();
                    }
                    else
                    {
                        MessageBox.Show("No matching subject found for the given Block Section.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
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

        
    }
}

using Microsoft.Win32;
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

namespace Info_module.Pages.TableMenus.After_College_Selection.CSVMenu
{
    /// <summary>
    /// Interaction logic for CurriculumConfigurationxaml.xaml
    /// </summary>
    public partial class CurriculumConfiguration : Page
    {
        public int DepartmentId { get; set; }
        public int CurriculumId { get; set; }

        string connectionString = App.ConnectionString;

        string CurriculumRevision;


        public CurriculumConfiguration(int departmentId, int curriculumId)
        {
            InitializeComponent();

            DepartmentId = departmentId;
            CurriculumId = curriculumId;

            LoadUI();
            LoadDepartmentitems();
        }

        #region UI

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
                    curriculumRevision_txt.Text = curriculumRevision;
                    CurriculumRevision = curriculumRevision;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }

            LoadCurriculum();
            Load_Curriculum_Subject_Grid();
            Load_SubjectTab_Subject_Grid();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigationService.Navigate(new CurriculumMenu(DepartmentId));
        }

        #endregion

        #region Curriculum

        private void LoadCurriculum()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
            SELECT
                s.semester_id AS 'Semester_Id',
                s.year_level AS `Year_Level`,
                s.semester AS `Semester`,
                sub.Subject_Id AS 'Subject_Id',
                d.Dept_Code AS 'Serving_Department',
                sub.Subject_Code AS `Subject_Code`,
                sub.Subject_Title AS `Subject_Title`,
                sub.Subject_Type AS `Subject_Type`,
                sub.Lecture_Lab AS `Lecture_Lab`,
                sub.Hours AS `Hours`,
                sub.Units AS `Units`
            FROM semester s
            LEFT JOIN semester_subject_list sl ON s.semester_id = sl.semester_id
            LEFT JOIN subjects sub ON sl.subject_id = sub.Subject_Id
            LEFT JOIN curriculum c ON s.curriculum_id = c.Curriculum_Id 
            LEFT JOIN departments d ON sub.Dept_Id = d.Dept_Id
            WHERE s.curriculum_id = @curriculumId
            ORDER BY s.year_level, 
                     FIELD(COALESCE(s.semester, ''), '1', '2', 'Summer');";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Remove duplicate values for "Year Level" and "Semester"
                    RemoveDuplicateYearSemester(dataTable);

                    // Assuming you have a DataGrid named 'curriculumSubjects_data'
                    curriculum_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum subjects: " + ex.Message);
            }
        }

        private void RemoveDuplicateYearSemester(DataTable dataTable)
        {
            // Add the Hide_Year_Semester column to the DataTable if it doesn't exist
            if (!dataTable.Columns.Contains("Hide_Year_Semester"))
            {
                dataTable.Columns.Add("Hide_Year_Semester", typeof(bool));
            }
            string previousYearLevel = "";
            string previousSemester = "";

            foreach (DataRow row in dataTable.Rows)
            {
                string currentYearLevel = row["Year_Level"].ToString();
                string currentSemester = row["Semester"].ToString();

                // Check if the current row has the same Year Level and Semester as the previous one
                if (currentYearLevel == previousYearLevel && currentSemester == previousSemester)
                {
                    row["Hide_Year_Semester"] = true; // Custom flag to mark rows to be hidden
                }
                else
                {
                    row["Hide_Year_Semester"] = false; // Custom flag for visible rows
                    previousYearLevel = currentYearLevel;
                    previousSemester = currentSemester;
                }
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
                        Subject_Id AS Curriculum_Selected_Subject_ID, 
                        Subject_Code AS Curriculum_Selected_Subject_Code, 
                        Subject_Title AS Curriculum_Selected_Subject_Tittle 
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
                    sem_Subject_data.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        int selectedSemesterId;
        int selectedSubjectId;
        int selectedYear;
        string selectedSemester;

        private void curriculum_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            

            if (curriculum_data.SelectedItem is DataRowView selectedRow)
            {
                selectedSemesterId = selectedRow["Semester_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Semester_Id"]) : 0;
                selectedSubjectId = selectedRow["Subject_Id"] != DBNull.Value ? Convert.ToInt32(selectedRow["Subject_Id"]) : 0;
                selectedYear = selectedRow["Year_Level"] != DBNull.Value ? Convert.ToInt32(selectedRow["Year_Level"]) : 0;
                selectedSemester = selectedRow["Semester"] != DBNull.Value ? selectedRow["Semester"].ToString() : string.Empty;

               

                foreach (ComboBoxItem item in curriculum_year_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedYear.ToString())
                    {
                        curriculum_year_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedSemesterText = selectedSemester == "1" ? "1st Semester"
                           : selectedRow["Semester"].ToString() == "2" ? "2nd Semester"
                           : "Summer";

                foreach (ComboBoxItem item in curriculum_semester_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedSemesterText)
                    {
                        curriculum_semester_cmbx.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        int curriculum_selected_subject_id;

        private void sem_Subject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sem_Subject_data.SelectedItem is DataRowView selectedRow)
            {
                curriculum_selected_subject_id = Convert.ToInt32(selectedRow["Curriculum_Selected_Subject_ID"]);
                blockSection_subjectCode_txt.Text = selectedRow["Curriculum_Selected_Subject_Code"].ToString();;
            }

        }


        private void remove_subject_form_sem_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int subjectId = selectedSubjectId;
                int semesterId = selectedSemesterId;

                // Validate Subject ID (ensure that a subject is selected)
                if (subjectId <= 0)
                {
                    MessageBox.Show("Please Select a Subject in the Curriculum", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate Semester ID (ensure that a valid semester is selected)
                if (semesterId <= 0)
                {
                    MessageBox.Show("Please select a valid semester.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Remove subject from semester
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the combination of semester_id and subject_id exists in the semester_subject_list table
                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM semester_subject_list
                WHERE semester_id = @semesterId AND subject_id = @subjectId";

                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@semesterId", semesterId);
                    checkCmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount == 0)
                    {
                        MessageBox.Show("This subject is not linked to the selected semester.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Delete the subject from the semester_subject_list table
                    string deleteQuery = @"
                DELETE FROM semester_subject_list
                WHERE semester_id = @semesterId AND subject_id = @subjectId";

                    MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, connection);
                    deleteCmd.Parameters.AddWithValue("@semesterId", semesterId);
                    deleteCmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int rowsAffected = deleteCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Subject successfully removed from the semester.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to remove subject from semester.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Optionally, reload the list of subjects linked to the semester
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



        private void subject_add_to_sem_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int subjectId = curriculum_selected_subject_id;
                int yearLevel;

                // Validate and override BlockSectionId if user input exists and is valid
                if (!int.TryParse(curriculum_year_cmbx.SelectedValue?.ToString(), out yearLevel))
                {
                    MessageBox.Show("Please select a valid year level.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if semester is null or empty
                string semester = curriculum_semester_cmbx.SelectedValue?.ToString();
                if (string.IsNullOrEmpty(semester))
                {
                    MessageBox.Show("Please select a semester.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    // First, retrieve the semester_id based on Year_Level, Semester, and Curriculum_Id
                    string getSemesterIdQuery = @"
                        SELECT semester_id 
                        FROM semester 
                        WHERE year_level = @yearLevel 
                        AND semester = @semester 
                        AND curriculum_id = @curriculumId";

                    MySqlCommand getSemesterIdCmd = new MySqlCommand(getSemesterIdQuery, connection);
                    getSemesterIdCmd.Parameters.AddWithValue("@yearLevel", yearLevel);
                    getSemesterIdCmd.Parameters.AddWithValue("@semester", semester);
                    getSemesterIdCmd.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    object result = getSemesterIdCmd.ExecuteScalar();


                    int semesterId = Convert.ToInt32(result);

                    // Now check if the combination of semester_id and subject_id already exists in the semester_subject_list table
                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM semester_subject_list
                        WHERE semester_id = @semesterId AND subject_id = @subjectId";

                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@semesterId", semesterId);
                    checkCmd.Parameters.AddWithValue("@subjectId", subjectId);

                    int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (existingCount > 0)
                    {
                        MessageBox.Show("This subject is already linked to the selected year level, semester, and curriculum.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Insert new subject into the subject_list table
                    string insertQuery = @"
                INSERT INTO semester_subject_list (semester_id, subject_id) 
                VALUES (@semester_id, @subject_id)";

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("@blockSection_id", semesterId);
                    insertCmd.Parameters.AddWithValue("@subject_id", subjectId);

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


        #endregion

        #region Subjects

        private void Load_SubjectTab_Subject_Grid(string statusFilter = "Active")
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
                    subjectTab_subject_grid.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                string selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                Load_SubjectTab_Subject_Grid(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {

                    remove_btn.Content = "Deactivate";
                    remove_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {

                    remove_btn.Content = "Activate";
                    remove_btn.FontSize = 12;
                }
                else
                {

                    remove_btn.Content = "Switch Status";
                    remove_btn.FontSize = 8;
                }
            }
        }

        int subjectTab_SelectedSubjectId;
        string subjectTab_SelectedSubjectDepartment;
        string subjectTab_SelectedSubjectCode;
        string subjectTab_SelectedSubjectTitle;
        string subjectTab_SelectedSubjectType;
        string subjectTab_SelectedSubjectLABorLEC;
        int subjectTab_SelectedSubjectHour;
        int subjectTab_SelectedSubjectUnit;
        int subjectTab_SelectedSubjectStatus;


        private void subjectTab_subject_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subjectTab_subject_grid.SelectedItem is DataRowView selectedRow)
            {
                subjectTab_SelectedSubjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                subjectTab_SelectedSubjectDepartment = selectedRow["Subject_Department"].ToString();
                subjectTab_SelectedSubjectCode = selectedRow["Subject_Code"].ToString();
                subjectTab_SelectedSubjectTitle = selectedRow["Subject_Title"].ToString();
                subjectTab_SelectedSubjectType = selectedRow["Subject_Type"].ToString();
                subjectTab_SelectedSubjectLABorLEC = selectedRow["LEC_LAB"].ToString();
                subjectTab_SelectedSubjectHour = Convert.ToInt32(selectedRow["Hours"]);
                subjectTab_SelectedSubjectUnit = Convert.ToInt32(selectedRow["Units"]);
                subjectTab_SelectedSubjectStatus = (selectedRow["Status"].ToString() == "Active") ? 1 : 0;


                //fill up text boxes
                subjectId_txt.Text = subjectTab_SelectedSubjectId.ToString();
                subjectCode_txt.Text = subjectTab_SelectedSubjectCode.ToString();
                subjectTitle_txt.Text = subjectTab_SelectedSubjectTitle.ToString();
                hours_txt.Text = subjectTab_SelectedSubjectHour.ToString();
                units_txt.Text = subjectTab_SelectedSubjectUnit.ToString();

                //match combobox

                SetSelectedDepartment(subjectTab_SelectedSubjectDepartment);

                string selectedSubjectTypeText = subjectTab_SelectedSubjectType == "major" ? "Major" : "minor";
                foreach (ComboBoxItem item in subjectType_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedSubjectTypeText)
                    {
                        subjectType_cmbx.SelectedItem = item;
                        break;
                    }
                }

                string selectedLecLabText = subjectTab_SelectedSubjectLABorLEC == "LEC" ? "Lecture" : "Laboratory";
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

        private void ClearSubjectInputs()
        {
            subjectId_txt.Clear();
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
                        Load_SubjectTab_Subject_Grid();
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
                            Load_SubjectTab_Subject_Grid();
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
                if (subjectTab_subject_grid.SelectedItem == null)
                {
                    MessageBox.Show("Please select a subject to update its status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the selected subject's ID and current status
                DataRowView selectedRow = (DataRowView)subjectTab_subject_grid.SelectedItem;

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
                Load_SubjectTab_Subject_Grid();
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

            MessageBox.Show("Please ensure the CSV columns are: Year Level, Semester, Subject Code, Serving Department, Subject Title, Subject Type, Lecture Lab, Hours, and Units.",
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

                        if (rows.Length != 9)
                        {
                            MessageBox.Show("Error: Curriculum CSV must have exactly 9 columns.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr[0] = rows[0].Trim(); // Year Level
                            dr[1] = rows[1].Trim(); // Semester
                            dr[2] = rows[2].Trim().ToUpper(); // Subject Code
                            dr[3] = rows[3].Trim().ToUpper(); // Serving Department
                            dr[4] = rows[4].Trim(); // Subject Title
                            dr[5] = rows[5].Trim(); // Subject Type
                            dr[6] = rows[6].Trim().ToUpper(); // Lecture Lab
                            dr[7] = Convert.ToInt32(rows[7].Trim()); // Hours
                            dr[8] = Convert.ToInt32(rows[8].Trim()); // Units
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
            // **Confirmation Dialog Before Saving**
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to add subjects to the curriculum: {CurriculumRevision}?",
                "Confirm Curriculum Save",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;
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
                            int yearLevel = Convert.ToInt32(row["Year_Level"]);
                            int semester = Convert.ToInt32(row["Semester"]);

                            // Check if Block Section exists, add if it doesn't
                            int semesterId = GetOrInsertBlockSection(connection, yearLevel, semester);
                            ProcessSubject(row, connection, semesterId);
                            LoadCurriculum();
                        }
                    }
                    else
                    {

                        foreach (DataRowView item in curriculum_data.Items)
                        {

                            DataRow row = item.Row;
                            int semesterId = Convert.ToInt32(CSVblockSection_cmbx.SelectedValue);
                            ProcessSubject(row, connection, semesterId);
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

        private void ProcessSubject(DataRow row, MySqlConnection connection, int semesterId)
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
                if (semesterId != 0)
                {
                    InsertSubjectList(connection, semesterId, subjectId, subjectCode);
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

        private void InsertSubjectList(MySqlConnection connection, int semesterId, int subjectId, string subjectCode)
        {
            // Check if the blockSectionId + subjectId combination already exists
            string checkQuery = @"
        SELECT subjectList_Id FROM subject_list 
        WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId;";
            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@blockSectionId", semesterId);
                checkCmd.Parameters.AddWithValue("@subjectId", subjectId);
                object result = checkCmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    MessageBox.Show($"Subject '{subjectCode}' is already linked to Block Section {semesterId}.",
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
                insertCmd.Parameters.AddWithValue("@blockSectionId", semesterId);
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

        private int GetOrInsertBlockSection(MySqlConnection connection, int yearLevel, int semester)
        {


            // Check if block section exists
            string checkQuery = "SELECT semester_id FROM semester WHERE year_level = @YearLevel AND semester = @Semester;";
            using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
            {
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
            INSERT INTO semester (curriculumId, year_level, Semester) 
            VALUES (@curriculumId, @YearLevel, @Semester);
            SELECT LAST_INSERT_ID();";
            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@curriculumId", CurriculumId); // Replace CurriculumId with the actual curriculumId variable
                insertCmd.Parameters.AddWithValue("@YearLevel", yearLevel);
                insertCmd.Parameters.AddWithValue("@Semester", semester);
                int newBlockSectionId = Convert.ToInt32(insertCmd.ExecuteScalar());
                return newBlockSectionId;
            }
        }


        #endregion

    }
}

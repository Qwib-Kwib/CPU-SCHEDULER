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

using static Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu;
using ZstdSharp.Unsafe;

namespace Info_module.Pages.TableMenus.After_College_Selection.CSVMenu
{
    /// <summary>
    /// Interaction logic for CurriculumCSV.xaml
    /// </summary>
    public partial class CurriculumCSV : Page
    {
        public int DepartmentId { get; set; }
        public int CurriculumId { get; set; }

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

            LoadSubject();
            LoadDepartmentitems();



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
                using(MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Id, Dept_Code FROM departments";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    List<Department> departments = new List<Department>();
                    using(MySqlDataReader reader = command.ExecuteReader())
                    {
                        while(reader.Read())
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



        #endregion

        #region Data Grid

        private void LoadSubject(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    s.Subject_Id AS Subject_Id,
                    d.Dept_Code AS Serving_Department,
                    s.Year_Level AS Year_Level,
                    s.Semester AS Semester,
                    s.Subject_Code AS Subject_Code,
                    s.Subject_Title AS Subject_Title,
                    s.Subject_Type AS Subject_Type,
                    s.Lecture_Lab AS Lecture_Lab,
                    s.Hours AS Hours,
                    s.Units AS Units,
                    CASE
                        WHEN s.Status = 1 Then 'Active'
                        ELSE 'Inactive'
                    END AS 'Status'
                FROM subjects s
                JOIN departments d ON s.Dept_Id = d.Dept_Id
                JOIN curriculum_subjects cs ON s.Subject_Id = cs.Subject_Id
                WHERE cs.Curriculum_Id = @curriculumId";

                    if (statusFilter == "Active")
                    {
                        query += " AND s.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND s.Status = 0";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Bind the DataTable directly to the DataGrid
                    subject_data.ItemsSource = dt.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedStatus = "Active";
            if (switch_btn == null)
            {
                return;
            }

            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                // Load the curriculum data filtered by the selected status
                LoadSubject(selectedStatus);

                // Dynamically change the button content based on the selected status
                if (selectedStatus == "Active")
                {
                    switch_btn.Content = "Deactivate";
                    switch_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    switch_btn.Content = "Activate";
                    switch_btn.FontSize = 12;
                }
                else
                {
                    switch_btn.Content = "Switch Status";
                    switch_btn.FontSize = 12;
                }
            }
        }

        private void subject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subject_data.SelectedItem is DataRowView selectedRow)
            {
                selectedSubjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                selectedServingDept = selectedRow["Serving_Department"].ToString();
                selectedYear = Convert.ToInt32(selectedRow["Year_Level"]);
                selectedSemester = selectedRow["Semester"].ToString();
                selectedSubjectCode = selectedRow["Subject_Code"].ToString();
                selectedSubjectTittle = selectedRow["Subject_Title"].ToString();
                selectedSubjectType = selectedRow["Subject_Type"].ToString();
                selectedLABorLEC = selectedRow["Lecture_Lab"].ToString();
                selectedHour = Convert.ToInt32(selectedRow["Hours"]);
                selectedUnit = Convert.ToInt32(selectedRow["Units"]);
                selectedStatus = (selectedRow["Status"].ToString() == "Active") ? 1 : 0;


                //fill up text boxes
                subjectId_txt.Text = selectedSubjectId.ToString();
                yearLevel_txt.Text = selectedYear.ToString();
                subjectCode_txt.Text = selectedSubjectCode.ToString();
                subjecTitle_txt.Text = selectedSubjectTittle.ToString();
                hours_txt.Text = selectedHour.ToString();
                units_txt.Text = selectedUnit.ToString();

                //match combobox

                SetSelectedDepartment(selectedServingDept);


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

        #endregion

        #region Forms

        private void hours_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void units_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }
        private void ClearFormInputs()
        {
            subjectId_txt.Clear();
            servingDepartment_cmbx.SelectedItem = null;
            yearLevel_txt.Clear();
            subjectCode_txt.Clear();
            semester_cmbx.SelectedItem = null;
            subjecTitle_txt.Clear();
            subjectType_cmbx.SelectedItem = null;
            lecLab_cmbx.SelectedItem = null;
            hours_txt.Clear();
            units_txt.Clear();
        }


        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validation: Check if all fields are filled
            if (string.IsNullOrWhiteSpace(subjectId_txt.Text) ||
                servingDepartment_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(yearLevel_txt.Text) ||
                string.IsNullOrWhiteSpace(subjectCode_txt.Text) ||
                semester_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(subjecTitle_txt.Text) ||
                subjectType_cmbx.SelectedItem == null ||
                lecLab_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(hours_txt.Text) ||
                string.IsNullOrWhiteSpace(units_txt.Text))
            {
                MessageBox.Show("Please fill out all fields before adding a Subject.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Stop execution if validation fails
            }

            // Additional validation: Ensure Hours and Units are valid integers
            if (!int.TryParse(hours_txt.Text, out int hours) || !int.TryParse(units_txt.Text, out int units))
            {
                MessageBox.Show("Hours and Units must be valid numbers.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int newSubjectId;
            int curriculumId = CurriculumId; // Assuming curriculum_cmbx holds curriculum_id

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Insert into the subjects table
                    string insertSubjectQuery = @"INSERT INTO subjects (Dept_Id, Year_Level, Semester, Subject_Code, Subject_Title, Subject_Type, Lecture_Lab, Hours, Units)
                                          VALUES (@Dept_Id, @Year_Level, @Semester, @Subject_Code, @Subject_Title, @Subject_Type, @Lecture_Lab, @Hours, @Units)";
                    using (MySqlCommand command = new MySqlCommand(insertSubjectQuery, conn))
                    {
                        // Set the parameter values
                        command.Parameters.AddWithValue("@Dept_Id", (servingDepartment_cmbx.SelectedItem as Department)?.DepartmentIds);
                        command.Parameters.AddWithValue("@Year_Level", yearLevel_txt.Text);
                        command.Parameters.AddWithValue("@Semester", (semester_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Subject_Code", subjectCode_txt.Text);
                        command.Parameters.AddWithValue("@Subject_Title", subjecTitle_txt.Text);
                        command.Parameters.AddWithValue("@Subject_Type", (subjectType_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Lecture_Lab", (lecLab_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Hours", hours);
                        command.Parameters.AddWithValue("@Units", units);

                        // Execute the query to insert into the subjects table
                        command.ExecuteNonQuery();

                        // Retrieve the new Subject_Id (Last Inserted ID)
                        newSubjectId = (int)command.LastInsertedId;
                    }

                    // Insert into the curriculum_subjects table
                    string insertCurriculumSubjectQuery = @"INSERT INTO curriculum_subjects (subject_id, curriculum_id) 
                                                    VALUES (@Subject_Id, @Curriculum_Id)";
                    using (MySqlCommand curriculumCommand = new MySqlCommand(insertCurriculumSubjectQuery, conn))
                    {
                        curriculumCommand.Parameters.AddWithValue("@Subject_Id", newSubjectId);
                        curriculumCommand.Parameters.AddWithValue("@Curriculum_Id", curriculumId);

                        // Execute the query to insert into curriculum_subjects table
                        curriculumCommand.ExecuteNonQuery();
                    }
                }

                // Inform the user that the subject was added successfully
                MessageBox.Show("Subject added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSubject();
                ClearFormInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding subject: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void update_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validation: Ensure all fields are filled out before proceeding
            if (string.IsNullOrWhiteSpace(subjectId_txt.Text) ||
                servingDepartment_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(yearLevel_txt.Text) ||
                string.IsNullOrWhiteSpace(subjectCode_txt.Text) ||
                semester_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(subjecTitle_txt.Text) ||
                subjectType_cmbx.SelectedItem == null ||
                lecLab_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(hours_txt.Text) ||
                string.IsNullOrWhiteSpace(units_txt.Text))
            {
                MessageBox.Show("Please fill out all fields before updating the Subject.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Additional validation: Ensure Hours and Units are valid integers
            if (!int.TryParse(hours_txt.Text, out int hours) || !int.TryParse(units_txt.Text, out int units))
            {
                MessageBox.Show("Hours and Units must be valid numbers.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Subject_Id should be retrieved or already available
            int subjectId = int.Parse(subjectId_txt.Text); // Assuming subjectId_txt is populated with the Subject_Id

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL query to update the existing subject record
                    string updateQuery = @"UPDATE subjects 
                                   SET Dept_Id = @Dept_Id,
                                       Year_Level = @Year_Level,
                                       Semester = @Semester,
                                       Subject_Code = @Subject_Code,
                                       Subject_Title = @Subject_Title,
                                       Subject_Type = @Subject_Type,
                                       Lecture_Lab = @Lecture_Lab,
                                       Hours = @Hours,
                                       Units = @Units
                                   WHERE Subject_Id = @Subject_Id";

                    using (MySqlCommand command = new MySqlCommand(updateQuery, conn))
                    {
                        // Set the parameter values
                        command.Parameters.AddWithValue("@Dept_Id", (servingDepartment_cmbx.SelectedItem as Department)?.DepartmentIds);
                        command.Parameters.AddWithValue("@Year_Level", yearLevel_txt.Text);
                        command.Parameters.AddWithValue("@Semester", (semester_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Subject_Code", subjectCode_txt.Text);
                        command.Parameters.AddWithValue("@Subject_Title", subjecTitle_txt.Text);
                        command.Parameters.AddWithValue("@Subject_Type", (subjectType_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Lecture_Lab", (lecLab_cmbx.SelectedItem as ComboBoxItem)?.Tag.ToString());
                        command.Parameters.AddWithValue("@Hours", hours);
                        command.Parameters.AddWithValue("@Units", units);
                        command.Parameters.AddWithValue("@Subject_Id", subjectId); // This is the ID of the subject being updated

                        // Execute the update query
                        command.ExecuteNonQuery();
                    }
                }

                // Inform the user that the subject was updated successfully
                MessageBox.Show("Subject updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSubject();
                ClearFormInputs(); // Optionally clear the form inputs after updating
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating subject: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFormInputs ();
        }

        private void switch_btn_Click(object sender, RoutedEventArgs e)
        {
            if (subject_data.SelectedItems.Count > 0)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        foreach (DataRowView rowView in subject_data.SelectedItems)
                        {
                            DataRow row = rowView.Row;
                            int subjectId = selectedSubjectId;
                            int currentStatus = selectedStatus;
                            

                            int newStatus = (currentStatus == 1) ? 0 : 1;

                            string query = "UPDATE subjects SET Status = @Status WHERE Subject_Id = @SubjectId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Status", newStatus);
                                command.Parameters.AddWithValue("@SubjectId", subjectId);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSubject(); // Refresh data after updating status
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

        private void csv_btn_Click(object sender, RoutedEventArgs e)
        {
            form_grid.Visibility = Visibility.Hidden;
            csv_grid.Visibility = Visibility.Visible;
            csv_grid.Margin = new Thickness(10, 0, 10, 0);

        }



        #endregion

        #region CSV
        private void Upload_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please ensure the CSV columns are: Subject Id, Serving Department, Year Level, Semester, Subject Code, Subject Title, Subject Type, Lecture Lab, Hours, and Units.");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadCsvFile(filePath);
                    if (dataTable != null)
                    {
                        subject_data.ItemsSource = dataTable.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to read CSV file into DataTable
        private DataTable ReadCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns based on the expected CSV format
                    csvData.Columns.Add("Subject_Id", typeof(int)); // Assuming Subject Id is an integer
                    csvData.Columns.Add("Serving_Department", typeof(string));
                    csvData.Columns.Add("Year_Level", typeof(string));
                    csvData.Columns.Add("Semester", typeof(string));
                    csvData.Columns.Add("Subject_Code", typeof(string));
                    csvData.Columns.Add("Subject_Title", typeof(string));
                    csvData.Columns.Add("Subject_Type", typeof(string));
                    csvData.Columns.Add("Lecture_Lab", typeof(string));
                    csvData.Columns.Add("Hours", typeof(string));
                    csvData.Columns.Add("Units", typeof(string));

                    // Read the header line first to skip it
                    sr.ReadLine();

                    // Read the data lines
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        // Ensure that the CSV row has the expected number of columns
                        if (rows.Length != 10)
                        {
                            MessageBox.Show("Error: CSV file format is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr["Subject_Id"] = string.IsNullOrEmpty(rows[0].Trim()) ? -1 : Convert.ToInt32(rows[0].Trim()); // Use -1 if ID is empty
                            dr["Serving_Department"] = rows[1].Trim();
                            dr["Year_Level"] = rows[2].Trim();
                            dr["Semester"] = rows[3].Trim();
                            dr["Subject_Code"] = rows[4].Trim();
                            dr["Subject_Title"] = rows[5].Trim();
                            dr["Subject_Type"] = rows[6].Trim();
                            dr["Lecture_Lab"] = rows[7].Trim();
                            dr["Hours"] = rows[8].Trim();
                            dr["Units"] = rows[9].Trim();

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
                MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return csvData;
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            form_grid.Visibility = Visibility.Visible;
            csv_grid.Visibility = Visibility.Hidden;
            csv_grid.Margin = new Thickness(-220, 0, 240, 0);
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    int curriculumId = Convert.ToInt32(CurriculumId);

                    // Delete existing curriculum-subject bindings for the current Curriculum_Id
                    string deleteQuery = "DELETE FROM curriculum_subjects WHERE curriculum_id = @Curriculum_Id;";
                    MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                    deleteCommand.ExecuteNonQuery();

                    foreach (DataRowView item in subject_data.Items)
                    {
                        DataRow row = item.Row;

                        string deptCode = row["Serving_Department"].ToString(); // Assuming this is the Dept_Code in your DataGrid

                        // Query to retrieve Dept_Id based on Dept_Code
                        string deptIdQuery = "SELECT Dept_Id FROM departments WHERE Dept_Code = @Dept_Code;";
                        MySqlCommand deptIdCmd = new MySqlCommand(deptIdQuery, connection);
                        deptIdCmd.Parameters.AddWithValue("@Dept_Code", deptCode);

                        object deptIdObj = deptIdCmd.ExecuteScalar();
                        if (deptIdObj == null || deptIdObj == DBNull.Value)
                        {
                            MessageBox.Show($"Department with Dept_Code '{deptCode}' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        int deptId = Convert.ToInt32(deptIdObj);

                        // Check if the Subject_Code already exists in the subjects table
                        string subjectCode = row["Subject_Code"].ToString();
                        string subjectCheckQuery = "SELECT Subject_Id FROM subjects WHERE Subject_Code = @Subject_Code AND Dept_Id = @Dept_Id;";
                        MySqlCommand subjectCheckCmd = new MySqlCommand(subjectCheckQuery, connection);
                        subjectCheckCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                        subjectCheckCmd.Parameters.AddWithValue("@Dept_Id", deptId);

                        object subjectIdObj = subjectCheckCmd.ExecuteScalar();
                        int subjectId;

                        if (subjectIdObj == null || subjectIdObj == DBNull.Value)
                        {
                            // If the subject doesn't exist, insert it into the subjects table
                            string insertSubjectQuery = @"
                        INSERT INTO subjects (Dept_Id, Year_Level, Semester, Subject_Code, Subject_Title, Subject_Type, Lecture_Lab, Hours, Units)
                        VALUES (@Dept_Id, @Year_Level, @Semester, @Subject_Code, @Subject_Title, @Subject_Type, @Lecture_Lab, @Lec_Hours, @Credit_Units);
                        SELECT LAST_INSERT_ID();";

                            MySqlCommand insertSubjectCmd = new MySqlCommand(insertSubjectQuery, connection);
                            insertSubjectCmd.Parameters.AddWithValue("@Dept_Id", deptId);
                            insertSubjectCmd.Parameters.AddWithValue("@Year_Level", Convert.ToInt32(row["Year_Level"]));
                            insertSubjectCmd.Parameters.AddWithValue("@Semester", row["Semester"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Title", row["Subject_Title"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Type", row["Subject_Type"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Lecture_Lab", row["Lecture_Lab"].ToString());

                            // Validate and convert Lec Hours
                            if (!int.TryParse(row["Hours"].ToString(), out int lecHours))
                            {
                                MessageBox.Show("Invalid value for Lec Hours: " + row["Hours"].ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            insertSubjectCmd.Parameters.AddWithValue("@Lec_Hours", lecHours);

                            // Validate and convert Credit Units
                            if (!int.TryParse(row["Units"].ToString(), out int creditUnits))
                            {
                                MessageBox.Show("Invalid value for Credit Units: " + row["Units"].ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            insertSubjectCmd.Parameters.AddWithValue("@Credit_Units", creditUnits);

                            subjectId = Convert.ToInt32(insertSubjectCmd.ExecuteScalar());
                        }
                        else
                        {
                            // If the subject exists, use the existing Subject_Id
                            subjectId = Convert.ToInt32(subjectIdObj);
                        }

                        // Insert into curriculum_subjects to create the binding between subject and curriculum
                        string insertCurriculumSubjectQuery = @"
                    INSERT INTO curriculum_subjects (curriculum_id, subject_id)
                    VALUES (@Curriculum_Id, @Subject_Id);";

                        MySqlCommand insertCurriculumSubjectCmd = new MySqlCommand(insertCurriculumSubjectQuery, connection);
                        insertCurriculumSubjectCmd.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                        insertCurriculumSubjectCmd.Parameters.AddWithValue("@Subject_Id", subjectId);

                        insertCurriculumSubjectCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Data inserted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSubject();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data into database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }







        #endregion

        
    }
}

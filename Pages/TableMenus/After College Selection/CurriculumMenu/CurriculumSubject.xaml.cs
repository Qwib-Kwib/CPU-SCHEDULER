using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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

namespace Info_module.Pages.TableMenus.After_College_Selection.CurriculumMenu
{
    /// <summary>
    /// Interaction logic for CurriculumSubject.xaml
    /// </summary>
    public partial class CurriculumSubject : Window
    {
        string connectionString = App.ConnectionString;

        public int SubjectId { get; set; }

        public CurriculumSubject(bool isEditClick, int subjectId)
        {
            InitializeComponent();
            LoadDepartmentitems();
            SubjectId = subjectId;
            EditORAddMode(isEditClick);
        }

        public void EditORAddMode(bool isEditClick)
        {
            if (!isEditClick)
            {
                add_btn.Visibility = Visibility.Visible;
                update_btn.Visibility = Visibility.Hidden;
                this.Title = "Subject Add";
            }
            else
            {
                add_btn.Visibility = Visibility.Collapsed;
                update_btn.Visibility = Visibility.Visible;

                this.Title = "Subject Edit";
                LoadForms(SubjectId);
            }
        }

        private void LoadForms(int subjectId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load a specific subject by Subject_Id
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
                    departments d ON s.Dept_Id = d.Dept_Id
                WHERE 
                    s.Subject_Id = @Subject_Id"; // Filter by Subject_Id

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Subject_Id", subjectId); // Add parameter for Subject_Id

                    // Execute the command and read the data
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) // Read the first row
                        {
                            // Populate the textboxes with the data
                            subjectId_txt.Text = reader["Subject_Id"].ToString();
                            subjectCode_txt.Text = reader["Subject_Code"].ToString();
                            string subjectDepartment = reader["Subject_Department"].ToString();
                            subjectTitle_txt.Text = reader["Subject_Title"].ToString();
                            string subjectType = reader["Subject_Type"].ToString();
                            string lectureLab = reader["LEC_LAB"].ToString();
                            hours_txt.Text = reader["Hours"].ToString();
                            units_txt.Text = reader["Units"].ToString();

                            string selectedSubjectTypeText = subjectType == "major" ? "Major" : "minor";
                            foreach (ComboBoxItem item in subjectType_cmbx.Items)
                            {
                                if (item.Content.ToString() == selectedSubjectTypeText)
                                {
                                    subjectType_cmbx.SelectedItem = item;
                                    break;
                                }
                            }

                            string selectedLecLabText = lectureLab == "LEC" ? "Lecture" : "Laboratory";
                            foreach (ComboBoxItem item in lecLab_cmbx.Items)
                            {
                                if (item.Content.ToString() == selectedLecLabText)
                                {
                                    lecLab_cmbx.SelectedItem = item;
                                    break;
                                }
                            }

                            var department = servingDepartment_cmbx.Items.OfType<Department>()
                                .FirstOrDefault(d => d.DepartmentCodes == subjectDepartment);

                            if (department != null)
                            {
                                servingDepartment_cmbx.SelectedItem = department;
                            }

                        }
                        else
                        {
                            MessageBox.Show("No subject found with the specified ID.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subject: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void add_btn_Click(object sender, RoutedEventArgs e)
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

                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Subject Added", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
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
                        cmd.ExecuteNonQuery();

                    }
                }
                MessageBox.Show("Subject updated", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}

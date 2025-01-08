using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorMenuAdd.xaml
    /// </summary>
    public partial class InstructorMenuAdd : Window
    {
        string connectionString = App.ConnectionString;
        public int CollegeId { get; set; }

        public InstructorMenuAdd(int collegeId)
        {
            InitializeComponent();
            CollegeId = collegeId;
        }
    
        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private bool IsValidEmail(string email)
        {
            // Regular expression for validating an email address
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve data from input fields
            string employeeId = employeeId_txt.Text;
            string firstName = firstName_txt.Text;
            string middleName = middleName_txt.Text;
            string lastName = lastName_txt.Text;
            string email = email_txt.Text;
            string sex = male_rbtn.IsChecked == true ? "M" : female_rbtn.IsChecked == true ? "F" : "";
            int disability = disability_ckbox.IsChecked == true ? 1 : 0;

            if (employeeType_cmbx.SelectedValue == null)
            {
                MessageBox.Show("Please select an Employment Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email is invalid.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                MessageBox.Show("Please enter an employee Id", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Please enter a last name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Please enter a first name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Insert into instructor table, including disability
                    string query = @"
                INSERT INTO instructor (Dept_Id, Employee_Id, Fname, Mname, Lname, Employment_Type, Employee_Sex, Email, Disability) 
                VALUES (@Dept_Id, @Employee_Id, @Fname, @Mname, @Lname, @Employment_Type, @Employee_Sex, @Email, @Disability)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Dept_Id", CollegeId);
                    command.Parameters.AddWithValue("@Employee_Id", employeeId);
                    command.Parameters.AddWithValue("@Fname", firstName);
                    command.Parameters.AddWithValue("@Mname", middleName);
                    command.Parameters.AddWithValue("@Lname", lastName);
                    command.Parameters.AddWithValue("@Employment_Type", ((ComboBoxItem)employeeType_cmbx.SelectedItem).Tag.ToString());
                    command.Parameters.AddWithValue("@Employee_Sex", sex);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Disability", disability); // Handle disability

                    command.ExecuteNonQuery();
                    MessageBox.Show("Instructor added successfully!");

                    // Get the last inserted ID to use for availability
                    long instructorId = command.LastInsertedId;

                    // Create availability for the new instructor
                    createTimeAvailability(instructorId);
                }
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding instructor: " + ex.Message);
            }
        }

        private void createTimeAvailability(long instructorId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO instructor_availability (Internal_Employee_Id, Day_Of_Week, Start_Time, End_Time)
                SELECT @instructorId, day, @startTime, @endTime
                FROM (
                    SELECT 'Monday' AS day
                    UNION ALL
                    SELECT 'Tuesday'
                    UNION ALL
                    SELECT 'Wednesday'
                    UNION ALL
                    SELECT 'Thursday'
                    UNION ALL
                    SELECT 'Friday'
                    UNION ALL
                    SELECT 'Saturday'
                    UNION ALL
                    SELECT 'Sunday'
                ) AS days WHERE NOT EXISTS (
                    SELECT 1
                    FROM instructor_availability
                    WHERE Internal_Employee_Id = @instructorId
                    AND Day_Of_Week = days.day
                );";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@instructorId", instructorId);
                    command.Parameters.AddWithValue("@startTime", "07:00:00");
                    command.Parameters.AddWithValue("@endTime", "18:00:00"); // Corrected parameter name for end time
                    command.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error creating time availability: " + ex.Message);
            }
        }
    }
    
}

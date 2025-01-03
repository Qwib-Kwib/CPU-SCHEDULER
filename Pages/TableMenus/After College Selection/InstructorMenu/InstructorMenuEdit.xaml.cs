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
    /// Interaction logic for InstructorMenuEdit.xaml
    /// </summary>
    public partial class InstructorMenuEdit : Window
    {
        string connectionString = App.ConnectionString;
        public int InternalEmployeeId { get; set; }

        public InstructorMenuEdit(int internalEmployeeId)
        {
            InitializeComponent();
            InternalEmployeeId = internalEmployeeId;
            PopulateCollegesCodes();
            LoadEmployeeData(internalEmployeeId);
        }
        private void PopulateCollegesCodes()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Id, Dept_Code FROM departments";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    // Assuming buildingCode_cbx is your ComboBox name
                    CollegeCode_cbx.ItemsSource = dataTable.DefaultView;
                    CollegeCode_cbx.DisplayMemberPath = "Dept_Code";
                    CollegeCode_cbx.SelectedValuePath = "Dept_Id";
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building codes: " + ex.Message);
            }
        }

        private void LoadEmployeeData(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to select the department with the specified ID
                    string query = @"
                        SELECT 
                           i.dept_Id AS Department,
                           i.Employee_Id,
                           i.Lname AS LastName, 
                           i.Mname AS MiddleName, 
                           i.Fname AS FirstName, 
                           i.Employment_Type AS Employment, 
                           i.Employee_Sex AS Sex,
                           i.Email,
                           i.Disability
                        FROM instructor i
                        WHERE i.Internal_Employee_Id = @InternalEmployeeId"; // Filter by Department_Id

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@InternalEmployeeId", internalEmployeeId); // Add parameter

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Read the first row
                        {
                            CollegeCode_cbx.SelectedValue = reader["Department"];
                            employeeId_txt.Text = reader["Employee_Id"].ToString();
                            firstName_txt.Text = reader["FirstName"].ToString();
                            middleName_txt.Text = reader["MiddleName"].ToString();
                            lastName_txt.Text = reader["LastName"].ToString();
                            email_txt.Text = reader["Email"].ToString();
                           

                            foreach (ComboBoxItem item in employeeType_cmbx.Items)
                            {
                                if (item.Content.ToString() == reader["Employment"].ToString())
                                {
                                    employeeType_cmbx.SelectedItem = item;
                                    break;
                                }
                            }


                            string sex = reader["Sex"].ToString();
                            if (sex == "M")
                            {
                                male_rbtn.IsChecked = true;
                            }
                            else if (sex == "F")
                            {
                                female_rbtn.IsChecked = true;
                            }

                            object disabilityValue = reader["Disability"];

                            if (disabilityValue != DBNull.Value)
                            {
                                // Check if it's a boolean or an integer ("1" or "0")
                                if (disabilityValue is bool)
                                {
                                    disability_ckbox.IsChecked = (bool)disabilityValue;
                                }
                                else if (disabilityValue is int)
                                {
                                    disability_ckbox.IsChecked = ((int)disabilityValue == 1);
                                }
                                else
                                {
                                    // Handle string or other types, just in case
                                    disability_ckbox.IsChecked = disabilityValue.ToString() == "1";
                                }
                            }
                            else
                            {
                                disability_ckbox.IsChecked = false; // Default to unchecked if no value
                            }
                        }
                        else
                        {
                            MessageBox.Show("No department found with the specified ID.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void update_btn_Click(object sender, RoutedEventArgs e)
        {

            if (CollegeCode_cbx.SelectedValue == null)
            {
                MessageBox.Show("Please select a College.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (employeeType_cmbx.SelectedValue == null)
            {
                MessageBox.Show("Please select a Employemnt Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(email_txt.Text))
            {
                MessageBox.Show("Email is invalid.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;

            }

            if (string.IsNullOrWhiteSpace(employeeId_txt.Text))
            {
                MessageBox.Show("Please enter a employee Id", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(lastName_txt.Text))
            {
                MessageBox.Show("Please enter a last name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(firstName_txt.Text))
            {
                MessageBox.Show("Please enter a first name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                UPDATE instructor 
                SET Dept_Id = @Department, Employee_Id = @Employee_Id, Lname = @LastName, Mname = @MiddleName, Fname = @FirstName, 
                    Employment_Type = @Employment, Employee_Sex = @Sex, Email = @Email, Disability = @Disability
                WHERE Internal_Employee_Id = @Internal_Employee_Id";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Internal_Employee_Id", InternalEmployeeId);
                    command.Parameters.AddWithValue("@Department", CollegeCode_cbx.SelectedValue);
                    command.Parameters.AddWithValue("@Employee_Id", employeeId_txt.Text);
                    command.Parameters.AddWithValue("@LastName", lastName_txt.Text);
                    command.Parameters.AddWithValue("@MiddleName", middleName_txt.Text);
                    command.Parameters.AddWithValue("@FirstName", firstName_txt.Text);
                    command.Parameters.AddWithValue("@Employment", ((ComboBoxItem)employeeType_cmbx.SelectedItem).Tag.ToString());
                    command.Parameters.AddWithValue("@Sex", male_rbtn.IsChecked == true ? "M" : "F");
                    command.Parameters.AddWithValue("@Email", email_txt.Text);
                    command.Parameters.AddWithValue("@Disability", disability_ckbox.IsChecked == true ? 1 : 0); // Handle disability

                    command.ExecuteNonQuery();
                    

                }
                MessageBox.Show("Employee updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }

            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating instructor details: " + ex.Message);
            }
        }
    }
}

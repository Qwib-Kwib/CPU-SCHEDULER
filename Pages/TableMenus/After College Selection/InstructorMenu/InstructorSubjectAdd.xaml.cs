using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Crmf;
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
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorSubjectAdd.xaml
    /// </summary>
    public partial class InstructorSubjectAdd : Window
    {
        public int EmployeeId { get; set; }
        public int DepartmentId { get; set; }

        int selectedSubjectId;

        string connectionString = App.ConnectionString;

        public InstructorSubjectAdd(int employeeId, int departmentId)
        {
            InitializeComponent();
            EmployeeId = employeeId;
            DepartmentId = departmentId;
            LoadSubjects();
        }

        private void LoadSubjects()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Step 1: Get all subjects for the given department, including Subject_Title, ordered by Subject_Code
                    string subjectQuery = @"
                SELECT Subject_Id, Subject_Code, Subject_Title, Lecture_Lab AS LecLab
                FROM subjects
                WHERE Dept_Id = @Dept_Id
                ORDER BY Subject_Code ASC"; // Order by Subject_Code in ascending order

                    MySqlCommand subjectCommand = new MySqlCommand(subjectQuery, connection);
                    subjectCommand.Parameters.AddWithValue("@Dept_Id", DepartmentId);

                    // Create a DataTable to hold the result set
                    DataTable subjectTable = new DataTable();

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(subjectCommand))
                    {
                        adapter.Fill(subjectTable);
                    }

                    // Step 2: Bind the results to the DataGrid
                    subject_grid.ItemsSource = subjectTable.DefaultView; // Assuming subject_grid is your DataGrid name
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message);
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validate selectedSubjectId and EmployeeId
            if (selectedSubjectId <= 0)
            {
                MessageBox.Show("Please select a valid subject.");
                return;
            }

            if (EmployeeId <= 0)
            {
                MessageBox.Show("Please select a valid employee.");
                return;
            }

            // Validate load quantity input
            if (string.IsNullOrWhiteSpace(subjectLoad_txt.Text) || !int.TryParse(subjectLoad_txt.Text, out int loadQuantity) || loadQuantity <= 0)
            {
                MessageBox.Show("Please enter a valid load quantity greater than zero.");
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Step 1: Get Subject_Id and Subject_Code from 'subjects' table
                string getSubjectQuery = "SELECT Subject_Id, Subject_Code FROM subjects WHERE Subject_Id = @subjectId";
                MySqlCommand subjectCmd = new MySqlCommand(getSubjectQuery, conn);
                subjectCmd.Parameters.AddWithValue("@subjectId", selectedSubjectId);

                int subjectId = 0;
                string subjectCode = string.Empty;
                bool subjectExists = false;

                using (MySqlDataReader reader = subjectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        subjectId = reader.GetInt32("Subject_Id");
                        subjectCode = reader.GetString("Subject_Code");
                        subjectExists = true;
                    }
                }

                if (!subjectExists)
                {
                    MessageBox.Show("Invalid Subject_Id provided.");
                    return;
                }

                // Step 2: Get Employee_Id from 'instructor' table
                string getEmployeeQuery = "SELECT Internal_Employee_Id FROM instructor WHERE Internal_Employee_Id = @employeeId";
                MySqlCommand employeeCmd = new MySqlCommand(getEmployeeQuery, conn);
                employeeCmd.Parameters.AddWithValue("@employeeId", EmployeeId);

                int employeeId = 0;
                bool employeeExists = false;

                using (MySqlDataReader reader = employeeCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        employeeId = reader.GetInt32("Internal_Employee_Id");
                        employeeExists = true;
                    }
                }

                if (!employeeExists)
                {
                    MessageBox.Show("Invalid Employee_Id provided.");
                    return;
                }

                // Step 3: Insert into subject_load, repeat based on loadQuantity
                string insertQuery = "INSERT INTO subject_load (Internal_Employee_Id, Subject_Id, Subject_Code) VALUES (@employeeId, @subjectId, @subjectCode)";

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < loadQuantity; i++) // Loop to insert multiple times
                        {
                            MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn, transaction);
                            insertCmd.Parameters.AddWithValue("@employeeId", employeeId);
                            insertCmd.Parameters.AddWithValue("@subjectId", subjectId);
                            insertCmd.Parameters.AddWithValue("@subjectCode", subjectCode);

                            insertCmd.ExecuteNonQuery();
                        }

                        transaction.Commit(); // Commit the transaction if all inserts succeed
                        MessageBox.Show($"{loadQuantity} Subject Load(s) inserted successfully!");
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback the transaction if there's an error
                        MessageBox.Show("Error inserting Subject Load: " + ex.Message);
                    }
                }
            }
        }

        private void subject_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subject_grid.SelectedItem is DataRowView selectedRow)
            {
                selectedSubjectId = (int)selectedRow["Subject_Id"];
            }
        }
    }
}

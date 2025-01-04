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
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorSubjectRemove.xaml
    /// </summary>
    public partial class InstructorSubjectRemove : Window
    {
        public int EmployeeId { get; set; }

        int selectedSubjectId;
        string selectedSubjectCode;

        string connectionString = App.ConnectionString;

        public InstructorSubjectRemove(int employeeId)
        {
            InitializeComponent();
            EmployeeId = employeeId;
            LoadInstructorSubjects(employeeId);
        }

        private void LoadInstructorSubjects(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to retrieve subject ID, subject count, subject code, and subject title
                    string query = @"
                SELECT 
                    sl.Subject_Id,  -- Include Subject_Id for reference
                    sl.Subject_Code, 
                    s.Subject_Title,
                    COUNT(sl.Subject_Id) AS Subject_Load  -- Count occurrences of each Subject_Id
                FROM 
                    subject_load sl
                INNER JOIN 
                    subjects s ON sl.Subject_Id = s.Subject_Id
                WHERE 
                    sl.Internal_Employee_Id = @Employee_Id
                GROUP BY 
                    sl.Subject_Id, sl.Subject_Code, s.Subject_Title"; // Group by Subject_Id, Subject_Code, and Subject_Title

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Employee_Id", internalEmployeeId);

                        // Create a DataTable to hold the result set
                        DataTable subjectTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(subjectTable);
                        }

                        // Bind the DataTable to the DataGrid (instrutorSubject_data)
                        instrutorSubject_data.ItemsSource = subjectTable.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor subjects: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void Quantity_txtbx_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }


        private void instrutorSubject_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection changes in the DataGrid
            if (instrutorSubject_data.SelectedItem is DataRowView selectedRow)
            {
                selectedSubjectId = (int)selectedRow["Subject_Id"];
                selectedSubjectCode = selectedRow["Subject_Code"].ToString();
            }
        }

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            int employeeId_num = EmployeeId;  // Assuming this holds the current employee's ID
            if (instrutorSubject_data.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    string subjectCode = selectedRow["Subject_Code"].ToString();
                    int quantityToDelete = 0;  // Default value

                    // Retrieve the value from the quantity TextBox, if available
                    if (int.TryParse(subjectLoad_txt.Text, out int parsedQuantity))
                    {
                        quantityToDelete = parsedQuantity;
                    }

                    // Check if the quantity to delete is valid
                    if (quantityToDelete <= 0)
                    {
                        MessageBox.Show("Please enter a valid quantity greater than zero.");
                        return; // Exit the method if the quantity is not valid
                    }

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Query to delete rows for the selected subject code, excluding rows where status is "assigned"
                        string query = @"
                    DELETE FROM subject_load
                    WHERE Subject_Code = @subjectCode 
                    AND Internal_Employee_Id = @employeeId 
                    AND Status <> 'assigned'
                    LIMIT @quantityToDelete"; // Note: LIMIT may not work as expected in DELETE

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@subjectCode", subjectCode);
                            command.Parameters.AddWithValue("@employeeId", employeeId_num);
                            command.Parameters.AddWithValue("@quantityToDelete", quantityToDelete);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show($"{rowsAffected} subject(s) deleted from instructor successfully.");
                                LoadInstructorSubjects(employeeId_num);  // Refresh the subjects after deletion
                                subjectLoad_txt.Clear();
                                quantityToDelete = 0;
                            }
                            else
                            {
                                MessageBox.Show("No subjects were deleted (perhaps they are marked as 'assigned').");
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error deleting subject from instructor: " + ex.Message);
                }
                catch (Exception ex) // Catch any other unexpected exceptions
                {
                    MessageBox.Show("An unexpected error occurred: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select a subject to delete.");
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
    }
}

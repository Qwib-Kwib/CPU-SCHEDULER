using Microsoft.Win32;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cmp;
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
    /// Interaction logic for InstructorCSV.xaml
    /// </summary>
    public partial class InstructorCSV : Page
    {
        public int DepartmentId { get; set; }
        private string departmentCode;
        private string departmentName;

        string connectionString = App.ConnectionString;
        public InstructorCSV(int departmentId)
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            TopBarFrame.Navigate(topBar);
            topBar.txtPageTitle.Text = "Instructor Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;

            DepartmentId = departmentId;
            LoadBuildingDetails();
            LoadAllData();
        }
        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new InstructorMenu(DepartmentId));
        }

        private void LoadBuildingDetails() //change text block
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Code, Dept_Name FROM Departments WHERE Dept_Id = @departmentId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@departmentId", DepartmentId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            departmentCode = reader["Dept_Code"].ToString();
                            departmentName = reader["Dept_Name"].ToString();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building details: " + ex.Message);
            }

            DepartmentCode_txtblck.Text = departmentCode;
            DepartmentName_txtblck.Text = departmentName;
        }



        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InstructorMenu(DepartmentId));
        }

        private void Upload_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please ensure the CSV columns are: ID, Department Code, Last Name, Middle Name, First Name, Employment, Sex, Email, and Disability.");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadInstructorCsvFile(filePath);
                    if (dataTable != null)
                    {
                        Instructor_data.ItemsSource = dataTable.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private DataTable ReadInstructorCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns based on the expected CSV format
                    csvData.Columns.Add("Employee_Id", typeof(string));
                    csvData.Columns.Add("Department", typeof(string));
                    csvData.Columns.Add("LastName", typeof(string));
                    csvData.Columns.Add("MiddleName", typeof(string));
                    csvData.Columns.Add("FirstName", typeof(string));
                    csvData.Columns.Add("Employment", typeof(string));
                    csvData.Columns.Add("Sex", typeof(string));
                    csvData.Columns.Add("Email", typeof(string));
                    csvData.Columns.Add("Disability", typeof(bool));

                    // Read the header line first to skip it
                    sr.ReadLine();

                    // Read the data lines
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        // Ensure that the CSV row has the expected number of columns
                        if (rows.Length != 9)
                        {
                            MessageBox.Show("Error: CSV file format is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr["Employee_Id"] = rows[0].Trim();
                            dr["Department"] = rows[1].Trim();
                            dr["LastName"] = rows[2].Trim();
                            dr["MiddleName"] = rows[3].Trim();
                            dr["FirstName"] = rows[4].Trim();
                            dr["Employment"] = rows[5].Trim();
                            dr["Sex"] = rows[6].Trim();
                            dr["Email"] = rows[7].Trim();
                            dr["Disability"] = rows[8].Trim() == "1"; // Convert to bool (1 -> true, anything else -> false)

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



        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (Instructor_data.Items.Count == 0)
            {
                MessageBox.Show("No data to add.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get the DataTable from the DataGrid
            DataView dataView = (DataView)Instructor_data.ItemsSource;
            DataTable dataTable = dataView.Table;

            // Insert data into the database
            InsertInstructorDataIntoDatabase(dataTable);

            // Reload data after insertion
            LoadBuildingDetails();
            LoadAllData();
        }


        private void InsertInstructorDataIntoDatabase(DataTable dataTable)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Only insert rows that have not already been inserted
                        if (row.RowState == DataRowState.Added)
                        {
                            string query = @"
                        INSERT INTO instructor (Employee_Id, Dept_Id, Lname, Mname, Fname, Employment_Type, Employee_Sex, Email, disability)
                        VALUES (@Employee_Id, @Dept_Id, @Lname, @Mname, @Fname, @Employment_Type, @Employee_Sex, @Email, @Disability)";

                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                // Adjust parameters based on your DataTable column names
                                command.Parameters.AddWithValue("@Employee_Id", row["Employee_Id"]); // Assuming ID is auto-generated in the database
                                command.Parameters.AddWithValue("@Dept_Id", DepartmentId); // Assuming DepartmentId is correctly set
                                command.Parameters.AddWithValue("@Lname", row["LastName"]);
                                command.Parameters.AddWithValue("@Mname", row["MiddleName"]);
                                command.Parameters.AddWithValue("@Fname", row["FirstName"]);
                                command.Parameters.AddWithValue("@Employment_Type", row["Employment"]);
                                command.Parameters.AddWithValue("@Employee_Sex", row["Sex"]);
                                command.Parameters.AddWithValue("@Email", row["Email"]);
                                command.Parameters.AddWithValue("@Disability", row["Disability"]);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                MessageBox.Show("Data inserted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data into database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Remove_btn_Click(object sender, RoutedEventArgs e)
        {
            // Get the DataTable from the DataGrid
            DataTable dataTable = ((DataView)Instructor_data.ItemsSource).Table;

            // Create a list to store rows to be removed
            List<DataRow> rowsToRemove = new List<DataRow>();

            // Check selected items and collect rows to remove
            foreach (DataRowView selectedItem in Instructor_data.SelectedItems)
            {
                DataRow row = selectedItem.Row;
                // Check if ID column is empty
                if (string.IsNullOrEmpty(row["ID"].ToString()))
                {
                    rowsToRemove.Add(row);
                }
            }

            // Remove selected rows from the DataTable
            foreach (DataRow rowToRemove in rowsToRemove)
            {
                dataTable.Rows.Remove(rowToRemove);
            }

            // Refresh the DataGrid to reflect changes
            Instructor_data.ItemsSource = null;
            Instructor_data.ItemsSource = dataTable.DefaultView;

            MessageBox.Show("Selected rows with empty ID removed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void ViewData_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadBuildingDetails();
            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
            SELECT i.Employee_Id AS Employee_Id,
                   d.Dept_Code AS Department,
                   i.Lname AS LastName,
                   i.Mname AS MiddleName,
                   i.Fname AS FirstName,
                   i.Employment_Type AS Employment,
                   i.Employee_Sex AS Sex,
                   i.Email AS Email,
                   i.Disability AS Disability
            FROM instructor i
            JOIN departments d ON i.Dept_Id = d.Dept_Id
            WHERE i.Dept_Id = @deptId AND i.Status = 1;";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    Instructor_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }


        private void Status_btn_Click(object sender, RoutedEventArgs e)
        {
            if (Instructor_data.SelectedItems.Count == 0)
            {
                MessageBox.Show("No rows selected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Iterate through selected rows in the DataGrid
                    foreach (DataRowView rowView in Instructor_data.SelectedItems)
                    {
                        DataRow row = rowView.Row;
                        int employeeId = Convert.ToInt32(row["Employee_Id"]); // Assuming Employee_Id is an integer

                        // Toggle the status in the database (assuming Status is a numeric column)
                        string query = @"
                    UPDATE instructor
                    SET Status = CASE WHEN Status = 1 THEN 0 ELSE 1 END
                    WHERE Employee_Id = @Employee_Id";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@Employee_Id", employeeId);
                        command.ExecuteNonQuery();
                    }

                    LoadAllData();
                    MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh the data in the DataGrid after update
                    ViewData_btn_Click(sender, e); // Call your method to refresh data after update
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InstructorMenu(DepartmentId));
        }

        private void Instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

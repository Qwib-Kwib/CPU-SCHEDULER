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

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for InstructorSubjectLoad.xaml
    /// </summary>
    public partial class InstructorSubjectLoad : Page
    {
        public int DepartmentId { get; set; }

        public int EmployeeId;

        string connectionString = App.ConnectionString;
        public InstructorSubjectLoad(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;
            LoadDepartmentDetails();
            LoadInstructors();

            TopBar topBar = new TopBar();
            TopBarFrame.Navigate(topBar);
            topBar.txtPageTitle.Text = "Subject Load";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
        }


        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new InstructorMenuMain(DepartmentId));
        }

        private void LoadDepartmentDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Name FROM departments WHERE Dept_Id = @deptId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            collegeName_txt.Text = departmentName;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading department details: " + ex.Message);
            }
        }

        private void LoadInstructors()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // SQL query to select required fields for active instructors and total subject load
                    string query = @"
                SELECT 
                    i.Internal_Employee_Id, 
                    i.Employee_Id,
                    CONCAT(i.Fname, ' ', i.Mname, ' ', i.Lname) AS FullName,
                    COUNT(sl.Subject_Id) AS TotalLoad
                FROM 
                    instructor i
                LEFT JOIN 
                    subject_load sl ON i.Internal_Employee_Id = sl.Internal_Employee_Id
                WHERE 
                    i.Dept_Id = @deptId 
                    AND i.Status = 1
                GROUP BY 
                    i.Internal_Employee_Id, i.Employee_Id, i.Fname, i.Mname, i.Lname"; // Group by all selected fields

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Assuming instructor_data is a DataGrid or similar control
                    instructor_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }

        private void instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructor_data.SelectedItem != null)
            {
                DataRowView selectedRow = instructor_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    EmployeeId = (int)selectedRow["Internal_Employee_Id"];
                    LoadInstructorSubjects(EmployeeId);
                }
            }
        }

        private void LoadInstructorSubjects(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to retrieve subject count, subject code, and subject title, grouped by subject code and lecture lab
                    string query = @"
        SELECT sl.ID AS instructorSubject_Id,
               COUNT(sl.Subject_Id) AS subject_load, 
               sl.Subject_Code,
               s.Lecture_Lab AS Subject_LecLab,
               s.Subject_Title
        FROM subject_load sl
        INNER JOIN subjects s ON sl.Subject_Id = s.Subject_Id
        WHERE sl.Internal_Employee_Id = @Employee_Id
        GROUP BY sl.Subject_Code, s.Lecture_Lab, s.Subject_Title"; // Group by Subject_Code and Lecture_Lab

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

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeId == -1)
            {
                MessageBox.Show("Please select an Instructor ", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                InstructorSubjectAdd instructorSubjectAdd = new InstructorSubjectAdd(EmployeeId, DepartmentId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                instructorSubjectAdd.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                LoadInstructors();
                LoadInstructorSubjects(EmployeeId);
            }
        }

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeId == -1)
            {
                MessageBox.Show("Please select an Instructor ", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                InstructorSubjectRemove instructorSubjectRemove = new InstructorSubjectRemove(EmployeeId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                instructorSubjectRemove.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                LoadInstructors();
                LoadInstructorSubjects(EmployeeId);
            }

        }
    }
}

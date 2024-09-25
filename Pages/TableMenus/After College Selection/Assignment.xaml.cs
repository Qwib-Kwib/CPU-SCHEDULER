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

namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for Assignment.xaml
    /// </summary>
    /// 
    public partial class Assignment : Page
    {

        public int DepartmentId { get; set; }
        public int EmployeeId { get; set; } 

        string connectionString = App.ConnectionString;

        public Assignment()
        {
            InitializeComponent();
            LoadUI();
        }

        //UI
        #region UI 
        private void LoadUI()
        {
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Assignment Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
            LoadInstructors();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigateBack("Curriculum");
        }

        // Method to navigate back to the previous page
        private void NavigateBack(string sourceButton)
        {
            //CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            //NavigationService.Navigate(collegeSelection);
            NavigationService.Navigate(new MainMenu());
        }



        #endregion

        //Instructor
        #region Instructor

        private void LoadInstructors()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Updated query to show all instructors, without filtering by department
                    string query = @"
            SELECT i.Internal_Employee_Id, 
                   i.Employee_Id AS EmployeeId,
                   d.Dept_Code AS Department,
                   CONCAT(i.Lname, ', ', i.Fname, ' ', IFNULL(i.Mname, '')) AS FullName
            FROM instructor i
            JOIN departments d ON i.Dept_Id = d.Dept_Id
            WHERE i.Status = 1"; // Only filter by active status

                    MySqlCommand command = new MySqlCommand(query, connection);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the resulting data to the DataGrid
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
            
            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                EmployeeId = (int)selectedRow["Internal_Employee_Id"];

                LoadInstructorSubjects(EmployeeId);
            }
        }

        private void LoadInstructorSubjects(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT isub.Instructor_Subject_Id, s.Subject_Code, s.Subject_Title, isub.Quantity
                FROM instructor_subject isub
                JOIN subjects s ON isub.Subject_Id = s.Subject_Id
                WHERE isub.Internal_Employee_Id = @internalEmployeeId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@internalEmployeeId", internalEmployeeId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    instrutorSubject_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor subjects: " + ex.Message);
            }
        }



        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

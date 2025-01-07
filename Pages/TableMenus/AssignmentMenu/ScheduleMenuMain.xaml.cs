using Info_module.Pages.TableMenus.Assignment;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Info_module.Pages.TableMenus.AssignmentMenu
{
    /// <summary>
    /// Interaction logic for ScheduleMenuMain.xaml
    /// </summary>
    public partial class ScheduleMenuMain : Page
    {
        int BlockSectionId;

        string connectionString = App.ConnectionString;

        public ScheduleMenuMain()
        {
            InitializeComponent();

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Schedule Menu", TopBar_BackButtonClicked);

            pickBlockSection();
            loadScheduleByBlock(BlockSectionId);
            loadUnassinedSubject(BlockSectionId);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new AssignMenuMain());
        }


        private void loadScheduleByBlock(int section)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to load data from the class table
                    string query = @"
                SELECT c.Class_Id,
                       c.Subject_Id,
                       c.Internal_Employee_Id,
                       c.Room_Id,
                       c.Stub_Code,
                       s.Lecture_Lab as Lec_Lab,
                       c.Class_Day,
                       c.Start_Time,
                       c.End_Time,
                       s.Subject_Code AS SubjectCode,
                       CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
                       r.Room_Code AS RoomCode
                FROM subjects s
                LEFT JOIN class c ON c.Subject_Id = s.Subject_Id
                LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
                LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
                Where c.Block_Section_Id = '" + section + "'";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Bind the resulting data to the DataGrid
                    schedule_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading class details: " + ex.Message);
            }
        }


        private void loadUnassinedSubject(int section)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with "waiting" status for the specified block section
                    string query = @"
                SELECT 
                    s.Subject_Code,
                    s.Subject_Type,
                    s.Lecture_Lab,
                    s.Hours,
                    s.Units
                FROM 
                    block_subject_list bsl
                JOIN 
                    subjects s ON bsl.subjectId = s.Subject_Id
                WHERE 
                    bsl.blockSectionId = @blockSectionId AND 
                    bsl.status = 'waiting'";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add the blockSectionId parameter
                        command.Parameters.AddWithValue("@blockSectionId", section);

                        // Create a DataTable to hold the results
                        DataTable dataTable = new DataTable();

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            // Fill the DataTable with the results
                            adapter.Fill(dataTable);
                        }

                        // Bind the DataTable to a DataGrid or other UI component
                        // Assuming you have a DataGrid named 'UnassignedSubjectsGrid'
                        unAssingedSubjects_data.ItemsSource = dataTable.DefaultView; // Replace with your actual DataGrid name
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"MySQL Error ({ex.Number}): {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void pickBlock_btn_Click(object sender, RoutedEventArgs e)
        {
            pickBlockSection();
        }

        private void pickBlockSection()
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                ScheduleMenuBlockSection windowMenu = new ScheduleMenuBlockSection()
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };


                windowMenu.ShowDialog();

                // Retrieve the curriculum ID from the dialog
                BlockSectionId = windowMenu.SelectedBlockSectionId;
                BlockSection_txt.Text = windowMenu.SelectedBlockSectionName;
                
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                loadScheduleByBlock(BlockSectionId);
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                ScheduleMenuEdit windowMenu = new ScheduleMenuEdit(BlockSectionId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };


                windowMenu.ShowDialog();
                
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                loadScheduleByBlock(BlockSectionId);
            }

        }
    }
}

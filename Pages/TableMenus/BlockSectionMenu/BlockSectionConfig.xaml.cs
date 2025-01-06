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


namespace Info_module.Pages.TableMenus.BlockSectionMenu
{
    /// <summary>
    /// Interaction logic for BlockSectionConfig.xaml
    /// </summary>
    public partial class BlockSectionConfig : Page
    {
        string connectionString = App.ConnectionString;

        public int BlockSectionId;
        public string BlockSectionName;
        public int DepartmentId;
        public string Year;

        public BlockSectionConfig(int blockSectionId, string blockSectionName, int year, int departmentId)
        {
            InitializeComponent();
            BlockSectionId = blockSectionId;
            BlockSectionName = blockSectionName;
            DepartmentId = departmentId;
            Year = year.ToString();

            blockSectionName_txt.Text = blockSectionName;
            year_txt.Text = year.ToString();

            Load_BlockSection_Subjects(blockSectionId);

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Rooms Menu", TopBar_BackButtonClicked);
            
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new BlockSectionMenuMain(DepartmentId));
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();

        }

        private void Load_BlockSection_Subjects(int blockSectionId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get all subject details for a specific block section
                    string query = @"
                SELECT 
                    s.Subject_Id,
                    s.Subject_Code,
                    d.Dept_Name AS Serving_Department, 
                    s.Subject_Title,
                    s.Subject_Type,
                    s.Lecture_Lab,
                    s.Hours AS Hours,
                    s.Units AS Units
                FROM 
                    block_subject_list bsl
                INNER JOIN 
                    subjects s ON bsl.subjectId = s.Subject_Id
                INNER JOIN 
                    departments d ON s.Dept_Id = d.Dept_Id
                WHERE 
                    bsl.blockSectionId = @blockSectionId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                    // Create and fill DataTable
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Bind the DataTable to the DataGrid (Block_Subject_List_data)
                    Block_Subject_List_data.ItemsSource = dt.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects for the block section: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void removeSubject_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if there is a selected item in the DataGrid
            if (Block_Subject_List_data.SelectedItem is DataRowView selectedRow)
            {
                // Get the Subject_Id from the selected row
                int subjectId = Convert.ToInt32(selectedRow["Subject_Id"]);

                try
                {
                    // Check if a block section and subject are selected
                    int blockSectionId = BlockSectionId; // Replace with logic to get selected block section ID

                    if (blockSectionId == 0 || subjectId == 0)
                    {
                        MessageBox.Show("Please select both a block section and a subject to remove.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Remove the subject from the block_subject_list
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = @"
                DELETE FROM block_subject_list
                WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId";

                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                        cmd.Parameters.AddWithValue("@subjectId", subjectId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Subject successfully removed from the block section.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to remove subject from the block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    // Optional: Refresh the DataGrid or update UI after removing the subject
                    Load_BlockSection_Subjects(BlockSectionId); // Example function to refresh the block section list
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a subject to remove.", "No Subject Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


        }


        private void addSubject_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                BlockSectionSubjecAdd windowMenu = new BlockSectionSubjecAdd(BlockSectionId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                windowMenu.ShowDialog();

                // Check if the dialog was closed with a result

                // Retrieve the subject details from the dialog
                int subjectId = windowMenu.SubjectId;
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                Load_BlockSection_Subjects(BlockSectionId);
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new BlockSectionMenuMain(DepartmentId));
        }


        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the blockSectionId and blockSectionName from your UI (e.g., TextBoxes)
            int blockSectionId = BlockSectionId; // Ensure this is set correctly
            string blockSectionName = blockSectionName_txt.Text; // Assuming blockSectionName_txt is your TextBox for name
            string Year = year_txt.Text; // Ensure this is set correctly

            // Debugging output
            Console.WriteLine($"Updating Block Section ID: {blockSectionId}, Name: {blockSectionName}, Year: {Year}");

            try
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to update blockSectionName using blockSectionId
                    string query = "UPDATE block_section SET blockSectionName = @blockSectionName, School_Year = @year WHERE blockSectionId = @blockSectionId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Add parameters to prevent SQL injection
                        command.Parameters.AddWithValue("@blockSectionName", blockSectionName);
                        command.Parameters.AddWithValue("@year", Year);
                        command.Parameters.AddWithValue("@blockSectionId", blockSectionId);

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Block section updated successfully!");
                        }
                        else
                        {
                            MessageBox.Show("No record found with the given Block Section ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void year_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }
    }
}

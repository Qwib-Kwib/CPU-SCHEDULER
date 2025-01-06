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

namespace Info_module.Pages.TableMenus.BlockSectionMenu
{
    /// <summary>
    /// Interaction logic for BlockSectionSubjecAdd.xaml
    /// </summary>
    public partial class BlockSectionSubjecAdd : Window
    {
        string connectionString = App.ConnectionString;

        public int SubjectId;
        public int BlockId;

        public BlockSectionSubjecAdd(int blockId)
        {
            InitializeComponent();
            LoadSubject_Grid();
            BlockId = blockId;
        }

        private void LoadSubject_Grid()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to load subjects with CurriculumId and active status
                    string query = @"
                        SELECT 
                            s.Subject_Id AS Subject_Id, 
                            s.Subject_Code AS Subject_Code,
                            d.Dept_Code AS Subject_Department,
                            s.Subject_Title AS Subject_Title,
                            s.Subject_Type AS Subject_Type,
                            s.Lecture_Lab AS LEC_LAB,
                            s.Hours AS Hours,
                            s.Units AS Units
                        FROM 
                            subjects s 
                        JOIN 
                            departments d ON s.Dept_Id = d.Dept_Id
                        WHERE s.Status = 1";
                    // Add ordering to make results consistent
                    query += " ORDER BY s.Subject_Code";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    // Create and fill DataTable
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Assuming you're using a DataGrid to display the subject list
                    subject_grid.ItemsSource = dt.DefaultView; // Bind to DataGrid (or other UI component)
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void subject_grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (subject_grid.SelectedItem is DataRowView selectedRow)
            {
                SubjectId = Convert.ToInt32(selectedRow["Subject_Id"]);
                try
                {
                    // Check if a block section and subject are selected
                    int blockSectionId = BlockId; // Replace with logic to get selected block section ID
                    int subjectId = SubjectId; // Replace with logic to get selected subject ID

                    if (blockSectionId == 0 || subjectId == 0)
                    {
                        MessageBox.Show("Please select both a block section and a subject.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Check if the subject is already assigned to the block section
                    if (IsSubjectAssignedToBlock(blockSectionId, subjectId))
                    {
                        MessageBox.Show("This subject is already assigned to the selected block section.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Insert new subject into block_subject_list
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = @"
                INSERT INTO block_subject_list (blockSectionId, subjectId, status)
                VALUES (@blockSectionId, @subjectId, 'assigned')";

                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                        cmd.Parameters.AddWithValue("@subjectId", subjectId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Subject successfully added to the block section.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to add subject to the block section.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

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
                MessageBox.Show("Please select a subject to add.", "No Subject Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.Close();
        }


        private bool IsSubjectAssignedToBlock(int blockSectionId, int subjectId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM block_subject_list WHERE blockSectionId = @blockSectionId AND subjectId = @subjectId";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@blockSectionId", blockSectionId);
                cmd.Parameters.AddWithValue("@subjectId", subjectId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
    }


}

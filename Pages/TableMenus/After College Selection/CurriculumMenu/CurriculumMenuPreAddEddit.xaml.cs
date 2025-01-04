using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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

namespace Info_module.Pages.TableMenus.After_College_Selection.CurriculumMenu
{
    /// <summary>
    /// Interaction logic for CurriculumMenuPreAddEddit.xaml
    /// </summary>
    public partial class CurriculumMenuPreAddEddit : Window
    {

        string connectionString = App.ConnectionString;

        public int CurriculumId { get; set; }
        public int DepartmentId { get; set; }

        public CurriculumMenuPreAddEddit(int departmentId,int curriculumId, bool isEditClick)
        {
            InitializeComponent();
            CurriculumId = curriculumId;
            DepartmentId = departmentId;
            EditORAddMode(isEditClick);
        }

        public void EditORAddMode(bool isEditClick)
        {
            if (!isEditClick)
            {
                curriculumId_grid.Visibility = Visibility.Hidden;
                add_btn.Visibility = Visibility.Visible;
                edit_btn.Visibility = Visibility.Hidden;
                this.Title = "Curriculum Add";
            }
            else 
            {
                this.Title = "Curriculum Edit";
                LoadForms(CurriculumId);
            }
        }

        //Edit
        #region
        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            int curriculumId;
            if (!int.TryParse(curriculumId_txt.Text, out curriculumId))
            {
                MessageBox.Show("Please enter a valid Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(curriculumRevision_txt.Text) ||
                    string.IsNullOrWhiteSpace(curriculumDescription_txt.Text) ||
                    !int.TryParse(yearEffectiveIn_txt.Text, out int yearEffectiveIn) ||
                    !int.TryParse(yearEffectiveOut_txt.Text, out int yearEffectiveOut))
                {
                    MessageBox.Show("Please fill in all fields with valid data.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"UPDATE curriculum 
                             SET Dept_Id = @Course_Id, 
                                 Curriculum_Revision = @Curriculum_Revision, 
                                 Curriculum_Description = @Curriculum_Description, 
                                 Year_Effective_In = @Year_Effective_In, 
                                 Year_Effective_Out = @Year_Effective_Out 
                             WHERE Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Course_Id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut);
                        command.Parameters.AddWithValue("@Curriculum_Id", curriculumId);

                        int rowsAffected = command.ExecuteNonQuery(); // Check how many rows were affected

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Curriculum Updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("No curriculum found with the specified ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) // Catch any other exceptions
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadForms(int curriculumId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to select the curriculum with the specified ID
                    string query = @"
                SELECT 
                    Curriculum_Revision,
                    Curriculum_Description,
                    Year_Effective_In,
                    Year_Effective_Out
                FROM curriculum
                WHERE Curriculum_Id = @curriculumId"; // Filter by Curriculum_Id

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@curriculumId", curriculumId); // Add parameter

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Read the first row
                        {
                            // Populate the text boxes with the data
                            curriculumId_txt.Text = curriculumId.ToString(); // Use the parameter passed to the method
                            curriculumRevision_txt.Text = reader["Curriculum_Revision"]?.ToString() ?? "N/A"; // Handle potential null
                            curriculumDescription_txt.Text = reader["Curriculum_Description"]?.ToString() ?? "N/A"; // Handle potential null
                            yearEffectiveIn_txt.Text = reader["Year_Effective_In"]?.ToString() ?? "N/A"; // Handle potential null
                            yearEffectiveOut_txt.Text = reader["Year_Effective_Out"]?.ToString() ?? "N/A"; // Handle potential null
                        }
                        else
                        {
                            MessageBox.Show("No Curriculum found with the specified ID.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) // Catch any other exceptions
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Add
        #region
        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(curriculumRevision_txt.Text) ||
                    string.IsNullOrWhiteSpace(curriculumDescription_txt.Text) ||
                    !int.TryParse(yearEffectiveIn_txt.Text, out int yearEffectiveIn) ||
                    !int.TryParse(yearEffectiveOut_txt.Text, out int yearEffectiveOut))
                {
                    MessageBox.Show("Please fill in all fields with valid data.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    curriculumId_txt.Text = "";
                    connection.Open();

                    // Insert new curriculum and retrieve the Curriculum_Id
                    string insertCurriculumQuery = @"
                INSERT INTO curriculum (Dept_id, Curriculum_Revision, Curriculum_Description, Year_Effective_In, Year_Effective_Out)
                VALUES (@Dept_id, @Curriculum_Revision, @Curriculum_Description, @Year_Effective_In, @Year_Effective_Out);
                SELECT LAST_INSERT_ID();";

                    int newCurriculumId = 0;

                    using (MySqlCommand command = new MySqlCommand(insertCurriculumQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Dept_id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut_txt.Text);

                        // Execute and retrieve the new Curriculum_Id
                        newCurriculumId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Check if Curriculum_Id was retrieved successfully
                    if (newCurriculumId > 0)
                    {
                        // Insert semesters for 1st, 2nd, Summer semesters for each year (up to 4th year)
                        string insertSemesterQuery = @"
                    INSERT INTO semester (curriculum_id, year_level, semester) 
                    VALUES (@Curriculum_Id, @Year_Level, @Semester)";

                        using (MySqlCommand semesterCommand = new MySqlCommand(insertSemesterQuery, connection))
                        {
                            // Loop through each year (1 to 4) and each semester for the year
                            for (int year = 1; year <= 4; year++)
                            {
                                foreach (string sem in new string[] { "1", "2", "Summer" })
                                {
                                    semesterCommand.Parameters.Clear();
                                    semesterCommand.Parameters.AddWithValue("@Curriculum_Id", newCurriculumId);
                                    semesterCommand.Parameters.AddWithValue("@Year_Level", year);
                                    semesterCommand.Parameters.AddWithValue("@Semester", sem);
                                    semesterCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

                MessageBox.Show("Curriculum and its semesters were successfully added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion



        #endregion

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}

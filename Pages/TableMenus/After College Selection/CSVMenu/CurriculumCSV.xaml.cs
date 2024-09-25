using CsvHelper;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using CsvHelper.Configuration;

using static Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu;

namespace Info_module.Pages.TableMenus.After_College_Selection.CSVMenu
{
    /// <summary>
    /// Interaction logic for CurriculumCSV.xaml
    /// </summary>
    public partial class CurriculumCSV : Page
    {
        public int DepartmentId { get; set; }
        public int CurriculumId { get; set; }

        string connectionString = App.ConnectionString;


        public CurriculumCSV(int departmentId, int curriculumId)
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Curriculum Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);

            DepartmentId = departmentId;
            CurriculumId = curriculumId;
            LoadCurriculumDetails();
            LoadSubject();
        }

        private void LoadCurriculumDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to fetch Curriculum_Revision from curriculum table
                    string curriculumQuery = @"
                SELECT Curriculum_Revision 
                FROM curriculum 
                WHERE Curriculum_Id = @curriculumId";

                    MySqlCommand curriculumCommand = new MySqlCommand(curriculumQuery, connection);
                    curriculumCommand.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    // Query to fetch Logo_Image from departments table
                    string departmentQuery = @"
                SELECT Logo_Image 
                FROM departments 
                WHERE Dept_Id = @deptId";

                    MySqlCommand departmentCommand = new MySqlCommand(departmentQuery, connection);
                    departmentCommand.Parameters.AddWithValue("@deptId", DepartmentId);

                    // Execute the first query to get Curriculum_Revision
                    string curriculumRevision = null;
                    using (MySqlDataReader curriculumReader = curriculumCommand.ExecuteReader())
                    {
                        if (curriculumReader.Read())
                        {
                            curriculumRevision = curriculumReader["Curriculum_Revision"].ToString();
                        }
                    }

                    // Set the curriculum revision text box
                    curriculumRevision_txt.Text = curriculumRevision;

                    // Execute the second query to get Logo_Image
                    byte[] logoImageData = null;
                    using (MySqlDataReader departmentReader = departmentCommand.ExecuteReader())
                    {
                        if (departmentReader.Read())
                        {
                            // Load logo image
                            if (!(departmentReader["Logo_Image"] is DBNull))
                            {
                                logoImageData = (byte[])departmentReader["Logo_Image"];
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = new MemoryStream(logoImageData);
                                bitmap.EndInit();
                                Logo_img.Source = bitmap;
                            }
                            else
                            {
                                // Handle case where no image is found
                                Logo_img.Source = null;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        private void LoadSubject()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    s.Subject_Id AS Subject_Id,
                    d.Dept_Code AS Serving_Department,
                    s.Year_Level AS Year_Level,
                    s.Semester AS Semester,
                    s.Subject_Code AS Subject_Code,
                    s.Subject_Title AS Subject_Title,
                    s.Subject_Type AS Subject_Type,
                    s.Lecture_Lab AS Lecture_Lab,
                    s.Hours AS Hours,
                    s.Units AS Units
                FROM subjects s
                JOIN departments d ON s.Dept_Id = d.Dept_Id
                JOIN curriculum_subjects cs ON s.Subject_Id = cs.Subject_Id
                WHERE cs.Curriculum_Id = @curriculumId";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@curriculumId", CurriculumId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    // Bind the DataTable directly to the DataGrid
                    subject_data.ItemsSource = dt.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void NavigateBack(string sourceButton)
        {
            NavigationService.Navigate(new CurriculumMenu(DepartmentId));
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigateBack("Curriculum");
        }

        private void Upload_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Please ensure the CSV columns are: Subject Id, Serving Department, Year Level, Semester, Subject Code, Subject Title, Subject Type, Lecture Lab, Hours, and Units.");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadCsvFile(filePath);
                    if (dataTable != null)
                    {
                        subject_data.ItemsSource = dataTable.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to read CSV file into DataTable
        private DataTable ReadCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns based on the expected CSV format
                    csvData.Columns.Add("Subject_Id", typeof(int)); // Assuming Subject Id is an integer
                    csvData.Columns.Add("Serving_Department", typeof(string));
                    csvData.Columns.Add("Year_Level", typeof(string));
                    csvData.Columns.Add("Semester", typeof(string));
                    csvData.Columns.Add("Subject_Code", typeof(string));
                    csvData.Columns.Add("Subject_Title", typeof(string));
                    csvData.Columns.Add("Subject_Type", typeof(string));
                    csvData.Columns.Add("Lecture_Lab", typeof(string));
                    csvData.Columns.Add("Hours", typeof(string));
                    csvData.Columns.Add("Units", typeof(string));

                    // Read the header line first to skip it
                    sr.ReadLine();

                    // Read the data lines
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        // Ensure that the CSV row has the expected number of columns
                        if (rows.Length != 10)
                        {
                            MessageBox.Show("Error: CSV file format is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr["Subject_Id"] = string.IsNullOrEmpty(rows[0].Trim()) ? -1 : Convert.ToInt32(rows[0].Trim()); // Use -1 if ID is empty
                            dr["Serving_Department"] = rows[1].Trim();
                            dr["Year_Level"] = rows[2].Trim();
                            dr["Semester"] = rows[3].Trim();
                            dr["Subject_Code"] = rows[4].Trim();
                            dr["Subject_Title"] = rows[5].Trim();
                            dr["Subject_Type"] = rows[6].Trim();
                            dr["Lecture_Lab"] = rows[7].Trim();
                            dr["Hours"] = rows[8].Trim();
                            dr["Units"] = rows[9].Trim();

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


        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CurriculumMenu(DepartmentId));
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    int curriculumId = Convert.ToInt32(CurriculumId);

                    // Delete existing curriculum-subject bindings for the current Curriculum_Id
                    string deleteQuery = "DELETE FROM curriculum_subjects WHERE curriculum_id = @Curriculum_Id;";
                    MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                    deleteCommand.ExecuteNonQuery();

                    foreach (DataRowView item in subject_data.Items)
                    {
                        DataRow row = item.Row;

                        string deptCode = row["Serving_Department"].ToString(); // Assuming this is the Dept_Code in your DataGrid

                        // Query to retrieve Dept_Id based on Dept_Code
                        string deptIdQuery = "SELECT Dept_Id FROM departments WHERE Dept_Code = @Dept_Code;";
                        MySqlCommand deptIdCmd = new MySqlCommand(deptIdQuery, connection);
                        deptIdCmd.Parameters.AddWithValue("@Dept_Code", deptCode);

                        object deptIdObj = deptIdCmd.ExecuteScalar();
                        if (deptIdObj == null || deptIdObj == DBNull.Value)
                        {
                            MessageBox.Show($"Department with Dept_Code '{deptCode}' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        int deptId = Convert.ToInt32(deptIdObj);

                        // Check if the Subject_Code already exists in the subjects table
                        string subjectCode = row["Subject_Code"].ToString();
                        string subjectCheckQuery = "SELECT Subject_Id FROM subjects WHERE Subject_Code = @Subject_Code AND Dept_Id = @Dept_Id;";
                        MySqlCommand subjectCheckCmd = new MySqlCommand(subjectCheckQuery, connection);
                        subjectCheckCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                        subjectCheckCmd.Parameters.AddWithValue("@Dept_Id", deptId);

                        object subjectIdObj = subjectCheckCmd.ExecuteScalar();
                        int subjectId;

                        if (subjectIdObj == null || subjectIdObj == DBNull.Value)
                        {
                            // If the subject doesn't exist, insert it into the subjects table
                            string insertSubjectQuery = @"
                        INSERT INTO subjects (Dept_Id, Year_Level, Semester, Subject_Code, Subject_Title, Subject_Type, Lecture_Lab, Hours, Units)
                        VALUES (@Dept_Id, @Year_Level, @Semester, @Subject_Code, @Subject_Title, @Subject_Type, @Lecture_Lab, @Lec_Hours, @Credit_Units);
                        SELECT LAST_INSERT_ID();";

                            MySqlCommand insertSubjectCmd = new MySqlCommand(insertSubjectQuery, connection);
                            insertSubjectCmd.Parameters.AddWithValue("@Dept_Id", deptId);
                            insertSubjectCmd.Parameters.AddWithValue("@Year_Level", Convert.ToInt32(row["Year_Level"]));
                            insertSubjectCmd.Parameters.AddWithValue("@Semester", row["Semester"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Code", subjectCode);
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Title", row["Subject_Title"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Subject_Type", row["Subject_Type"].ToString());
                            insertSubjectCmd.Parameters.AddWithValue("@Lecture_Lab", row["Lecture_Lab"].ToString());

                            // Validate and convert Lec Hours
                            if (!int.TryParse(row["Hours"].ToString(), out int lecHours))
                            {
                                MessageBox.Show("Invalid value for Lec Hours: " + row["Hours"].ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            insertSubjectCmd.Parameters.AddWithValue("@Lec_Hours", lecHours);

                            // Validate and convert Credit Units
                            if (!int.TryParse(row["Units"].ToString(), out int creditUnits))
                            {
                                MessageBox.Show("Invalid value for Credit Units: " + row["Units"].ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            insertSubjectCmd.Parameters.AddWithValue("@Credit_Units", creditUnits);

                            subjectId = Convert.ToInt32(insertSubjectCmd.ExecuteScalar());
                        }
                        else
                        {
                            // If the subject exists, use the existing Subject_Id
                            subjectId = Convert.ToInt32(subjectIdObj);
                        }

                        // Insert into curriculum_subjects to create the binding between subject and curriculum
                        string insertCurriculumSubjectQuery = @"
                    INSERT INTO curriculum_subjects (curriculum_id, subject_id)
                    VALUES (@Curriculum_Id, @Subject_Id);";

                        MySqlCommand insertCurriculumSubjectCmd = new MySqlCommand(insertCurriculumSubjectQuery, connection);
                        insertCurriculumSubjectCmd.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                        insertCurriculumSubjectCmd.Parameters.AddWithValue("@Subject_Id", subjectId);

                        insertCurriculumSubjectCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Data inserted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSubject();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data into database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



    }
}

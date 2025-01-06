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
    /// Interaction logic for BlockSectionMenuSemester.xaml
    /// </summary>
    public partial class BlockSectionMenuSemester : Window
    {
        public int CurriculumId;
        public string CurriculumDescription;
        public string YearLevel;
        public string Semester;

        string connectionString = App.ConnectionString;

        public BlockSectionMenuSemester(int curriculumId, string yearLevel, string semester, string curriculumDescription)
        {
            InitializeComponent();

            CurriculumId = curriculumId;
            YearLevel = yearLevel;
            Semester = semester;
            CurriculumDescription = curriculumDescription;

            SemesterInfo_txt.Text = $"{YearLevel}, {ConvertSemester(Semester)}, {CurriculumDescription}";

            LoadSemester();
        }
        private string ConvertSemester(string semester)
        {
            switch (semester)
            {
                case "1":
                    return "1st Semester";
                case "2":
                    return "2nd Semester";
                case "Summer":
                    return "Summer";
                default:
                    return semester; // Return the original value if it doesn't match
            }
        }

        private void close_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadSemester()
        {
            int yearLevel = int.Parse(YearLevel);
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                    SELECT
                        sub.Subject_Id AS 'Subject_Id',
                        d.Dept_Code AS 'Serving_Department',
                        sub.Subject_Code AS `Subject_Code`,
                        sub.Subject_Title AS `Subject_Title`,
                        sub.Subject_Type AS `Subject_Type`,
                        sub.Lecture_Lab AS `Lecture_Lab`,
                        sub.Hours AS `Hours`,
                        sub.Units AS `Units`
                    FROM semester s
                    LEFT JOIN semester_subject_list sl ON s.semester_id = sl.semester_id
                    LEFT JOIN subjects sub ON sl.subject_id = sub.Subject_Id
                    LEFT JOIN curriculum c ON s.curriculum_id = c.Curriculum_Id 
                    LEFT JOIN departments d ON sub.Dept_Id = d.Dept_Id
                    WHERE s.curriculum_id = @curriculumId
                    AND s.year_level = @yearLevel
                    AND s.semester = @semester";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@curriculumId", CurriculumId);
                    command.Parameters.AddWithValue("@yearLevel", yearLevel);
                    command.Parameters.AddWithValue("@semester", Semester);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Assuming you have a DataGrid named 'curriculumSubjects_data'
                    semester_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum subjects: " + ex.Message);
            }
        }
    }
}

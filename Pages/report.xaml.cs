using Info_module.ViewModels;
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
using Word = Microsoft.Office.Interop.Word;

namespace Info_module.Pages
{
    /// <summary>
    /// Interaction logic for report.xaml
    /// </summary>
    public partial class report : Page
    {

        string connectionString = App.ConnectionString;

        public report()
        {
            InitializeComponent();
            LoadUI();
            
        }
        #region UI 

        private void LoadUI()
        {
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Assignment Menu", TopBar_BackButtonClicked);
            LoadDepartmentitems();
            LoadCurriculum();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new MainMenu());
        }
        public class Department
        {
            public int DepartmentIds { get; set; }
            public string DepartmentCodes { get; set; }

        }

        private void LoadDepartmentitems()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Id, Dept_Code FROM departments";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    List<Department> departments = new List<Department>();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departments.Add(new Department
                            {
                                DepartmentIds = reader.GetInt32("Dept_Id"),
                                DepartmentCodes = reader.GetString("Dept_Code")
                            });
                        }
                    }

                    // Add "All" option at the top
                    departments.Insert(0, new Department { DepartmentIds = -1, DepartmentCodes = "All" });

                    collegiate_cmbx.ItemsSource = departments;

                    collegiate_cmbx.DisplayMemberPath = "DepartmentCodes";
                    collegiate_cmbx.SelectedValuePath = "DepartmentIds";

                    // Set the default selected item as "All"
                    collegiate_cmbx.SelectedIndex = 0; // Selects "All"
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading Departments: " + ex.Message);
            }
        }

        private DataTable curriculumDataTable;

        // Method to load the curriculum with filtering by Department and Status
        private void LoadCurriculum(int deptId = -1, int status = 1)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to get curriculum details and active block section count
                    string query = @"
SELECT 
    c.Curriculum_Id AS Curriculum_Id, 
    c.Curriculum_Revision AS Curriculum_Revision,
    c.Curriculum_Description AS Curriculum_Description,
    d.Dept_Code AS Department,
    CONCAT(c.Year_Effective_In, '-', c.Year_Effective_Out) AS Year_Effective,
    COUNT(bs.blockSectionId) AS ActiveBlockSectionCount
FROM curriculum c 
JOIN departments d ON c.Dept_Id = d.Dept_Id
LEFT JOIN block_section bs ON bs.curriculumId = c.Curriculum_Id AND bs.Status = 1
WHERE c.Status = @Status"; // Filter by status using a parameter

                    // Filter by Department if a Dept_Id is provided
                    if (deptId != -1)
                    {
                        query += " AND c.Dept_Id = @Dept_Id";
                    }

                    // Group the results to count active block sections and filter only curricula with block sections
                    query += " GROUP BY c.Curriculum_Id HAVING COUNT(bs.blockSectionId) > 0";

                    MySqlCommand command = new MySqlCommand(query, connection);

                    // Set status parameter
                    command.Parameters.AddWithValue("@Status", status);

                    // Set department filter parameter if applicable
                    if (deptId != -1)
                    {
                        command.Parameters.AddWithValue("@Dept_Id", deptId);
                    }

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    curriculumDataTable = new DataTable();
                    dataAdapter.Fill(curriculumDataTable); // Fill DataTable

                    // Bind the data to the DataGrid
                    curriculum_data.ItemsSource = curriculumDataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }



        int CurriculumId = 0;
        string CurriculumRevision;
        string CurriculumDescription;

        // Handle selection change event in DataGrid
        private void curriculumDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculum_data.SelectedItem is DataRowView selectedRow)
            {
                if (selectedRow != null && selectedRow["Curriculum_Id"] != DBNull.Value)
                {
                    CurriculumId = Convert.ToInt32(selectedRow["Curriculum_Id"]);
                    CurriculumRevision = selectedRow["Curriculum_Revision"].ToString();
                    CurriculumDescription = selectedRow["Curriculum_Description"].ToString();
                    curriculumId_txt.Text = CurriculumId.ToString();
                    curriculumRevision_txt.Text = CurriculumRevision;
                }
            }
            else
            {
                // Clear text if no row is selected
                curriculumId_txt.Text = string.Empty;
            }
        }

        private void collegiate_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only filter if the ComboBox has a valid selection
            if (collegiate_cmbx.SelectedValue != null)
            {
                // Get the selected department ID
                int selectedDeptId = (int)collegiate_cmbx.SelectedValue;

                // Find the department code corresponding to the selected ID
                string selectedDeptCode = null;

                if (collegiate_cmbx.SelectedItem is Department selectedDepartment)
                {
                    selectedDeptCode = selectedDepartment.DepartmentCodes; // Get the department code
                }

                // Use DataView to filter the DataTable
                if (curriculumDataTable != null)
                {
                    DataView view = new DataView(curriculumDataTable);

                    if (selectedDeptCode != "All") // If the selected value is not "All"
                    {
                        view.RowFilter = $"Department = '{selectedDeptCode}'"; // Filter by department code
                    }
                    else
                    {
                        view.RowFilter = string.Empty; // Show all if "All" is selected
                    }

                    curriculum_data.ItemsSource = view; // Bind the filtered view to the DataGrid
                }
            }
            else
            {
                // If nothing is selected, reset the DataGrid to show all data
                curriculum_data.ItemsSource = curriculumDataTable.DefaultView;
            }
        }






        #endregion

        #region Word

        private void genereateReport_btn_Click(object sender, RoutedEventArgs e)
        {
            if (CurriculumId == 0)
            {
                MessageBox.Show("Select curriculum first");
                return;
            }

            if(CurriculumRevision == null)
            {
                MessageBox.Show("Curriculum revision missing");
                return;
            }

            if (CurriculumDescription == null)
            {
                MessageBox.Show("Curriculum Description missing");
                return;
            }

            try
            {
                // Step 1: Prompt the user to select the file path
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Report",
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = "Curriculum_Report.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Step 2: Create a new Word application and document
                    Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
                    Word.Document wordDoc = wordApp.Documents.Add();

                    // Step 3: Add sample content to the Word document
                    //// Add Subject Revision and Description as header 1 and header 2
                    AddHeadersToDocument(wordDoc, CurriculumRevision, CurriculumDescription);

                    // Add a new paragraph for spacing
                    Word.Paragraph spacingPara = wordDoc.Content.Paragraphs.Add();
                    spacingPara.Range.InsertParagraphAfter();

                    //// Add Body
                    AddAllYearAndSemester(wordDoc);

                    // Step 4: Save the document to the specified file path
                    wordDoc.SaveAs2(filePath);
                    wordDoc.Close();
                    wordApp.Quit();

                    MessageBox.Show($"Report successfully created at:\n{filePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void AddHeadersToDocument(Word.Document wordDoc, string header1, string header2)
        {
            // Add Header 1
            Word.Paragraph header1Para = wordDoc.Content.Paragraphs.Add();
            header1Para.Range.Text = header1;
            header1Para.Range.Font.Size = 18; // Set font size for Header 1
            header1Para.Range.Font.Bold = 1; // Set bold for Header 1
            header1Para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            header1Para.Range.InsertParagraphAfter();

            // Add Header 2
            Word.Paragraph header2Para = wordDoc.Content.Paragraphs.Add();
            header2Para.Range.Text = header2;
            header2Para.Range.Font.Size = 16; // Set font size for Header 2
            header2Para.Range.Font.Bold = 1; // Set bold for Header 2
            header2Para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            header2Para.Range.InsertParagraphAfter();
        }
        
        //Labels
        private void AddYearAndSemester(Word.Document wordDoc, string text)
        {
            // Add left-aligned text
            Word.Paragraph leftAlignedPara = wordDoc.Content.Paragraphs.Add();
            leftAlignedPara.Range.Text = text;
            leftAlignedPara.Range.Font.Size = 14; // Set font size for left-aligned text
            leftAlignedPara.Range.Font.Bold = 0; // Set normal font for left-aligned text
            leftAlignedPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft; // Align left
            leftAlignedPara.Range.InsertParagraphAfter();
        }

        private void AddBlockSectionName(Word.Document wordDoc, string text)
        {
            // Add left-aligned text
            Word.Paragraph leftAlignedPara = wordDoc.Content.Paragraphs.Add();
            leftAlignedPara.Range.Text = text;
            leftAlignedPara.Range.Font.Size = 14; // Set font size for left-aligned text
            leftAlignedPara.Range.Font.Bold = 0; // Set normal font for left-aligned text
            leftAlignedPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Align left
            leftAlignedPara.Range.InsertParagraphAfter();
        }


        private void AddAllYearAndSemester(Word.Document wordDoc)
        {
            for (int year = 1; year <= 4; year++)
            {
                int currentYear = year;
                string currentSemester;
                List<int> blockSectionIds;

                // Add 1st Semester
                AddYearAndSemester(wordDoc, $"{year} Year, 1st Semester");
                currentSemester = "1";

                blockSectionIds = GetBlockSectionIds(CurriculumId, currentSemester, currentYear); //Gets list of block Section for current Year and Semester
                for (int i = 0; i < blockSectionIds.Count; i++) //Goes through every Block Section in the list
                {
                    int blockSectionId = blockSectionIds[i]; //Declare Block Section Id

                    string blockSectionName = GetBlockSectionName(blockSectionId); //Take Block Section Name

                    AddBlockSectionName(wordDoc, blockSectionName); //Add block Section Name in Word

                    loadSchedule(blockSectionId); //Load Class Schedule of Block section to a Data Grid

                    AddDataGridToWordTable(wordDoc, schedule_data); // Get data grid and put it into doc table

                    // Add a new paragraph for spacing
                    Word.Paragraph spacingPara = wordDoc.Content.Paragraphs.Add();
                    spacingPara.Range.InsertParagraphAfter();


                }



                // Add 2nd Semester
                AddYearAndSemester(wordDoc, $"{year} Year, 2nd Semester");
                currentSemester = "2";

                blockSectionIds = GetBlockSectionIds(CurriculumId, currentSemester, currentYear);
                for (int i = 0; i < blockSectionIds.Count; i++)
                {
                    int blockSectionId = blockSectionIds[i];
                    string blockSectionName = GetBlockSectionName(blockSectionId);
                    AddBlockSectionName(wordDoc, blockSectionName);
                    loadSchedule(blockSectionId);
                    AddDataGridToWordTable(wordDoc, schedule_data); // Get data grid and put it into doc table

                    // Add a new paragraph for spacing
                    Word.Paragraph spacingPara = wordDoc.Content.Paragraphs.Add();
                    spacingPara.Range.InsertParagraphAfter();

                }

                // Add Summer
                AddYearAndSemester(wordDoc, $"{year} Year, Summer");
                currentSemester = "Summer";

                blockSectionIds = GetBlockSectionIds(CurriculumId, currentSemester, currentYear);
                for (int i = 0; i < blockSectionIds.Count; i++)
                {
                    int blockSectionId = blockSectionIds[i];
                    string blockSectionName = GetBlockSectionName(blockSectionId);
                    AddBlockSectionName(wordDoc, blockSectionName);
                    loadSchedule(blockSectionId);
                    AddDataGridToWordTable(wordDoc, schedule_data); // Get data grid and put it into doc table

                    // Add a new paragraph for spacing
                    Word.Paragraph spacingPara = wordDoc.Content.Paragraphs.Add();
                    spacingPara.Range.InsertParagraphAfter();

                }
            }
        }
        private void AddDataGridToWordTable(Word.Document wordDoc, DataGrid dataGrid)
        {
            // Get the number of rows and columns in the DataGrid
            int rowCount = dataGrid.Items.Count;

            // Count only visible columns
            int visibleColumnCount = dataGrid.Columns.Count(col => col.Visibility == Visibility.Visible);

            // Move the content range to the end of the document
            Word.Range range = wordDoc.Content;
            range.Collapse(Word.WdCollapseDirection.wdCollapseEnd); // Move to the end of the document

            // Create a table in the Word document with only visible columns
            Word.Table wordTable = wordDoc.Tables.Add(range, rowCount + 1, visibleColumnCount);
            wordTable.Borders.Enable = 1; // Enable borders for the table

            // Add headers to the first row of the table
            int headerColIndex = 1; // To track the column index for the Word table
            for (int col = 0; col < dataGrid.Columns.Count; col++)
            {
                // Only add headers for visible columns
                if (dataGrid.Columns[col].Visibility == Visibility.Visible)
                {
                    wordTable.Cell(1, headerColIndex).Range.Text = dataGrid.Columns[col].Header.ToString();
                    headerColIndex++; // Move to the next column in the Word table
                }
            }

            // Add data from the DataGrid to the Word table
            for (int row = 0; row < rowCount; row++)
            {
                int dataColIndex = 1; // To track the column index for the Word table
                for (int col = 0; col < dataGrid.Columns.Count; col++)
                {
                    // Only add data for visible columns
                    if (dataGrid.Columns[col].Visibility == Visibility.Visible)
                    {
                        // Get the value from the DataGrid
                        var cellValue = (dataGrid.Items[row] as DataRowView)?.Row[col].ToString() ?? string.Empty;
                        wordTable.Cell(row + 2, dataColIndex).Range.Text = cellValue; // +2 because the first row is for headers
                        dataColIndex++; // Move to the next column in the Word table
                    }
                }
            }
        }

        #endregion

        #region DataGrid

        private void loadSchedule(int blockSectionId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to load data from the class table with filtering by Block_Section_Id
                    string query = @"
                        SELECT c.Class_Id,
                               c.Subject_Id,
                               c.Internal_Employee_Id,
                               c.Room_Id,
                               c.Stub_Code,
                               c.Class_Day,
                               c.Start_Time,
                               c.End_Time,
                               s.Subject_Code AS SubjectCode,
                               CONCAT(i.Lname, ', ', i.Fname) AS InstructorName,
                               r.Room_Code AS RoomCode
                        FROM class c
                        LEFT JOIN subjects s ON c.Subject_Id = s.Subject_Id
                        LEFT JOIN instructor i ON c.Internal_Employee_Id = i.Internal_Employee_Id
                        LEFT JOIN rooms r ON c.Room_Id = r.Room_Id
                        WHERE c.Block_Section_Id = @BlockSectionId"; // Add WHERE clause to filter by Block_Section_Id

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BlockSectionId", blockSectionId); // Add parameter for Block_Section_Id

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

        private List<int> GetBlockSectionIds(int curriculumId, string semester, int yearLevel)
        {
            List<int> blockSectionIds = new List<int>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to get blockSectionId based on curriculumId, semester, and year_level
                    string query = @"
                SELECT blockSectionId 
                FROM block_section 
                WHERE curriculumId = @CurriculumId 
                  AND semester = @Semester 
                  AND year_level = @YearLevel";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CurriculumId", curriculumId);
                    command.Parameters.AddWithValue("@Semester", semester);
                    command.Parameters.AddWithValue("@YearLevel", yearLevel);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Add each blockSectionId to the list
                            blockSectionIds.Add(reader.GetInt32("blockSectionId"));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving block section IDs: " + ex.Message);
            }

            return blockSectionIds;
        }
        private string GetBlockSectionName(int blockSectionId)
        {
            string blockSectionName = string.Empty;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to get blockSectionName based on blockSectionId
                    string query = @"
            SELECT blockSectionName 
            FROM block_section 
            WHERE blockSectionId = @BlockSectionId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BlockSectionId", blockSectionId);

                    // Execute the query and read the result
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Get the blockSectionName from the result
                            blockSectionName = reader.GetString("blockSectionName");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving block section name: " + ex.Message);
            }

            return blockSectionName;
        }



        #endregion

        private void test_btn_Click(object sender, RoutedEventArgs e)
        {
            

            //try
            //{
            //    // Step 1: Prompt the user to select the file path
            //    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            //    {
            //        Title = "Save Report",
            //        Filter = "Word Document (*.docx)|*.docx",
            //        FileName = "Curriculum_Report.docx"
            //    };

            //    if (saveFileDialog.ShowDialog() == true)
            //    {
            //        string filePath = saveFileDialog.FileName;

            //        // Step 2: Create a new Word application and document
            //        Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
            //        Word.Document wordDoc = wordApp.Documents.Add();


            //        loadSchedule(6);
            //        AddDataGridToWordTable(wordDoc, schedule_data);

            //        // Step 4: Save the document to the specified file path
            //        wordDoc.SaveAs2(filePath);
            //        wordDoc.Close();
            //        wordApp.Quit();

            //        MessageBox.Show($"Report successfully created at:\n{filePath}");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error: " + ex.Message);
            //}

        }
    }


}

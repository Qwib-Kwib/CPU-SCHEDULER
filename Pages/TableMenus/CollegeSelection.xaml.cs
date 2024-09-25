using Info_module.Pages.TableMenus.After_College_Selection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for CollegeSelection.xaml
    /// </summary>
    public partial class CollegeSelection : Page
    {
        private StackPanel[] stackPanels;

        private string SourceButton { get; set; }

        string connectionString = App.ConnectionString;

        public List<(int DeptId, string DeptName, BitmapImage ImageSource)> GetDepartments()
        {
            var departments = new List<(int DeptId, string DeptName, BitmapImage ImageSource)>();

            string query = "SELECT Dept_Id, Dept_Name, Logo_Image FROM departments WHERE Status = 1 ORDER BY Dept_Name";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                conn.Open();
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int deptId = reader.GetInt32("Dept_Id");
                        string deptName = reader.GetString("Dept_Name");
                        byte[] logoBytes = reader["Logo_Image"] as byte[];

                        BitmapImage bitmap = new BitmapImage();
                        using (var stream = new MemoryStream(logoBytes))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                        }

                        departments.Add((deptId, deptName, bitmap));
                    }
                }
            }

            return departments;
        }


        public CollegeSelection(string sourceButton)
        {

            InitializeComponent();
            InitializeStackPanels();
            AddDepartmentButtons();

            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "College Selection";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
            SourceButton = sourceButton;
            SourceButton = sourceButton;
         }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        private void NavigateToNextPage(int Department_Id)
        {
            Page nextPage = null;

            if (SourceButton == "Curriculum")
            {
                nextPage = new CurriculumMenu(Department_Id);
            }
            else if (SourceButton == "Instructor")
            {
                nextPage = new InstructorMenu(Department_Id);
            }

            // Navigate to the next page if it's not null
            if (nextPage != null)
            {
                NavigationService.Navigate(nextPage);
            }
        }

        private void InitializeStackPanels()
        {
            stackPanels = new StackPanel[] { column_1, column_2, column_3, column_4, column_5, column_6 };
        }

        private void AddDepartmentButtons()
        {
            // Get the departments from the database
            var departments = GetDepartments();

            if (departments == null || departments.Count == 0)
            {
                Debug.WriteLine("No departments found or error in fetching departments.");
                return;
            }
            int panelIndex = 0;
            bool toggleBorder = false;
            foreach (var department in departments)
            {
                // Create a new button
                Button button = new Button
                {
                    Height = 120,
                    Width = 120,
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = null,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100,202, 173, 34)),
                    Tag = department.DeptId // Store Dept_Id in Tag property
                };

                // Create the grid
                Grid grid = new Grid
                {
                    Height = 120,
                    Width = 120
                };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30, GridUnitType.Star) });

                // Create the Image
                Image image = new Image
                {
                    Margin = new Thickness(16, 2, 16, 2),
                    Source = department.ImageSource
                };
                Grid.SetRow(image, 0); // Assign to Row 0
                grid.Children.Add(image);

                // Create the TextBlock
                TextBlock textBlock = new TextBlock
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Text = department.DeptName,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(textBlock, 1); // Assign to Row 1
                grid.Children.Add(textBlock);

                // Add the grid to the button
                button.Content = grid;

                if (toggleBorder)
                {
                    button.BorderBrush = new SolidColorBrush(Color.FromArgb(100,0, 22, 202)); // Second color #FF0016CA
                }
                toggleBorder = !toggleBorder;

                button.Click += (sender, e) =>
                {
                    // Handle button click here
                    var deptId = (int)((Button)sender).Tag; // Retrieve DeptId from Tag
                    NavigateToNextPage(deptId);
                };

                // Add button to the respective StackPanel
                stackPanels[panelIndex].Children.Add(button);

                // Cycle through the StackPanels
                panelIndex = (panelIndex + 1) % stackPanels.Length;
            }
        }

    }
}

using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;

namespace Info_module.Pages.TableMenus.CollegeMenu
{
    /// <summary>
    /// Interaction logic for CollegeMenuEdit.xaml
    /// </summary>
    public partial class CollegeMenuEdit : Window
    {
        public int DepartmentId { get; set; }
        string connectionString = App.ConnectionString;

        public CollegeMenuEdit(int departmentId)
        {
            InitializeComponent();
            DepartmentId = departmentId;
            PopulateBuildingCodes();
            LoadCollegeData(departmentId);
            LoadAndDisplayImage(departmentId);
        }

        private void LoadCollegeData(int departmentId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to select the department with the specified ID
                    string query = @"
                        SELECT 
                            d.Building_Id,
                            d.Dept_Code AS 'Department_Code',
                            d.Dept_Name AS 'Department_Name'
                        FROM departments d
                        WHERE d.Dept_Id = @DepartmentId"; // Filter by Department_Id

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@DepartmentId", departmentId); // Add parameter

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Read the first row
                        {
                            // Populate the text boxes with the data
                            buildingCode_cbx.SelectedValue = reader["Building_Id"]; // Assuming this is the correct type
                            deparmentCode_txt.Text = reader["Department_Code"].ToString();
                            departmentName_txt.Text = reader["Department_Name"].ToString();
                        }
                        else
                        {
                            MessageBox.Show("No department found with the specified ID.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateBuildingCodes()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Id, Building_Code FROM buildings";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    // Assuming buildingCode_cbx is your ComboBox name
                    buildingCode_cbx.ItemsSource = dataTable.DefaultView;
                    buildingCode_cbx.DisplayMemberPath = "Building_Code";
                    buildingCode_cbx.SelectedValuePath = "Building_Id";
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building codes: " + ex.Message);
            }
        }
        private byte[] uploadedImageBytes;

        private void imageUpload_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filename = openFileDialog.FileName;
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        uploadedImageBytes = new byte[fs.Length];
                        fs.Read(uploadedImageBytes, 0, uploadedImageBytes.Length);
                    }
                    // Display image in preview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(uploadedImageBytes);
                    bitmap.EndInit();
                    logoPreview_img.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error uploading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private byte[] currentImageBytes;

        private void LoadAndDisplayImage(int departmentId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Logo_Image FROM departments WHERE Dept_Id = @Dept_Id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Dept_Id", departmentId);

                    // Execute the query and retrieve the image bytes
                    object result = command.ExecuteScalar();

                    // Check if the result is not null and is of type byte[]
                    if (result is byte[] imageBytes && imageBytes.Length > 0)
                    {
                        currentImageBytes = imageBytes;
                        BitmapImage bitmap = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(imageBytes))
                        {
                            stream.Position = 0;
                            bitmap.BeginInit();
                            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = null;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                        }
                        bitmap.Freeze(); // Freeze to make it cross-thread accessible
                        logoPreview_img.Source = bitmap;
                    }
                    else
                    {
                        // If no image is found, clear the image display
                        logoPreview_img.Source = null;
                        currentImageBytes = null;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Database error while loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Update_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (buildingCode_cbx.SelectedValue == null)
            {
                MessageBox.Show("Please select a building code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(deparmentCode_txt.Text))
            {
                MessageBox.Show("Please enter a department code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(departmentName_txt.Text))
            {
                MessageBox.Show("Please enter a department name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                                UPDATE departments 
                                SET Building_Id = @Building_Id, 
                                    Dept_Code = @Dept_Code, 
                                    Dept_Name = @Dept_Name" +
                                    (uploadedImageBytes != null ? ", Logo_Image = @Logo_Image" : "") +
                                " WHERE Dept_Id = @Dept_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Dept_Id", DepartmentId);
                        command.Parameters.AddWithValue("@Building_Id", buildingCode_cbx.SelectedValue);
                        command.Parameters.AddWithValue("@Dept_Code", deparmentCode_txt.Text);
                        command.Parameters.AddWithValue("@Dept_Name", departmentName_txt.Text);

                        if (uploadedImageBytes != null)
                        {
                            command.Parameters.AddWithValue("@Logo_Image", uploadedImageBytes);
                        }

                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Department updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();

            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating department: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}

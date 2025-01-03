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
    /// Interaction logic for CollegeMenuAdd.xaml
    /// </summary>
    public partial class CollegeMenuAdd : Window
    {
        string connectionString = App.ConnectionString;

        public CollegeMenuAdd()
        {
            InitializeComponent();
            PopulateBuildingCodes();
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

        private void Add_btn_Click(object sender, RoutedEventArgs e)
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
                    string query = @"INSERT INTO departments (Building_Id, Dept_Code, Dept_Name, Logo_Image) 
                             VALUES (@Building_Id, @Dept_Code, @Dept_Name, @Logo_Image)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Building_Id", buildingCode_cbx.SelectedValue);
                        command.Parameters.AddWithValue("@Dept_Code", deparmentCode_txt.Text);
                        command.Parameters.AddWithValue("@Dept_Name", departmentName_txt.Text);

                        // Check if the logo image is provided; if not, set it to DBNull
                        if (uploadedImageBytes != null && uploadedImageBytes.Length > 0)
                        {
                            command.Parameters.AddWithValue("@Logo_Image", uploadedImageBytes);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@Logo_Image", DBNull.Value);
                        }

                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Department added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding department: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

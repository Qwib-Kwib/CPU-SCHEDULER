using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Info_module.Pages.TableMenus
{
    public partial class BuildingMenu : Page
    {
        // Store the currently selected status
        private string currentStatus = "Active";

        public BuildingMenu()
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Building Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        string connectionString = App.ConnectionString;

        private void LoadData(string statusFilter = "Active")
        {
            currentStatus = statusFilter; // Store the current status filter
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                        Building_Id, Building_Code, Building_Name,
                                        CASE
                                            When Status = 1 then 'Active'
                                            Else 'Inactive'
                                        End as 'Status'
                                    FROM buildings";

                    // Apply filter based on the status
                    if (statusFilter == "Active")
                    {
                        query += " WHERE Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " WHERE Status = 0";
                    }

                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    account_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                string selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadData(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    status_btn.Content = "Deactivate"; // For active departments
                    status_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    status_btn.Content = "Activate"; // For inactive departments
                    status_btn.FontSize = 12;
                }
                else
                {
                    status_btn.Content = "Switch Status"; // Default text for "All"
                    status_btn.FontSize = 6;
                }
            }
        }


        private void btnCSVRooms_Click(object sender, RoutedEventArgs e)
        {
            int buildingId;
            if (!int.TryParse(ID_txt.Text, out buildingId))
            {
                MessageBox.Show("Pick a valid Building Id.");
                return;
            }

            CSVRooms rooms = new CSVRooms(buildingId);
            NavigationService.Navigate(rooms);
        }

        private void building_data_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            string buildingCode = buildingCode_txt.Text;
            string buildingName = buildingName_txt.Text;
            string posX = posX_txt.Text;
            string posY = PosY_txt.Text;

            if (string.IsNullOrWhiteSpace(buildingCode) || string.IsNullOrWhiteSpace(buildingName) ||
                string.IsNullOrWhiteSpace(posX) || string.IsNullOrWhiteSpace(posY))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO buildings (Building_Code, Building_Name, Pos_X, Pos_Y, status) VALUES (@buildingCode, @buildingName, @posX, @posY, 1)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildingCode", buildingCode);
                        command.Parameters.AddWithValue("@buildingName", buildingName);
                        command.Parameters.AddWithValue("@posX", posX);
                        command.Parameters.AddWithValue("@posY", posY);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Building added successfully.");
                LoadData();
                ClearTextFields();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding building: " + ex.Message);
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ID_txt.Text, out int buildingId))
            {
                MessageBox.Show("Invalid ID.");
                return;
            }

            string buildingCode = buildingCode_txt.Text;
            string buildingName = buildingName_txt.Text;
            string posX = posX_txt.Text;
            string posY = PosY_txt.Text;

            if (string.IsNullOrWhiteSpace(buildingCode) || string.IsNullOrWhiteSpace(buildingName) ||
                string.IsNullOrWhiteSpace(posX) || string.IsNullOrWhiteSpace(posY))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE buildings SET Building_Code = @buildingCode, Building_Name = @buildingName, Pos_X = @posX, Pos_Y = @posY WHERE Building_Id = @buildingId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildingId", buildingId);
                        command.Parameters.AddWithValue("@buildingCode", buildingCode);
                        command.Parameters.AddWithValue("@buildingName", buildingName);
                        command.Parameters.AddWithValue("@posX", posX);
                        command.Parameters.AddWithValue("@posY", posY);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Building details updated successfully.");
                LoadData();
                ClearTextFields();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating building details: " + ex.Message);
            }
        }

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            if (account_data.SelectedItem != null)
            {
                DataRowView selectedRow = account_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    int buildingId = Convert.ToInt32(selectedRow["Building_Id"]);
                    string currentStatus = selectedRow["Status"].ToString(); // Get the current status

                    // Determine the new status value
                    int newStatus = (currentStatus == "Active") ? 0 : 1; // Toggle status

                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "UPDATE buildings SET status = @NewStatus WHERE Building_Id = @Building_Id";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@NewStatus", newStatus);
                                command.Parameters.AddWithValue("@Building_Id", buildingId);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Refresh data after update with the current status filter
                        LoadData(currentStatus); // Reapply the current filter

                        string message = (newStatus == 0) ? "Building set to Inactive successfully." : "Building set to Active successfully.";
                        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error updating building status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a building to change its status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        private void account_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (account_data.SelectedItem is DataRowView selectedRow)
            {
                ID_txt.Text = selectedRow["Building_Id"].ToString();
                buildingCode_txt.Text = selectedRow["Building_Code"].ToString();
                buildingName_txt.Text = selectedRow["Building_Name"].ToString();
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearTextFields();
        }

        private void ClearTextFields()
        {
            ID_txt.Text = string.Empty;
            buildingCode_txt.Text = string.Empty;
            buildingName_txt.Text = string.Empty;
            posX_txt.Text = string.Empty;
            PosY_txt.Text = string.Empty;
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Accept only digits (0-9) and one decimal point
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
            {
                e.Handled = true;
            }
            // Ensure only one decimal point is allowed
            TextBox textBox = sender as TextBox;
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }
    }
}

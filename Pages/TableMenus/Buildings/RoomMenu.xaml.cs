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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.IO.Packaging;
using Info_module.ViewModels;

namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for CSVRooms.xaml
    /// </summary>
    /// plssssssssssss
    public partial class CSVRooms : Page
    {
        private int buildingId;
        private string buildingCode;
        private string buildingName;
        private int selectedRoomId;
        private string selectedBuildingCode;
        private string selectedRoomCode;
        private int selectedFloorLevel;
        private string selectedRoomType;
        private int selectedMaxSeat;
        private int selectedStatus;

        string connectionString = App.ConnectionString;

        public CSVRooms(int buildingId)
        {

            InitializeComponent();
            this.buildingId = buildingId;
            LoadUI();

            
            
        }

        #region UI

        private void LoadUI()
        {
            selectedStatus = 0;
            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Rooms Menu", TopBar_BackButtonClicked);
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Code, Building_Name FROM buildings WHERE Building_Id = @buildingId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@buildingId", buildingId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            buildingCode = reader["Building_Code"].ToString();
                            buildingName = reader["Building_Name"].ToString();
                            
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building details: " + ex.Message);
            }

            buildingName_txtblck.Text = buildingName;
            LoadBuilding();
            //string what = buildingCode;
            //test_txt.Text = what;
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new BuildingMap());
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();

        }
        #endregion

        #region DATA GRID

        private void room_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (room_data.SelectedItem is DataRowView selectedRow)
            {
                selectedRoomId = Convert.ToInt32(selectedRow["Room_Id"]);
                selectedBuildingCode = selectedRow["Building_Code"].ToString();
                selectedRoomCode = selectedRow["Room_Code"].ToString();
                selectedFloorLevel = Convert.ToInt32(selectedRow["Floor_Level"]);
                selectedRoomType = selectedRow["Room_Type"].ToString();
                selectedMaxSeat = Convert.ToInt32(selectedRow["Max_Seat"]);
                if (selectedRow["Status"].ToString() == "Active")
                {
                    selectedStatus = 1;
                }
                else if (selectedRow["Status"].ToString() == "Inactive")
                {
                    selectedStatus = 0;
                }


                roomId_txt.Text = selectedRoomId.ToString();
                buildingCode_txt.Text = selectedBuildingCode.ToString();
                roomCode_txt.Text = selectedRoomCode.ToString();
                roomFloor_txt.Text = selectedFloorLevel.ToString();
                foreach (ComboBoxItem item in roomType_cmbx.Items)
                {
                    if (item.Content.ToString() == selectedRoomType)
                    {
                        roomType_cmbx.SelectedItem = item;
                        break;
                    }
                }
                maxSeat_txt.Text = selectedMaxSeat.ToString();

            }
        }

        private void LoadBuilding(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    Room_Id AS 'Room_Id', 
                    Room_Code, 
                    Room_Floor AS 'Floor_Level', 
                    Room_Type,  
                    Max_Seat, 
                    CASE 
                        WHEN Status = 1 THEN 'Active' 
                        ELSE 'Inactive' 
                    END AS 'Status'
                FROM rooms
                WHERE Building_Id = @BuildingId";

                    // Modify the query based on the status filter
                    if (statusFilter == "Active")
                    {
                        query += " AND Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND Status = 0";
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BuildingId", buildingId);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    // Add the Building_Code column and set its value for each row
                    dataTable.Columns.Add("Building_Code", typeof(string));
                    foreach (DataRow row in dataTable.Rows)
                    {
                        row["Building_Code"] = buildingCode; // Make sure buildingCode is set correctly
                    }

                    room_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                string selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadBuilding(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    statusRoom_btn.Content = "Deactivate"; // For active departments
                    statusRoom_btn.FontSize = 11;
                }
                else if (selectedStatus == "Inactive")
                {
                    statusRoom_btn.Content = "Activate"; // For inactive departments
                    statusRoom_btn.FontSize = 12;
                }
                else
                {
                    statusRoom_btn.Content = "Switch Status"; // Default text for "All"
                    statusRoom_btn.FontSize = 10;
                }
            }

        }




        #endregion

        #region FORMS


        private void roomFloor_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void maxSeat_txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }

        private void ClearFormInputs()
        {
            roomId_txt.Clear();
            buildingCode_txt.Clear();
            roomFloor_txt.Clear();
            roomCode_txt.Clear();
            roomType_cmbx.SelectedIndex = -1;
            maxSeat_txt.Clear();
        }

        private void addRoom_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validation: Check if all relevant forms are filled
            if (string.IsNullOrWhiteSpace(roomCode_txt.Text) ||
                string.IsNullOrWhiteSpace(roomFloor_txt.Text) ||
                roomType_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(maxSeat_txt.Text))
            {
                MessageBox.Show("Please fill out all fields before adding a room.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Stop execution if validation fails
            }

            // Additional validation for numeric inputs
            if (!int.TryParse(roomFloor_txt.Text, out int roomFloor) || !int.TryParse(maxSeat_txt.Text, out int maxSeat))
            {
                MessageBox.Show("Room Floor and Max Seat must be valid numbers.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO rooms (Building_Id, Room_Code, Room_Floor, Room_Type, Max_Seat) 
                             VALUES (@Building_Id, @Room_Code, @Room_Floor, @Room_Type, @Max_Seat)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Building_Id", buildingId);
                        command.Parameters.AddWithValue("@Room_Code", roomCode_txt.Text);
                        command.Parameters.AddWithValue("@Room_Floor", roomFloor);
                        command.Parameters.AddWithValue("@Room_Type", (roomType_cmbx.SelectedItem as ComboBoxItem)?.Content.ToString()); // Get room type text
                        command.Parameters.AddWithValue("@Max_Seat", maxSeat);
                        command.ExecuteNonQuery();
                    }
                }
                LoadBuilding();
                ClearFormInputs();
                MessageBox.Show("Room added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding Room: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void updateRoom_btn_Click(object sender, RoutedEventArgs e)
        {
            // Validation: Check if all relevant forms are filled
            if (string.IsNullOrWhiteSpace(roomCode_txt.Text) ||
                string.IsNullOrWhiteSpace(roomFloor_txt.Text) ||
                roomType_cmbx.SelectedItem == null ||
                string.IsNullOrWhiteSpace(maxSeat_txt.Text))
            {
                MessageBox.Show("Please fill out all fields before adding a room.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Stop execution if validation fails
            }

            // Additional validation for numeric inputs
            if (!int.TryParse(roomFloor_txt.Text, out int roomFloor) || !int.TryParse(maxSeat_txt.Text, out int maxSeat))
            {
                MessageBox.Show("Room Floor and Max Seat must be valid numbers.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    UPDATE rooms
                    SET Room_Code = @Room_Code, 
                        Room_Floor = @Room_Floor, 
                        Room_Type = @Room_Type,
                        Max_Seat = @Max_Seat                   
                    WHERE Room_Id = @Room_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Room_Id", Convert.ToInt32(roomId_txt.Text));
                        command.Parameters.AddWithValue("@Room_Code", roomCode_txt.Text);
                        command.Parameters.AddWithValue("@Room_Floor", roomFloor);
                        command.Parameters.AddWithValue("@Room_Type", (roomType_cmbx.SelectedItem as ComboBoxItem)?.Content.ToString()); // Updated Room Type
                        command.Parameters.AddWithValue("@Max_Seat", maxSeat);
                        command.ExecuteNonQuery();
                    }
                }
                LoadBuilding();
                ClearFormInputs();
                MessageBox.Show("Room updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating Room: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFormInputs();
        }

        private void switchCsv_click(object sender, RoutedEventArgs e)
        {
            roomForm_grid.Visibility = Visibility.Hidden;
            roomCsv_grid.Visibility = Visibility.Visible;
            roomCsv_grid.Margin = new Thickness(10, 10, 10, 10);
            Buttons(false);

        }

        private void statusRoom_btn_Click(object sender, RoutedEventArgs e)
        {
            if (room_data.SelectedItems.Count > 0)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        foreach (DataRowView rowView in room_data.SelectedItems)
                        {
                            DataRow row = rowView.Row;
                            int roomId = selectedRoomId;
                            int currentStatus = 0;
                            if (row["Status"].ToString() == "Active")
                            {
                                currentStatus = 1;
                            }
                            else if (row["Status"].ToString() == "Inactive")
                            {
                                currentStatus = 0;
                            }

                            int newStatus = (currentStatus == 1) ? 0 : 1;

                            string query = "UPDATE rooms SET status = @Status WHERE Room_Id = @RoomId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Status", newStatus);
                                command.Parameters.AddWithValue("@RoomId", roomId);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadBuilding(); // Refresh data after updating status
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one entry to update status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region CSV

        private void switchForm_btn_Click(object sender, RoutedEventArgs e)
        {
            roomForm_grid.Visibility = Visibility.Visible;
            roomCsv_grid.Visibility = Visibility.Hidden;
            roomCsv_grid.Margin = new Thickness(-185, 10, 205, 10);
        }

        private void InsertDataIntoDatabase(DataTable dataTable)
        {
            int skippedCount = 0; // Counter for skipped rows

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Only insert rows that have not already been inserted
                        if (row.RowState == DataRowState.Added)
                        {
                            // First, check if the Room_Code already exists
                            string checkQuery = "SELECT COUNT(*) FROM rooms WHERE Room_Code = @Room_Code";
                            using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection))
                            {
                                checkCommand.Parameters.AddWithValue("@Room_Code", row["Room_Code"]);
                                int roomExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                                // If room code exists, skip this row and count it
                                if (roomExists > 0)
                                {
                                    skippedCount++;
                                    continue; // Skip to the next row
                                }
                            }

                            // If room does not exist, insert it
                            string insertQuery = "INSERT INTO rooms (Building_Id, Room_Code, Room_Floor, Room_Type, Max_Seat, status) " +
                                                 "VALUES (@Building_Id, @Room_Code, @Room_Floor, @Room_Type, @Max_Seat, @status)";
                            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@Building_Id", buildingId);
                                insertCommand.Parameters.AddWithValue("@Room_Code", row["Room_Code"]);
                                insertCommand.Parameters.AddWithValue("@Room_Floor", row["Floor_Level"]);
                                insertCommand.Parameters.AddWithValue("@Room_Type", row["Room_Type"]);
                                insertCommand.Parameters.AddWithValue("@Max_Seat", row["Max_Seat"]);
                                insertCommand.Parameters.AddWithValue("@status", row["Status"]);

                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // Notify user how many rows were inserted and how many were skipped
                MessageBox.Show($"{dataTable.Rows.Count - skippedCount} rows were inserted successfully. {skippedCount} rows were skipped due to duplicate Room_Code.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data into database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Upload_btn_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("Exclude ID and Building Code from CSV.");

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
                        room_data.ItemsSource = dataTable.DefaultView;
                        Buttons(true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private DataTable ReadCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns based on the expected CSV format
                    csvData.Columns.Add("Room_Code", typeof(string));
                    csvData.Columns.Add("Floor_Level", typeof(int));
                    csvData.Columns.Add("Room_Type", typeof(string));
                    csvData.Columns.Add("Max_Seat", typeof(int));
                    csvData.Columns.Add("Status", typeof(int));

                    // Read the header line first to skip it
                    sr.ReadLine();

                    // Read the data lines
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        // Ensure that the CSV row has the expected number of columns
                        if (rows.Length != 6)
                        {
                            MessageBox.Show("Error: CSV file format is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr["Room_Code"] = rows[0].Trim();
                            dr["Floor_Level"] = int.Parse(rows[1].Trim());
                            dr["Room_Type"] = rows[2].Trim();
                            dr["Max_Seat"] = int.Parse(rows[4].Trim());
                            dr["Status"] = int.Parse(rows[5].Trim());

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

        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (room_data.Items.Count == 0)
            {
                MessageBox.Show("No data to add.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataView dataView = (DataView)room_data.ItemsSource;
            DataTable dataTable = dataView.Table;
            InsertDataIntoDatabase(dataTable);
            Buttons(false);
        }
        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation dialog
            MessageBoxResult result = MessageBox.Show("Are you sure you want to cancel?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // If the user clicks Yes, clear the DataGrid
            if (result == MessageBoxResult.Yes)
            {
                LoadBuilding();
                Buttons(false);
            }
        }

        private void Buttons(bool isVisible)
        {
            if (isVisible)
            {
                cancel_btn.Visibility = Visibility.Visible;
                add_btn.Visibility = Visibility.Visible;
            }
            else
            {
                cancel_btn.Visibility = Visibility.Hidden;
                add_btn.Visibility = Visibility.Hidden;
            }
        }








        #endregion


    }

}

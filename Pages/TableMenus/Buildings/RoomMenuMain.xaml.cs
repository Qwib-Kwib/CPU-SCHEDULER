using Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu;
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

namespace Info_module.Pages.TableMenus.Buildings
{
    /// <summary>
    /// Interaction logic for RoomMenuMain.xaml
    /// </summary>
    public partial class RoomMenuMain : Page
    {

        private int BuildingId;

        string connectionString = App.ConnectionString;

        string buildingName;
        string buildingCode;
        int RoomId = -1;

        public RoomMenuMain(int buildingId)
        {
            InitializeComponent();
            LoadBuildingName(buildingId);
            BuildingId = buildingId;

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Rooms Menu", TopBar_BackButtonClicked);
            LoadRooms();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new BuildingMenu());
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();

        }

        private void LoadBuildingName(int BuildingNameId)
        {
            
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Code, Building_Name FROM buildings WHERE Building_Id = @buildingId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@buildingId", BuildingNameId);
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

            buildingName_txtblck.Text = $"({buildingCode}), {buildingName}";
        }

        private void LoadRooms(string statusFilter = "Active")
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
                    command.Parameters.AddWithValue("@BuildingId", BuildingId);

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
            string selectedStatus = "Active";
            if (statusRoom_btn == null)
            {
                return;
            }

            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadRooms(selectedStatus);

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

        private void addRoom_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                RoomMenuAdd windowMenu = new RoomMenuAdd(BuildingId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                windowMenu.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                LoadRooms();
            }
        }

        private void updateRoom_btn_Click(object sender, RoutedEventArgs e)
        {
            if (RoomId == -1)
            {
                MessageBox.Show("Please select a Room ", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                RoomMenuEdit windowMenu = new RoomMenuEdit(RoomId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,

                };
                windowMenu.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
                LoadRooms();
            }

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
                            int roomId = RoomId;
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
                    LoadRooms(); // Refresh data after updating status
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

        private void switchCsv_click(object sender, RoutedEventArgs e)
        {
            try
            {
                dim_rectangle.Visibility = Visibility.Visible;

                Window hostWindow = Window.GetWindow(this);

                RoomMenuCsv windowMenu = new RoomMenuCsv(BuildingId)
                {
                    Owner = hostWindow, // Set the current window as the owner
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,


                };
                windowMenu.ShowDialog();
            }
            finally
            {
                // Hide the dim overlay when the dialog is closed
                dim_rectangle.Visibility = Visibility.Collapsed;
             
                LoadRooms();
            }

        }

        private void room_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (room_data.SelectedItem is DataRowView selectedRow)
            {
                RoomId = Convert.ToInt32(selectedRow["Room_Id"]);
            }

        }
    }
}

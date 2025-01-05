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

namespace Info_module.Pages.TableMenus.Buildings
{
    /// <summary>
    /// Interaction logic for RoomMenuAdd.xaml
    /// </summary>
    public partial class RoomMenuAdd : Window
    {
        public int BuildingId { get; set; }
        string connectionString = App.ConnectionString;
        public RoomMenuAdd(int buildingId)
        {
            InitializeComponent();
            BuildingId = buildingId;
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
                        command.Parameters.AddWithValue("@Building_Id", BuildingId);
                        command.Parameters.AddWithValue("@Room_Code", roomCode_txt.Text);
                        command.Parameters.AddWithValue("@Room_Floor", roomFloor);
                        command.Parameters.AddWithValue("@Room_Type", (roomType_cmbx.SelectedItem as ComboBoxItem)?.Content.ToString()); // Get room type text
                        command.Parameters.AddWithValue("@Max_Seat", maxSeat);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Room added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding Room: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IsTextInt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }
    }
}

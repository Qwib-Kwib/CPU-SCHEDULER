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

namespace Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu
{
    /// <summary>
    /// Interaction logic for RoomMenuEdit.xaml
    /// </summary>
    public partial class RoomMenuEdit : Window
    {
        public int RoomId { get; set; }

        string connectionString = App.ConnectionString;


        public RoomMenuEdit(int roomId)
        {
            InitializeComponent();
            RoomId = roomId;   
            LoadRoomDetails();
            roomId_txt.Text = roomId.ToString();
        }

        private void LoadRoomDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            Room_Code, 
                            Room_Floor, 
                            Room_Type,  
                            Max_Seat, 
                            CASE 
                                WHEN Status = 1 THEN 'Active' 
                                ELSE 'Inactive' 
                            END AS 'Status'
                        FROM rooms
                        WHERE Room_Id = @roomId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@roomId", RoomId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            roomCode_txt.Text = reader["Room_Code"].ToString();
                            roomFloor_txt.Text = reader["Room_Floor"].ToString();
                            foreach (ComboBoxItem item in roomType_cmbx.Items)
                            {
                                if (item.Content.ToString() == reader["Room_Type"].ToString())
                                {
                                    roomType_cmbx.SelectedItem = item;
                                    break;
                                }
                            }
                            maxSeat_txt.Text = reader["Max_Seat"].ToString();

                        }
                        else 
                        {

                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void update_btn_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Room updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating Room: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void IsTextInt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = App.IsTextNumeric(e.Text);
        }
    }
}

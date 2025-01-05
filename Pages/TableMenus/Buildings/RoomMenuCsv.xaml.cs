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

namespace Info_module.Pages.TableMenus.Buildings
{
    /// <summary>
    /// Interaction logic for RoomMenuCsv.xaml
    /// </summary>
    public partial class RoomMenuCsv : Window
    {

        string connectionString = App.ConnectionString;

        public int BuildingId {  get; set; }
        
        public RoomMenuCsv(int buildingId)
        {
            InitializeComponent();
            this.BuildingId = buildingId;
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to cancel?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // If the user clicks Yes, clear the DataGrid
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
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
                                insertCommand.Parameters.AddWithValue("@Building_Id", BuildingId);
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
        }
    }
}

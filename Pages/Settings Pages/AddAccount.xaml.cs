using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Security.Cryptography;
using System.Data;

namespace Info_module.Pages.Settings_Pages
{
    /// <summary>
    /// Interaction logic for AddAccount.xaml
    /// </summary>
    public partial class AddAccount : Page
    {
        string connectionString = App.ConnectionString;

        public AddAccount()
        {
            InitializeComponent();
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            string username = username_txt.Text;
            string password = password_txt.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and Password cannot be empty.");
                return;
            }

            if (IsUsernameExists(username))
            {
                MessageBox.Show("Username already exists. Please choose a different username.");
                return;
            }

            string hashedPassword = HashPassword(password);

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO users (USERNAME, PASSWORD) VALUES (@username, @password)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", hashedPassword);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Account successfully added.");
                LoadAccounts();
                username_txt.Clear();
                password_txt.Clear();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data: " + ex.Message);
            }
        }

        private void disable_btn_Click(object sender, RoutedEventArgs e)
        {
            string id = id_txt.Text;

            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("ID cannot be empty.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE users SET status = 0 WHERE ID = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Account successfully disabled.");
                LoadAccounts(); // Refresh the DataGrid
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating data: " + ex.Message);
            }
        }

        private void activate_btn_Click(object sender, RoutedEventArgs e)
        {
            string id = id_txt.Text;

            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("ID cannot be empty.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE users SET status = 1 WHERE ID = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Account successfully activated.");
                LoadAccounts(); // Refresh the DataGrid
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating data: " + ex.Message);
            }
        }


        private bool IsUsernameExists(string username)
        {
            bool exists = false;
            string query = "SELECT COUNT(*) FROM users WHERE USERNAME = @username";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);

                    try
                    {
                        connection.Open();
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        exists = count > 0;
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("An error occurred while checking the username: " + ex.Message);
                    }
                }
            }

            return exists;
        }



        private string HashPassword(string password)//Password Encryption
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }   

        private void account_data_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAccounts(); 
        }

        private void LoadAccounts() 
        {
            DataTable dataTable = new DataTable();
            string query = "SELECT ID, USERNAME, status FROM users";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                account_data.ItemsSource = dataTable.DefaultView;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("An error occurred while loading accounts: " + ex.Message);
            }
        } 

        private void account_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (account_data.SelectedItem != null)
            {
                DataRowView row = (DataRowView)account_data.SelectedItem;
                id_txt.Text = row["ID"].ToString();
                username_txt.Text = row["USERNAME"].ToString();
                password_txt.Text = ""; // Password should remain empty for security reasons
            }
        }
    }
}
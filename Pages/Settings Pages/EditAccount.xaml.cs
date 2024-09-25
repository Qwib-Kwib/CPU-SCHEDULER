using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace Info_module.Pages.Settings_Pages
{
    /// <summary>
    /// Interaction logic for EditAccount.xaml
    /// </summary>
    public partial class EditAccount : Page
    {
        public EditAccount()
        {
            InitializeComponent();
        }

        string connectionString = App.ConnectionString;

        private string HashPassword(string password)
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

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            string oldUsername = oldUsername_txt.Text;
            string oldPassword = oldPassword_txt.Text;
            string newUsername = newUsername_txt.Text;
            string newPassword = newPassword_txt.Text;

            if (string.IsNullOrWhiteSpace(oldUsername) || string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Hash the new password
                    string hashedNewPassword = HashPassword(newPassword);

                    // Update username and hashed password
                    string query = "UPDATE users SET USERNAME = @newUsername, PASSWORD = @hashedNewPassword WHERE USERNAME = @oldUsername AND PASSWORD = @oldPassword";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@oldUsername", oldUsername);
                        command.Parameters.AddWithValue("@oldPassword", oldPassword);
                        command.Parameters.AddWithValue("@newUsername", newUsername);
                        command.Parameters.AddWithValue("@hashedNewPassword", hashedNewPassword);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Account details updated successfully.");
                            // Optionally clear fields after update
                            oldUsername_txt.Text = "";
                            oldPassword_txt.Text = "";
                            newUsername_txt.Text = "";
                            newPassword_txt.Text = "";
                        }
                        else
                        {
                            MessageBox.Show("No account found with the provided old username and password.");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating account details: " + ex.Message);
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            oldUsername_txt.Text = "";
            oldPassword_txt.Text = "";
            newUsername_txt.Text = "";
            newPassword_txt.Text = "";

        }
    }
}

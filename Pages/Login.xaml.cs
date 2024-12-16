using MySql.Data.MySqlClient;
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

namespace Info_module.Pages
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        string connectionString = App.ConnectionString;
        public Login()
        {
            InitializeComponent();
        }

        private void login_btn_Click(object sender, RoutedEventArgs e)
        {
            string username = username_txt.Text;
            string password = password_txt.Password;

            string hashedPassword = HashPassword(password);
            //// Hash entered password

            ////uncomment to bypass login
            //MainMenu mainMenu = new MainMenu();
            //NavigationService.Navigate(mainMenu);
            ////this...


            //comment this to bypass login
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Password FROM users WHERE Username = @Username";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string storedPasswordHash = result.ToString();

                            // Compare hashed passwords
                            if (hashedPassword == storedPasswordHash)
                            {
                                // Passwords match, login successful
                                MainMenu mainMenu = new MainMenu();
                                NavigationService.Navigate(mainMenu);
                            }
                            else
                            {
                                MessageBox.Show("Incorrect username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("User not found.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error connecting to database: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //this....
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

        private void network_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}

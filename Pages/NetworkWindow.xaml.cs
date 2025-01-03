using Info_module.ViewModels;
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

namespace Info_module.Pages
{
    /// <summary>
    /// Interaction logic for NetworkWindow.xaml
    /// </summary>
    public partial class NetworkWindow : Window
    {
        

        public NetworkWindow()
        {

            InitializeComponent();
            var connectionViewModel = ConnectionViewModel.Instance;
            LoadTextBoxes(connectionViewModel);
            

        }

        private void LoadTextBoxes(ConnectionViewModel connectionViewModel)
        {
            server_txt.Text = connectionViewModel.Server;
            database_txt.Text = connectionViewModel.Database;
            userId_txt.Text = connectionViewModel.UserId;
            password_txt.Text = connectionViewModel.Password;
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void default_btn_Click(object sender, RoutedEventArgs e)
        {
            var connectionViewModel = ConnectionViewModel.Instance;

            // Update properties
            connectionViewModel.Server = "localhost";
            connectionViewModel.Database = "universitydb";
            connectionViewModel.UserId = "root";
            connectionViewModel.Password = "";

            LoadTextBoxes(connectionViewModel);

        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            var connectionViewModel = ConnectionViewModel.Instance;

            connectionViewModel.Server = server_txt.Text.ToString();
            connectionViewModel.Database = database_txt.Text.ToString();
            connectionViewModel.UserId = userId_txt.Text.ToString();
            connectionViewModel.Password = password_txt.Text.ToString();

            LoadTextBoxes(connectionViewModel);
        }
    }
}

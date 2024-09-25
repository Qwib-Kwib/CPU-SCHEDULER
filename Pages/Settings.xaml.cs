using Info_module.Pages.Settings_Pages;
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

namespace Info_module.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Settings";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        private void AddAccount_btn_Click(object sender, RoutedEventArgs e)
        {
            AddAccount addAccount = new AddAccount();
            SettingsFrame.Navigate(addAccount);

        }

        private void EditAccount_btn_Click(object sender, RoutedEventArgs e)
        {
            EditAccount editAccount = new EditAccount();
            SettingsFrame.Navigate(editAccount);

        }
    }
}

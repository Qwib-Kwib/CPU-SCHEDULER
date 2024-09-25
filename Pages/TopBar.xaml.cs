using Info_module.Pages.TableMenus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
    /// Interaction logic for TopBar.xaml
    /// </summary>
    public partial class TopBar : Page
    {
        public event EventHandler BackButtonClicked;
        public TopBar()
        {
            InitializeComponent();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            BackButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnSideBar_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SideFrameInstance.Navigate(new SideBar());
            MainWindow.SideFrameInstance.Visibility = Visibility.Visible;
            MainWindow.SideFrameInstance.IsHitTestVisible = true;
            MainWindow.MainFrameInstance.IsHitTestVisible = false;
        }


    }
}

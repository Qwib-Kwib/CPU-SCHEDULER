using Info_module.Pages.TableMenus;
using Info_module.Pages.TableMenus.After_College_Selection;
using Info_module.Pages.TableMenus.Assignment;
using Info_module.Pages.TableMenus.CollegeMenu;
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
    /// Interaction logic for SideBar.xaml
    /// </summary>
    public partial class SideBar : Page
    {
        public SideBar()
        {
            InitializeComponent();
        }

        private void offSidebar_Click(object sender, RoutedEventArgs e)
        {
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;
        }

        private void Department_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new CollegeMenuMain());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;
        }

        private void Building_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new BuildingMenu());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;

        }

        private void Curriculum_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new CollegeSelection("Curriculum"));
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;

        }

        private void Instructor_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new CollegeSelection("Instructor"));
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;
        }

        private void assignment_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new AssignMenuMain());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;
        }

        private void Settings_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new Settings());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;

        }

        private void Back_btn_Click(object sender, RoutedEventArgs e)
        {
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;
        }

        private void LogOut_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new Login());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;

        }

        private void ___CSV_guide_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Report_btn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MainFrameInstance.Navigate(new report());
            // Hide the SideFrame
            MainWindow.SideFrameInstance.Visibility = Visibility.Collapsed;
            MainWindow.SideFrameInstance.IsHitTestVisible = false;

            // Optionally bring the MainFrame to the foreground (if necessary)
            MainWindow.MainFrameInstance.Visibility = Visibility.Visible;
            MainWindow.MainFrameInstance.IsHitTestVisible = true;

        }
    }
}

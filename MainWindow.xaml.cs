using Info_module.Pages;
using Info_module.Pages.TableMenus;
using Info_module.Pages.TableMenus.After_College_Selection;
using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using Info_module.Pages.TableMenus.CollegeMenu;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Info_module
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Frame SideFrameInstance { get; private set; }
        public static Frame MainFrameInstance { get; private set; }


        public MainWindow()
        {
            InitializeComponent();
            CollegeMenuMain login = new CollegeMenuMain();


            MainFrame.Navigate(login);
            SideFrameInstance = SideFrame;
            MainFrameInstance = MainFrame;
        }

        private void HostWindowInteractionAttempt(object sender, MouseButtonEventArgs e)
        {

        }

        private void HostWindowInteractionAttempt(object sender, KeyEventArgs e)
        {

        }
    }
}

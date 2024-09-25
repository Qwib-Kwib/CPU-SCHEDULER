using Info_module.Pages;
using Info_module.Pages.TableMenus;
using Info_module.Pages.TableMenus.After_College_Selection;
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
            Login login = new Login();
            


            MainFrame.Navigate(login);
            SideFrameInstance = SideFrame;
            MainFrameInstance = MainFrame;
        }
    }
}

using Info_module.Pages;
using Info_module.Pages.TableMenus;
using Info_module.Pages.TableMenus.After_College_Selection;
using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using Info_module.Pages.TableMenus.After_College_Selection.InstructorMenu;
using Info_module.Pages.TableMenus.AssignmentMenu;
using Info_module.Pages.TableMenus.BlockSectionMenu;
using Info_module.Pages.TableMenus.Buildings;
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

        private double aspectRatio;



        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new ScheduleMenuMain());
            SideFrameInstance = SideFrame;
            MainFrameInstance = MainFrame;

            aspectRatio = 16.0 / 9.0;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Get the new width and height
            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            // Calculate the new height based on the aspect ratio
            if (newWidth / newHeight > aspectRatio)
            {
                // Width is too wide, adjust height
                newWidth = newHeight * aspectRatio;
            }
            else
            {
                // Height is too tall, adjust width
                newHeight = newWidth / aspectRatio;
            }

            // Set the new size
            this.Width = newWidth;
            this.Height = newHeight;

        }
    }
}

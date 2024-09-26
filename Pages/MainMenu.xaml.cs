using Info_module.Pages.TableMenus;
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
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Page
    {
        public MainMenu()
        {
            InitializeComponent();

            var app = (App)Application.Current;
            app.LoadUI(TopBarFrame, "Main Menu", TopBar_BackButtonClicked);
            TopBar topBar = new TopBar();
            topBar.HideButton();
        }
        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new MainMenu());
        }

        private void NavigatefromMainMenu(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }

        private void btnMenuDepartment_Click(object sender, RoutedEventArgs e)
        {
            DepartmentMenu departmentMenu = new DepartmentMenu();
            NavigationService.Navigate(departmentMenu);

        }

        private void btnMenuBuilding_Click(object sender, RoutedEventArgs e)
        {
            BuildingMap buildingMap = new BuildingMap();
            NavigationService.Navigate(buildingMap);
            //BuildingMenu buildingMenu = new BuildingMenu();
            //NavigationService.Navigate(buildingMenu);
        }

        private void btnMenuCurriculum_Click(object sender, RoutedEventArgs e)
        {
            NavigatefromMainMenu("Curriculum");
        }

        private void btnMenuInstructor_Click(object sender, RoutedEventArgs e)
        {
            NavigatefromMainMenu("Instructor");

        }

        private void Assignment_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Assignment()) ;
        }
    }
}

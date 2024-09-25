using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

namespace Info_module.Pages.TableMenus
{
	/// <summary>
	/// Interaction logic for BuildingMap.xaml
	/// </summary>
	public partial class BuildingMap : Page
	{
		public BuildingMap()
		{
			InitializeComponent();
			TopBar topBar = new TopBar();
			topBar.txtPageTitle.Text = "Building Map";
			topBar.Visibility = Visibility.Visible;
			topBar.BackButtonClicked += TopBar_BackButtonClicked;
			TopBarFrame.Navigate(topBar);
		}

		private void TopBar_BackButtonClicked(object sender, EventArgs e)
		{
			var mainWindow = (MainWindow)Application.Current.MainWindow;
			mainWindow.MainFrame.Navigate(new MainMenu());
		}

		private void mt_bldg_click(object sender, RoutedEventArgs e)
		{

			Button clickedButton = sender as Button;
			if (clickedButton != null && clickedButton.Tag != null)
			{
				// XAML Tag to int function, which is then used to open a specific set of details in another page
				int buildingId = Convert.ToInt32(clickedButton.Tag);
				CSVRooms rooms = new CSVRooms(buildingId);
				NavigationService.Navigate(rooms);

			}

			else
			{
				MessageBox.Show("Error");
			}

		}

		private void ov_bldg_click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = sender as Button;
			if (clickedButton != null && clickedButton.Tag != null)
			{
				// XAML Tag to int function, which is then used to open a specific set of details in another page
				int buildingId = Convert.ToInt32(clickedButton.Tag);
				CSVRooms rooms = new CSVRooms(buildingId);
				NavigationService.Navigate(rooms);

			}

			else
			{
				MessageBox.Show("Error");
			}

		}

		private void nv_bldg_click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = sender as Button;
			if (clickedButton != null && clickedButton.Tag != null)
			{
				// XAML Tag to int function, which is then used to open a specific set of details in another page
				int buildingId = Convert.ToInt32(clickedButton.Tag);
				CSVRooms rooms = new CSVRooms(buildingId);
				NavigationService.Navigate(rooms);

			}

			else
			{
				MessageBox.Show("Error");
			}

		}


		private void en_bldg_click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = sender as Button;
			if (clickedButton != null && clickedButton.Tag != null)
			{

				// XAML Tag to int function, which is then used to open a specific set of details in another page
				int buildingId = Convert.ToInt32(clickedButton.Tag);
				CSVRooms rooms = new CSVRooms(buildingId);
				NavigationService.Navigate(rooms);

			}

			else
			{
				MessageBox.Show("Error");
			}

		}

        private void buildingMenu_btn_Click(object sender, RoutedEventArgs e)
        {
			NavigationService.Navigate(new BuildingMenu());

        }
    }
}

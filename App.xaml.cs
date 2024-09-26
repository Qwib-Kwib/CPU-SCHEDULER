using Info_module.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Info_module
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>//
    public partial class App : Application
    {
        //Kim's server
        //public static readonly string ConnectionString = @"Server=26.182.137.35;Database=universitydb;User ID=test;Password=;";

        //local
        public static readonly string ConnectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        public static bool IsTextNumeric(string str)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(str, "[^0-9]");
        }

        public void LoadUI(Frame topBarFrame, string pageTitle, EventHandler backButtonClickedEvent)
        {
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = pageTitle;
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += backButtonClickedEvent;
            topBarFrame.Navigate(topBar);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Info_module.ViewModels
{

    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private static ConnectionViewModel _instance;

        // Private constructor to prevent instantiation
        private ConnectionViewModel()
        {
            // Initialize default values
            Server = "localhost";
            Database = "universitydb";
            UserId = "root";
            Password = "";
        }

        public static ConnectionViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConnectionViewModel();
                }
                return _instance;
            }
        }

        // Properties
        private string _server;
        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                OnPropertyChanged(nameof(Server));
                UpdateConnectionString();
            }
        }

        private string _database;
        public string Database
        {
            get => _database;
            set
            {
                _database = value;
                OnPropertyChanged(nameof(Database));
                UpdateConnectionString();
            }
        }

        private string _userId;
        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
                UpdateConnectionString();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                UpdateConnectionString();
            }
        }

        private string _connectionString;
        public string ConnectionString
        {
            get => _connectionString;
            private set
            {
                _connectionString = value;
                OnPropertyChanged(nameof(ConnectionString));
            }
        }

        private void UpdateConnectionString()
        {
            ConnectionString = $"Server={Server};Database={Database};UserID={UserId};Password={Password};";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using Katran.Pages;
using Katran.ViewModels;
using Katran.Views;
using KatranClassLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Katran.Models
{
    public class SettingsTab : INotifyPropertyChanged
    {
        private Visibility tabVisibility;
        public Visibility TabVisibility
        {
            get { return tabVisibility; }
            set { tabVisibility = value; OnPropertyChanged(); }
        }

        private MainPageViewModel mainPageViewModel;
        public MainPageViewModel MainPageViewModel
        {
            get { return mainPageViewModel; }
            set { mainPageViewModel = value; }
        }

        private bool isDarkTheme;

        public bool IsDarkTheme
        {
            get { return isDarkTheme; }
            set
            {
                isDarkTheme = value;
                OnPropertyChanged();

                if (isDarkTheme)
                {
                    App.Theme = Settings.Theme.Dark;
                }
                else
                {
                    App.Theme = Settings.Theme.Light;
                }

                if (MainPageViewModel != null)
                {
                    if (isDarkTheme)
                    {
                        MainPageViewModel.MainViewModel.CurrentSettings.CurrentTheme = Settings.Theme.Dark;
                    }
                    else
                    {
                        MainPageViewModel.MainViewModel.CurrentSettings.CurrentTheme = Settings.Theme.Light;
                    }

                    Settings.SaveSettings(MainPageViewModel.MainViewModel.CurrentSettings);
                }
            }
        }


        public SettingsTab()
        {
            MainPageViewModel = null;
            TabVisibility = Visibility.Collapsed;
            IsDarkTheme = true;
        }

        public SettingsTab(MainPageViewModel mainPageViewModel)
        {
            MainPageViewModel = mainPageViewModel;
            TabVisibility = Visibility.Collapsed;

            IsDarkTheme = MainPageViewModel.MainViewModel.CurrentSettings.CurrentTheme == Settings.Theme.Dark;
        }

        public ICommand LogOut
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    DialogWindow dialog = new DialogWindow((string)Application.Current.FindResource("l_LogOutTooltip"));

                    if (dialog.ShowDialog().Value)
                    {
                        MainPageViewModel.clientListener.CloseConnection();
                        File.Delete(RegistrationTemplate.AuthTokenFileName);
                        MainPageViewModel.MainViewModel.CurrentPage = new AuhtorizationPage(MainPageViewModel.MainViewModel);
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

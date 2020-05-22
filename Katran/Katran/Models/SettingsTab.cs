using Katran.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public SettingsTab()
        {
            MainPageViewModel = null;
            TabVisibility = Visibility.Collapsed;
        }

        public SettingsTab(MainPageViewModel mainPageViewModel)
        {
            MainPageViewModel = mainPageViewModel;
            TabVisibility = Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

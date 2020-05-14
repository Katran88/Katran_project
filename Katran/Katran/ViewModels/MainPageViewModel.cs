using Katran.Models;
using Katran.Pages;
using Katran.UserControlls;
using Katran.Views;
using KatranClassLibrary;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private Visibility createChatTabVisibility;
        public Visibility CreateChatTabVisibility
        {
            get { return createChatTabVisibility; }
            set { createChatTabVisibility = value; OnPropertyChanged(); }
        }

        private Visibility settingsTabVisibility;
        public Visibility SettingsTabVisibility
        {
            get { return settingsTabVisibility; }
            set { settingsTabVisibility = value; OnPropertyChanged(); }
        }

        internal static ClientListener clientListener;
        private MainViewModel mainViewModel;
        public MainViewModel MainViewModel
        {
            get { return mainViewModel; }
        }

        MainPage mainPage;
        private ContactsTab contactsTab;

        public ContactsTab ContactsTab
        {
            get { return contactsTab; }
            set { contactsTab = value; OnPropertyChanged(); }
        }


        public MainPageViewModel()
        {
            this.mainViewModel = null;
            this.mainPage = null;
            ContactsTab = new ContactsTab();


            CreateChatTabVisibility = Visibility.Collapsed;
            settingsTabVisibility = Visibility.Collapsed;
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;

            ContactsTab = new ContactsTab(this);

            CreateChatTabVisibility = Visibility.Collapsed;
            settingsTabVisibility = Visibility.Collapsed;

            MainPageViewModel.clientListener = new ClientListener(this);
        }

        public ICommand ContactsSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (ContactsTab.TabVisibility != Visibility.Visible)
                    {
                        ContactsTab.TabVisibility = Visibility.Visible;
                        CreateChatTabVisibility = Visibility.Collapsed;
                        SettingsTabVisibility = Visibility.Collapsed;

                        Task.Factory.StartNew(() =>
                        {
                            Client.ServerRequest(new RRTemplate(RRType.RefreshContacts, new RefreshContactsTemplate(mainViewModel.UserInfo.Info.Id, null)));
                        });
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

using Katran.Models;
using Katran.Pages;
using Katran.UserControlls;
using KatranClassLibrary;
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

        #region ContactsTab

        private Visibility contactsAndChatsTabVisibility;
        public Visibility ContactsAndChatsTabVisibility
        {
            get { return contactsAndChatsTabVisibility; }
            set { contactsAndChatsTabVisibility = value; OnPropertyChanged(); }
        }

        private Visibility contactsBorderVisibility;
        public Visibility ContactsBorderVisibility
        {
            get { return contactsBorderVisibility; }
            set { contactsBorderVisibility = value; OnPropertyChanged(); }
        }

        private Visibility noUserContactsVisibility;
        public Visibility NoUserContactsVisibility
        {
            get { return noUserContactsVisibility; }
            set { noUserContactsVisibility = value; OnPropertyChanged(); }
        }

        ContactUI selectedContact; 
        public ContactUI SelectedContact
        {
            get { return selectedContact; }
            set { selectedContact = value; OnPropertyChanged(); }
        }

        ObservableCollection<ContactUI> contacts;
        public ObservableCollection<ContactUI> Contacts
        {
            get { return contacts; }
            set { contacts = value; OnPropertyChanged(); }
        }

        ObservableCollection<ContactUI> filteredNoUserContacts;
        public ObservableCollection<ContactUI> FilteredNoUserContacts
        {
            get { return filteredNoUserContacts; }
            set { filteredNoUserContacts = value; OnPropertyChanged(); }
        }

        private string searchTextField;
        public string SearchTextField
        {
            get { return searchTextField; }
            set 
            {
                string currentValue = Regex.Replace((string)value, @"[а-я|А-Я]+", "");
                searchTextField = Regex.Match(currentValue, @"[\w|@|\.]+").Value;
                OnPropertyChanged();

                if (!isSearchOutsideContacts)
                {
                    if (searchTextField.Length != 0)
                    {
                        ContactsBorderVisibility = Visibility.Visible;
                    }
                    else
                    {
                        ContactsBorderVisibility = Visibility.Collapsed;

                        foreach (ContactUI item in Contacts)
                        {
                            item.Visibility = Visibility.Visible;
                        }

                    }
                }
                else
                {
                    if (value.Length == 0)
                    {
                        NoUserContactsVisibility = Visibility.Collapsed;
                        ContactsBorderVisibility = Visibility.Collapsed;

                        foreach (ContactUI item in Contacts)
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        isSearchOutsideContacts = false;
                    }

                }
            }
        }

        public ICommand ContactsSearchButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (!isSearchOutsideContacts)
                    {
                        foreach (ContactUI item in Contacts)
                        {
                            if (!Regex.IsMatch(item.ContactUsername, searchTextField, RegexOptions.IgnoreCase))
                            {
                                item.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        NoUserContactsVisibility = Visibility.Visible;
                        Task.Factory.StartNew(() => 
                        { 
                            Client.ServerRequest(new RRTemplate(RRType.SearchOutContacts, new SearchOutContactsTemplate(mainViewModel.UserInfo.Info.Id, searchTextField, null))); 
                        });
                    }
                    

                }, (obj) => 
                {
                    return SearchTextField.Length != 0; 
                } );
            }
        }

        bool isSearchOutsideContacts;

        public ICommand SearchOutContacts
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    foreach (ContactUI item in Contacts)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    ContactsBorderVisibility = Visibility.Collapsed;
                    NoUserContactsVisibility = Visibility.Visible;
                    isSearchOutsideContacts = true;

                    Task.Factory.StartNew(() =>
                    {
                        Client.ServerRequest(new RRTemplate(RRType.SearchOutContacts, new SearchOutContactsTemplate(mainViewModel.UserInfo.Info.Id, searchTextField, null)));
                    });
                });
            }
        }

        public ICommand ContactsAddButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    MessageBox.Show("Add");
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        #endregion

        public MainPageViewModel()
        {
            this.mainViewModel = null;
            this.mainPage = null;
            Contacts = new ObservableCollection<ContactUI>();
            FilteredNoUserContacts = new ObservableCollection<ContactUI>();
            isSearchOutsideContacts = false;
            NoUserContactsVisibility = Visibility.Collapsed;

            ContactsAndChatsTabVisibility = Visibility.Collapsed;
            CreateChatTabVisibility = Visibility.Collapsed;
            settingsTabVisibility = Visibility.Collapsed;

            SearchTextField = "";
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;
            Contacts = new ObservableCollection<ContactUI>();
            FilteredNoUserContacts = new ObservableCollection<ContactUI>();
            SearchTextField = "";
            isSearchOutsideContacts = false;
            NoUserContactsVisibility = Visibility.Collapsed;

            ContactsAndChatsTabVisibility = Visibility.Collapsed;
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
                    if (ContactsAndChatsTabVisibility != Visibility.Visible)
                    {
                        ContactsAndChatsTabVisibility = Visibility.Visible;
                        CreateChatTabVisibility = Visibility.Collapsed;
                        settingsTabVisibility = Visibility.Collapsed;

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

using Katran.Pages;
using Katran.UserControlls;
using Katran.ViewModels;
using Katran.Views;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Katran.Models
{
    public class ContactsTab : INotifyPropertyChanged
    {
        private Visibility tabVisibility;
        public Visibility TabVisibility
        {
            get { return tabVisibility; }
            set { tabVisibility = value; OnPropertyChanged(); }
        }

        private Visibility contactsBorderVisibility;
        public Visibility ContactsBorderVisibility
        {
            get { return contactsBorderVisibility; }
            set { contactsBorderVisibility = value; OnPropertyChanged(); }
        }

        private Visibility removeContact_ButtonVisibility;
        public Visibility RemoveContact_ButtonVisibility
        {
            get { return removeContact_ButtonVisibility; }
            set { removeContact_ButtonVisibility = value; OnPropertyChanged(); }
        }

        private Visibility addContact_ButtonVisibility;
        public Visibility AddContact_ButtonVisibility
        {
            get { return addContact_ButtonVisibility; }
            set { addContact_ButtonVisibility = value; OnPropertyChanged(); }
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
            set
            {
                selectedContact = value;
                OnPropertyChanged();

                if (selectedContact != null)
                {
                    mainPageViewModel.CurrentChatMessages = selectedContact.ContactMessages;

                    if (!IsSearchOutsideContacts)
                    {
                        RemoveContact_ButtonVisibility = Visibility.Visible;
                        AddContact_ButtonVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        RemoveContact_ButtonVisibility = Visibility.Hidden;
                    }
                }
                else
                {
                    mainPageViewModel.CurrentChatMessages.Clear();
                    RemoveContact_ButtonVisibility = Visibility.Hidden;
                }
            }
        }

        ContactUI selectedNoUserContact;
        public ContactUI SelectedNoUserContact
        {
            get { return selectedNoUserContact; }
            set
            {
                selectedNoUserContact = value;
                OnPropertyChanged();

                if (selectedNoUserContact != null)
                {
                    if (IsSearchOutsideContacts)
                    {
                        RemoveContact_ButtonVisibility = Visibility.Hidden;
                        AddContact_ButtonVisibility = Visibility.Visible;
                    }
                    else
                    {
                        AddContact_ButtonVisibility = Visibility.Hidden;
                    }
                }
                else
                {
                    AddContact_ButtonVisibility = Visibility.Hidden;
                }
            }
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

                if (!IsSearchOutsideContacts)
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
                        IsSearchOutsideContacts = false;
                    }

                }

                if (IsSearchOutsideContacts)
                {
                    RemoveContact_ButtonVisibility = Visibility.Hidden;
                }
                else
                {
                    if (selectedContact != null)
                    {
                        RemoveContact_ButtonVisibility = Visibility.Visible;
                    }
                    else
                    {
                        RemoveContact_ButtonVisibility = Visibility.Hidden;
                    }
                }

                if (searchTextField.Length == 0)
                {
                    if (selectedContact != null)
                    {
                        RemoveContact_ButtonVisibility = Visibility.Visible;
                    }
                    else
                    {
                        RemoveContact_ButtonVisibility = Visibility.Hidden;
                    }

                    AddContact_ButtonVisibility = Visibility.Hidden;
                }

            }
        }

        public ICommand ContactsSearchButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (!IsSearchOutsideContacts)
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
                            Client.ServerRequest(new RRTemplate(RRType.SearchOutContacts, new SearchOutContactsTemplate(MainPageViewModel.MainViewModel.UserInfo.Info.Id, searchTextField, null)));
                        });
                    }


                }, (obj) =>
                {
                    return SearchTextField.Length != 0;
                });
            }
        }

        public bool IsSearchOutsideContacts;

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
                    RemoveContact_ButtonVisibility = Visibility.Hidden;
                    IsSearchOutsideContacts = true;

                    Task.Factory.StartNew(() =>
                    {
                        Client.ServerRequest(new RRTemplate(RRType.SearchOutContacts, new SearchOutContactsTemplate(MainPageViewModel.MainViewModel.UserInfo.Info.Id, searchTextField, null)));
                    });
                });
            }
        }

        public ICommand ContactAddButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    DialogWindow dialog = new DialogWindow((string)Application.Current.FindResource("l_AddContact"));

                    if (dialog.ShowDialog().Value)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Client.ServerRequest(new RRTemplate(RRType.AddContact, new AddRemoveContactTemplate(MainPageViewModel.MainViewModel.UserInfo.Info.Id, selectedNoUserContact.ContactID, -1)));
                        });
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand ContactRemoveButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    DialogWindow dialog = new DialogWindow((string)Application.Current.FindResource("l_RemoveContact"));

                    if (dialog.ShowDialog() == true)
                    {
                        Client.ServerRequest(new RRTemplate(RRType.RemoveContact, new AddRemoveContactTemplate(MainPageViewModel.MainViewModel.UserInfo.Info.Id, selectedContact.ContactID, selectedContact.ChatId)));
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        private MainPageViewModel mainPageViewModel;
        public MainPageViewModel MainPageViewModel
        {
            get { return mainPageViewModel; }
            set { mainPageViewModel = value; }
        }


        public ContactsTab()
        {
            MainPageViewModel = null;
            Contacts = new ObservableCollection<ContactUI>();
            FilteredNoUserContacts = new ObservableCollection<ContactUI>();
            IsSearchOutsideContacts = false;
            RemoveContact_ButtonVisibility = addContact_ButtonVisibility = Visibility.Hidden;
            NoUserContactsVisibility = Visibility.Collapsed;

            TabVisibility = Visibility.Collapsed;
            SearchTextField = "";
        }

        public ContactsTab(MainPageViewModel mainPageViewModel)
        {
            MainPageViewModel = mainPageViewModel;
            Contacts = new ObservableCollection<ContactUI>();
            FilteredNoUserContacts = new ObservableCollection<ContactUI>();
            SearchTextField = "";
            IsSearchOutsideContacts = false;
            RemoveContact_ButtonVisibility = addContact_ButtonVisibility = Visibility.Hidden;
            NoUserContactsVisibility = Visibility.Collapsed;

            TabVisibility = Visibility.Collapsed;
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

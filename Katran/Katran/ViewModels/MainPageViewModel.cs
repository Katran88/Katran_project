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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        internal static ClientListener clientListener;
        private MainViewModel mainViewModel;
        public MainViewModel MainViewModel
        {
            get { return mainViewModel; }
        }


        MainPage mainPage;

        #region ContactsTab

        ObservableCollection<ContactUI> contacts;
        public ObservableCollection<ContactUI> Contacts
        {
            get { return contacts; }
            set { contacts = value; OnPropertyChanged(); }
        }

        private string searchTextField;
        public string SearchTextField
        {
            get { return searchTextField; }
            set { searchTextField = value; OnPropertyChanged(); }
        }

        public ICommand ContactsSearchButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    MessageBox.Show("Search");
                }, (obj) => 
                {
                    return SearchTextField.Length != 0; 
                } );
            }
        }

        public ICommand SearchOutContacts
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    MessageBox.Show("Search Out Contacts");
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

        private ContentPresenter menuContentPresenter;
        public ContentPresenter MenuContentPresenter
        {
            get { return menuContentPresenter; }
            set { menuContentPresenter = value; OnPropertyChanged(); }
        }

        private ContentPresenter messagesContentPresenter;
        public ContentPresenter MessagesContentPresenter
        {
            get { return messagesContentPresenter; }
            set { messagesContentPresenter = value; OnPropertyChanged(); }
        }

        public MainPageViewModel()
        {
            this.mainViewModel = null;
            this.mainPage = null;
            MenuContentPresenter = new ContentPresenter();
            MessagesContentPresenter = new ContentPresenter();
            Contacts = new ObservableCollection<ContactUI>();
            SearchTextField = "";
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;
            MenuContentPresenter = new ContentPresenter();
            MessagesContentPresenter = new ContentPresenter();
            Contacts = new ObservableCollection<ContactUI>();
            SearchTextField = "";

            MainPageViewModel.clientListener = new ClientListener(this);
        }

        public ICommand ContactsSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (MenuContentPresenter.Content == null || ((FrameworkElement)MenuContentPresenter.Content).Tag.ToString().CompareTo("contactsPanel") != 0)
                    {
                        Grid contactsPanel = new Grid();
                        contactsPanel.Tag = "contactsPanel";
                        contactsPanel.RowDefinitions.Add(new RowDefinition()); // for ContactsSeracher
                        contactsPanel.RowDefinitions.Add(new RowDefinition()); // for Contacts
                        contactsPanel.RowDefinitions.Add(new RowDefinition()); // for ContactsBorder
                        contactsPanel.RowDefinitions.Add(new RowDefinition()); // for OutContacts
                        contactsPanel.RowDefinitions[0].Height = GridLength.Auto;
                        contactsPanel.RowDefinitions[1].Height = GridLength.Auto;
                        contactsPanel.RowDefinitions[2].Height = GridLength.Auto;

                        contactsPanel.Children.Add(new ContactsSearcher(SearchTextField, ContactsSearchButton));

                        contactsPanel.Children.Add(new ListView());
                        ((ListView)contactsPanel.Children[1]).ItemsSource = Contacts;

                        Grid.SetRow(contactsPanel.Children[0], 0);
                        Grid.SetRow(contactsPanel.Children[1], 1);

                        MenuContentPresenter.Content = contactsPanel;

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

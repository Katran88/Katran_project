using Katran.UserControlls;
using Katran.ViewModels;
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
using System.Windows.Input;

namespace Katran.Models
{
    public class CreateConversationTab : INotifyPropertyChanged
    {
        private Visibility tabVisibility;
        public Visibility TabVisibility
        {
            get { return tabVisibility; }
            set { tabVisibility = value; OnPropertyChanged(); }
        }

        private Visibility addContact_ButtonVisibility;
        public Visibility AddContact_ButtonVisibility
        {
            get { return addContact_ButtonVisibility; }
            set { addContact_ButtonVisibility = value; OnPropertyChanged(); }
        }

        private Visibility removeContact_ButtonVisibility;
        public Visibility RemoveContact_ButtonVisibility
        {
            get { return removeContact_ButtonVisibility; }
            set { removeContact_ButtonVisibility = value; OnPropertyChanged(); }
        }

        private MainPageViewModel mainPageViewModel;
        public MainPageViewModel MainPageViewModel
        {
            get { return mainPageViewModel; }
            set { mainPageViewModel = value; }
        }


        private string conversationTitle;
        public string ConversationTitle
        {
            get { return conversationTitle; }
            set { conversationTitle = value; OnPropertyChanged(); }
        }

        private string searchTextField;
        public string SearchTextField
        {
            get { return searchTextField; }
            set 
            { 
                searchTextField = value; 
                OnPropertyChanged();

                if (searchTextField.Length == 0 && Contacts != null)
                {
                    foreach (ContactUI c in Contacts)
                    {
                        c.Visibility = Visibility.Visible;
                    }
                    SelectedContact = null;
                }
            }
        }

        private string membersCount;
        public string MembersCount
        {
            get { return membersCount; }
            set
            {
                membersCount = (string)Application.Current.FindResource("l_Members") + ' ' + value;
                OnPropertyChanged();
            }
        }

        byte[] conversationAvatar;
        string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; OnPropertyChanged(); }
        }

        List<Contact> convMembers;
        ObservableCollection<ContactUI> contacts;
        public ObservableCollection<ContactUI> Contacts
        {
            get { return contacts; }
            set { contacts = value; OnPropertyChanged(); }
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
                    if (convMembers.Find((x) => x.UserId == selectedContact.ContactID) != null)
                    {
                        RemoveContact_ButtonVisibility = Visibility.Visible;
                        AddContact_ButtonVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        AddContact_ButtonVisibility = Visibility.Visible;
                        RemoveContact_ButtonVisibility = Visibility.Hidden;
                    }
                    
                }
                else
                {
                    AddContact_ButtonVisibility = Visibility.Hidden;
                    RemoveContact_ButtonVisibility = Visibility.Hidden;
                }
            }
        }

        public CreateConversationTab()
        {
            MainPageViewModel = null;
            ConversationTitle = "";
            FileName = "";
            SearchTextField = "";
            MembersCount = "0";
            convMembers = new List<Contact>();
            Contacts = new ObservableCollection<ContactUI>();
            AddContact_ButtonVisibility = TabVisibility = Visibility.Collapsed;
            RemoveContact_ButtonVisibility = AddContact_ButtonVisibility = Visibility.Hidden;
        }

        public CreateConversationTab(MainPageViewModel mainPageViewModel)
        {
            MainPageViewModel = mainPageViewModel;
            ConversationTitle = "";
            FileName = "";
            SearchTextField = "";
            convMembers = new List<Contact>();
            Contacts = new ObservableCollection<ContactUI>();
            AddContact_ButtonVisibility = TabVisibility = Visibility.Collapsed;
            RemoveContact_ButtonVisibility = AddContact_ButtonVisibility = Visibility.Hidden;

            MembersCount = "1";
            convMembers.Add(new Contact(mainPageViewModel.MainViewModel.UserInfo.Info.Id, null, "", Status.Offline, -1, null, ContactType.Chat)); //сразу добавляем себя в чат
        }

        public ICommand ContactSearchButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    SelectedContact = null;
                    foreach (ContactUI c in Contacts)
                    {
                        if (!Regex.IsMatch(c.ContactUsername, searchTextField, RegexOptions.IgnoreCase))
                        {
                            c.Visibility = Visibility.Collapsed;
                        }
                    }

                }, (obj) =>
                {
                    return SearchTextField.Length != 0;
                });
            }
        }

        public ICommand AddContactToConv
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    convMembers.Add(new Contact(SelectedContact.ContactID, null, "", Status.Offline, -1, null, ContactType.Chat)); //нужен только id для добавления
                    MembersCount = convMembers.Count.ToString();
                    SelectedContact = null;
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand RemoveContactFromConv
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    convMembers.Remove(convMembers.Find((x) => x.UserId == SelectedContact.ContactID));
                    MembersCount = convMembers.Count.ToString();
                    SelectedContact = null;

                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand CreateConv
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (ConversationTitle.Length == 0)
                    {
                        mainPageViewModel.MainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotConvTitle);
                    }
                    else
                    {
                        Client.ServerRequest(new RRTemplate(RRType.CreateConv, new CreateConvTemplate(-1, ConversationTitle, conversationAvatar, convMembers)));
                        mainPageViewModel.MainViewModel.NotifyUserByRowState(RowStateResourcesName.l_convCreated);
                        ResetFields();
                    }

                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        void ResetFields()
        {
            SelectedContact = null;
            SearchTextField = "";
            ConversationTitle = "";
            FileName = "";
            conversationAvatar = null;
            MembersCount = "1";
            convMembers.Clear();
        }

        public ICommand SelectImage
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                    dlg.DefaultExt = ".png";
                    dlg.Filter = "Images (*.jpeg, *.jpg, *.png)|*.jpeg;*.jpg;*.png";

                    bool? result = dlg.ShowDialog();

                    if (result.HasValue && result.Value == true)
                    {
                        Image imageIn = new Bitmap(dlg.FileName);
                        using (var ms = new MemoryStream())
                        {
                            imageIn.Save(ms, imageIn.RawFormat);

                            conversationAvatar = ms.GetBuffer();
                        }

                        string[] splitedStr = dlg.FileName.Split(@"\\".ToCharArray());
                        FileName = splitedStr[splitedStr.Length - 1];
                    }
                    else
                    {
                        conversationAvatar = null;
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

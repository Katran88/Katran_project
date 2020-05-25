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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private Visibility adminTabButtonVisibility;
        public Visibility AdminTabButtonVisibility
        {
            get { return adminTabButtonVisibility; }
            set { adminTabButtonVisibility = value; OnPropertyChanged(); }
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

        private CreateConversationTab conversationTab;
        public CreateConversationTab CreateConversationTab
        {
            get { return conversationTab; }
            set { conversationTab = value; OnPropertyChanged(); }
        }

        private AdminTab adminTab;
        public AdminTab AdminTab
        {
            get { return adminTab; }
            set { adminTab = value; OnPropertyChanged(); }
        }

        private SettingsTab settingsTab;
        public SettingsTab SettingsTab
        {
            get { return settingsTab; }
            set { settingsTab = value; OnPropertyChanged(); }
        }

        private string messageText;
        public string MessageText
        {
            get { return messageText; }
            set { messageText = value; OnPropertyChanged(); }
        }


        public MainPageViewModel()
        {
            AdminTabButtonVisibility = Visibility.Collapsed;

            this.mainViewModel = null;
            this.mainPage = null;

            ContactsTab = new ContactsTab();
            CreateConversationTab = new CreateConversationTab();
            AdminTab = new AdminTab();
            SettingsTab = new SettingsTab();

            MessageText = "";
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;
            MessageText = "";

            AdminTabButtonVisibility = MainViewModel.UserInfo.Info.LawStatus == LawStatus.Admin ? Visibility.Visible : Visibility.Collapsed;

            ContactsTab = new ContactsTab(this);
            CreateConversationTab = new CreateConversationTab(this);
            AdminTab = new AdminTab(this);
            SettingsTab = new SettingsTab(this);

            MainPageViewModel.clientListener = new ClientListener(this);

            ContactsTab.TabVisibility = Visibility.Visible;

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Client.ServerRequest(new RRTemplate(RRType.RefreshContacts, new RefreshContactsTemplate(mainViewModel.UserInfo.Info.Id, null)));
            });
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
                        CreateConversationTab.TabVisibility = Visibility.Collapsed;
                        AdminTab.TabVisibility = Visibility.Collapsed;
                        SettingsTab.TabVisibility = Visibility.Collapsed;

                        if (ContactsTab.Contacts.Count == 0)
                        {
                           Task.Factory.StartNew(() =>
                           {
                                Client.ServerRequest(new RRTemplate(RRType.RefreshContacts, new RefreshContactsTemplate(mainViewModel.UserInfo.Info.Id, null)));
                           }); 
                        }
                        
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand CreateСonversationSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (CreateConversationTab.TabVisibility != Visibility.Visible)
                    {
                        CreateConversationTab.TabVisibility = Visibility.Visible;
                        ContactsTab.TabVisibility = Visibility.Collapsed;
                        AdminTab.TabVisibility = Visibility.Collapsed;
                        SettingsTab.TabVisibility = Visibility.Collapsed;

                        CreateConversationTab.Contacts.Clear();
                        foreach (ContactUI c in ContactsTab.Contacts)
                        {
                            if(c.ContactType == ContactType.Chat)
                            CreateConversationTab.Contacts.Add(new ContactUI(c.ContactUsername,
                                                                             c.ContactLastMessage,
                                                                             c.ContactAvatar,
                                                                             c.ContactStatus,
                                                                             c.ContactID,
                                                                             c.ChatId,
                                                                             null,
                                                                             false));
                        }
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand AdminTabSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (AdminTab.TabVisibility != Visibility.Visible)
                    {
                        AdminTab.TabVisibility = Visibility.Visible;
                        CreateConversationTab.TabVisibility = Visibility.Collapsed;
                        ContactsTab.TabVisibility = Visibility.Collapsed;
                        SettingsTab.TabVisibility = Visibility.Collapsed;
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand SettingsTabSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (SettingsTab.TabVisibility != Visibility.Visible)
                    {
                        SettingsTab.TabVisibility = Visibility.Visible;
                        AdminTab.TabVisibility = Visibility.Collapsed;
                        CreateConversationTab.TabVisibility = Visibility.Collapsed;
                        ContactsTab.TabVisibility = Visibility.Collapsed;
                    }
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        private ObservableCollection<MessageUI> currentChatMessages;
        public ObservableCollection<MessageUI> CurrentChatMessages
        {
            get { return currentChatMessages; }
            set { currentChatMessages = value; OnPropertyChanged(); }
        }


        public ICommand SendMessage
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        Message message = new Message(-1, 
                                                      mainViewModel.UserInfo.Info.Id,
                                                      mainViewModel.UserInfo.Info.App_name,
                                                      Encoding.UTF8.GetBytes(messageText),
                                                      MessageType.Text,
                                                      "", "",
                                                      DateTime.Now,
                                                      MessageState.Unsended);

                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            contactsTab.SelectedContact.ContactMessages.Add(new MessageUI(this,
                                                                                          true,
                                                                                          mainViewModel.UserInfo.Avatar,
                                                                                          mainViewModel.UserInfo.Info.Status,
                                                                                          message.MessageType,
                                                                                          message.MessageState,
                                                                                          message.Time,
                                                                                          message.MessageBody,
                                                                                          message.SenderName,
                                                                                          contactsTab.SelectedContact.ChatId,
                                                                                          -1,
                                                                                          message.SenderID,
                                                                                          "", ""));
                        }));

                        Client.ServerRequest(new RRTemplate(RRType.SendMessage, new SendMessageTemplate(contactsTab.SelectedContact.ChatId, message)));
                        MessageText = "";
                    });
                }, obj => messageText.Length != 0 && contactsTab.SelectedContact != null);
            }
        }

        public ICommand SendFile
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                    bool? result = dlg.ShowDialog();

                    Task.Factory.StartNew(() =>
                    {
                        if (result.HasValue && result.Value == true)
                        {
                            Message message = new Message(-1,
                                                          mainViewModel.UserInfo.Info.Id,
                                                          mainViewModel.UserInfo.Info.App_name,
                                                          File.ReadAllBytes(dlg.FileName),
                                                          MessageType.File,
                                                          dlg.SafeFileName, "",
                                                          DateTime.Now,
                                                          MessageState.Unsended);

                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                contactsTab.SelectedContact.ContactMessages.Add(new MessageUI(this,
                                                                                              true,
                                                                                              mainViewModel.UserInfo.Avatar,
                                                                                              mainViewModel.UserInfo.Info.Status,
                                                                                              message.MessageType,
                                                                                              message.MessageState,
                                                                                              message.Time,
                                                                                              new byte[1],
                                                                                              message.SenderName,
                                                                                              -1,
                                                                                              -1,
                                                                                              message.SenderID,
                                                                                              message.FileName, ""));
                            }));
                            mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_upload);
                            Client.ServerRequest(new RRTemplate(RRType.SendMessage, new SendMessageTemplate(contactsTab.SelectedContact.ChatId, message)));
                        }
                    });
                }, obj => contactsTab.SelectedContact != null);
            }
        }

        public void DownloadFile(string selectedPath, int chatId, int messageId)
        {
            Task.Factory.StartNew(() =>
            {
                mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_download);
                RRTemplate serverReceive = Client.ServerRequest(new RRTemplate(RRType.DownloadFile, new DownloadFileTemplate(chatId, messageId, null)));
                if (serverReceive.RRType == RRType.DownloadFile)
                {
                    DownloadFileTemplate dfileT = serverReceive.RRObject as DownloadFileTemplate;
                    if (dfileT != null)
                    {
                        using (FileStream fs = new FileStream(selectedPath + @"\\" + dfileT.Message.FileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            fs.Write(dfileT.Message.MessageBody, 0, dfileT.Message.MessageBody.Length);
                            mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_succsDownloaded, fs.Name);
                        }
                    }
                }
            });
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

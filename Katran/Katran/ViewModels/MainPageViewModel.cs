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

        private string messageText;
        public string MessageText
        {
            get { return messageText; }
            set { messageText = value; OnPropertyChanged(); }
        }


        public MainPageViewModel()
        {
            this.mainViewModel = null;
            this.mainPage = null;
            ContactsTab = new ContactsTab();
            MessageText = "";

            CreateChatTabVisibility = Visibility.Collapsed;
            settingsTabVisibility = Visibility.Collapsed;
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;
            MessageText = "";

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
                                                                                      -1,
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
                    Task.Factory.StartNew(() =>
                    {
                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                        bool? result = dlg.ShowDialog();

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

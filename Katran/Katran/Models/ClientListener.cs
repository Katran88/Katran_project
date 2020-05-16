﻿using Katran.UserControlls;
using Katran.ViewModels;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Katran.Models
{
    public class ClientListener
    {
        TcpClient client;
        NetworkStream clientStream;
        MainPageViewModel mainPageViewModel;

        public ClientListener(MainPageViewModel mainPage)
        {
            this.mainPageViewModel = mainPage;

            client = new TcpClient();
            client.Connect(Client.serverIP, Client.serverPort);
            clientStream = client.GetStream();

            RefreshClientConnection();

            Thread listenerThread = new Thread(new ThreadStart(NotifyHandling));
            listenerThread.Start();
        }

        public void CloseConnection()
        {
            clientStream.Close();
            clientStream.Dispose();
            client.Close();
            Client.ServerRequest(new RRTemplate(RRType.UserDisconected, new RefreshUserTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id)));
        }

        void RefreshClientConnection()
        {
            RRTemplate request = new RRTemplate(RRType.RefreshUserConnection, new RefreshUserTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id));


            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, request);
                clientStream.Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
            }
        }


        void NotifyHandling()
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                while (true)
                {

                    try
                    {
                        memoryStream.Flush();
                        memoryStream.Position = 0;

                        do
                        {
                            byte[] buffer = new byte[256];
                            int bytes;

                            bytes = clientStream.Read(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, bytes);

                            buffer = new byte[256];
                        }
                        while (clientStream.DataAvailable);

                        memoryStream.Position = 0;
                        RRTemplate serverResponse = formatter.Deserialize(memoryStream) as RRTemplate;

                        if (serverResponse != null)
                        {
                            switch (serverResponse.RRType)
                            {
                                case RRType.AddContact:
                                    AddRemoveContactTemplate aContT = serverResponse.RRObject as AddRemoveContactTemplate;
                                    if (aContT != null)
                                    {
                                        AddContact(aContT);
                                    }
                                    break;
                                case RRType.RemoveContact:
                                    AddRemoveContactTemplate rContT = serverResponse.RRObject as AddRemoveContactTemplate;
                                    if (rContT != null)
                                    {
                                        RemoveContact(rContT);
                                    }
                                    break;
                                case RRType.RefreshContacts:
                                    RefreshContactsTemplate refrC = serverResponse.RRObject as RefreshContactsTemplate;
                                    if (refrC != null)
                                    {
                                        RefreshContacts(refrC);
                                    }
                                    break;
                                case RRType.SearchOutContacts:
                                    SearchOutContactsTemplate searchOutC = serverResponse.RRObject as SearchOutContactsTemplate;
                                    if (searchOutC != null)
                                    {
                                        SearchOutContacts(searchOutC);
                                    }
                                    break;
                                case RRType.SendMessage:
                                    SendMessageTemplate sMessT = serverResponse.RRObject as SendMessageTemplate;
                                    if (sMessT != null)
                                    {
                                        SendMessageReceive(sMessT);
                                    }
                                    break;
                                case RRType.RefreshContactStatus:
                                    RefreshContactStatusTemplate refrContStT = serverResponse.RRObject as RefreshContactStatusTemplate;
                                    if (refrContStT != null)
                                    {
                                        RefreshContactStatus(refrContStT);
                                    }
                                    break;
                                default:
                                    if (client.Connected)
                                    {
                                        mainPageViewModel.MainViewModel.ErrorService(new ErrorReportTemplate(ErrorType.Other, new Exception("Unknown problem")));
                                    }
                                    else
                                    {
                                        return;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            if (client.Connected)
                            {
                                mainPageViewModel.MainViewModel.ErrorService(new ErrorReportTemplate(ErrorType.UnCorrectServerResponse, new Exception("Uncorrect server response")));
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (client.Connected)
                        {
                            mainPageViewModel.MainViewModel.ErrorService(new ErrorReportTemplate(ErrorType.NoConnectionWithServer, ex));
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (client.Connected)
                        {
                            mainPageViewModel.MainViewModel.ErrorService(new ErrorReportTemplate(ErrorType.Other, ex));
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void RefreshContactStatus(RefreshContactStatusTemplate refrContStT)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (ContactUI contactUI in mainPageViewModel.ContactsTab.Contacts)
                {
                    if (contactUI.ContactID == refrContStT.ContactId)
                    {
                        contactUI.ContactStatus = refrContStT.NewContactStatus;
                        break;
                    }
                }
            }));
        }

        private void SendMessageReceive(SendMessageTemplate sMessT)
        {
            Task.Factory.StartNew(() =>
            {
                if (sMessT.Message.SenderID == mainPageViewModel.MainViewModel.UserInfo.Info.Id)
                {
                    foreach (ContactUI contactUI in mainPageViewModel.ContactsTab.Contacts)
                    {
                        if (contactUI.ChatId == sMessT.ReceiverChatID)
                        {
                            foreach (MessageUI m in contactUI.ContactMessages)
                            {
                                if (m.MessageDateTime == sMessT.Message.Time && m.IsOwnerMessage)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        m.MessageState = MessageState.Sended;
                                    }
                                    ));
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    ContactUI sender = null;

                    foreach (ContactUI c in mainPageViewModel.ContactsTab.Contacts)
                    {
                        if (c.ContactID == sMessT.Message.SenderID)
                        {
                            sender = c;
                            break;
                        }
                    }
                    if (sender != null)
                    {
                        MessageUI messageUI = new MessageUI(mainPageViewModel,
                                                        false,
                                                        sender.ContactAvatar,
                                                        sender.ContactStatus,
                                                        sMessT.Message.MessageType,
                                                        sMessT.Message.MessageState,
                                                        sMessT.Message.Time,
                                                        sMessT.Message.MessageBody,
                                                        sMessT.Message.FileName,
                                                        sMessT.Message.FileSize);

                        foreach (ContactUI contactUI in mainPageViewModel.ContactsTab.Contacts)
                        {
                            if (contactUI.ChatId == sMessT.ReceiverChatID)
                            {
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    contactUI.ContactMessages.Add(messageUI);
                                }
                                ));
                                break;
                            }
                        }

                    }
                    
                }
            }
            );

            
        }

        private void RemoveContact(AddRemoveContactTemplate rContT)
        {
            ContactUI rContact = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.Contacts)
            {
                if (i.ContactID == rContT.TargetContactId)
                {
                    rContact = i;
                    break;
                }
            }

            if (rContact != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.ContactsTab.Contacts.Remove(rContact);
                    mainPageViewModel.ContactsTab.SelectedContact = null;
                }
                ));
            }
        }

        private void AddContact(AddRemoveContactTemplate arContT)
        {
            ContactUI newContact = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.FilteredNoUserContacts)
            {
                if (i.ContactID == arContT.TargetContactId)
                {
                    newContact = i;
                    break;
                }
            }

            if (newContact != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.ContactsTab.FilteredNoUserContacts.Remove(newContact);
                    mainPageViewModel.ContactsTab.Contacts.Add(newContact);
                    newContact.Visibility = Visibility.Collapsed;
                    newContact.ChatId = arContT.ChatId;
                    mainPageViewModel.ContactsTab.SelectedNoUserContact = null;
                }
                ));
            }
        }

        private void SearchOutContacts(SearchOutContactsTemplate searchOutC)
        {
            Task.Factory.StartNew(() =>
            {
                List<ContactUI> OutContacts = new List<ContactUI>();

                foreach (Contact item in searchOutC.Contacts)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MemoryStream memoryStream = new MemoryStream(item.AvatarImage);
                        BitmapImage avatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                        OutContacts.Add(new ContactUI(item.AppName, "", avatar, item.Status, item.UserId, item.ChatId, new ObservableCollection<MessageUI>()));
                    }
                    ));
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.ContactsTab.FilteredNoUserContacts.Clear();
                    foreach (ContactUI item in OutContacts)
                    {
                        mainPageViewModel.ContactsTab.FilteredNoUserContacts.Add(item);
                    }
                }
                ));
            });
        }

        private void RefreshContacts(RefreshContactsTemplate refrC)
        {
            Task.Factory.StartNew(() =>
            {
                ObservableCollection<MessageUI> tempMessagesUI;
                List<ContactUI> RefreshedContacts = new List<ContactUI>();
                MessageUI tempMessageUI;

                foreach (Contact contact in refrC.Contacts)
                {
                    tempMessagesUI = new ObservableCollection<MessageUI>();

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MemoryStream memoryStream = new MemoryStream(contact.AvatarImage);
                        BitmapImage contactAvatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                        foreach (Message i in contact.Messages)
                        {
                            tempMessageUI = new MessageUI(mainPageViewModel,
                                                          refrC.ContactsOwner == i.SenderID,
                                                          refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Avatar : contactAvatar,
                                                          refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Info.Status : contact.Status,
                                                          i.MessageType,
                                                          i.MessageState,
                                                          i.Time,
                                                          i.MessageBody,
                                                          i.FileName,
                                                          i.FileSize);
                            tempMessagesUI.Insert(0, tempMessageUI);
                        }

                        RefreshedContacts.Add(new ContactUI(contact.AppName, "", contactAvatar, contact.Status, contact.UserId, contact.ChatId, tempMessagesUI));
                    }
                    ));                    
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.ContactsTab.Contacts.Clear();
                    foreach (ContactUI item in RefreshedContacts)
                    {
                        mainPageViewModel.ContactsTab.Contacts.Add(item);
                    }
                }
                ));
            });
        }
    }
}

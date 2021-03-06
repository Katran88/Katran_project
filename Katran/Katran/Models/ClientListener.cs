﻿using Katran.Pages;
using Katran.Properties;
using Katran.UserControlls;
using Katran.ViewModels;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            Client.ServerRequest(new RRTemplate(RRType.UserDisconected, new RefreshUserTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id)));

            clientStream.Close();
            clientStream.Dispose();
            client.Close();
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
                                case RRType.AddContactTarget:
                                    AddRemoveContactTargetTemplate aconttT = serverResponse.RRObject as AddRemoveContactTargetTemplate;
                                    if (aconttT != null)
                                    {
                                        AddContactTarget(aconttT);
                                    }
                                    break;
                                case RRType.RemoveContactTarget:
                                    AddRemoveContactTargetTemplate rconttT = serverResponse.RRObject as AddRemoveContactTargetTemplate;
                                    if (rconttT != null)
                                    {
                                        RemoveContactTarget(rconttT);
                                    }
                                    break;
                                case RRType.RefreshMessageState:
                                    RefreshMessageStateTemplate refrmsT = serverResponse.RRObject as RefreshMessageStateTemplate;
                                    if (refrmsT != null)
                                    {
                                        RefreshMessageState(refrmsT);
                                    }
                                    break;
                                case RRType.CreateConv:
                                    CreateConvTemplate crconvT = serverResponse.RRObject as CreateConvTemplate;
                                    if (crconvT != null)
                                    {
                                        AddConv(crconvT);
                                    }
                                    break;
                                case RRType.RemoveConv:
                                    RemoveConvTemplate rconvT = serverResponse.RRObject as RemoveConvTemplate;
                                    if (rconvT != null)
                                    {
                                        RemoveConv(rconvT);
                                    }
                                    break;
                                case RRType.RemoveConvTarget:
                                    RemoveConvTemplate rconvtT = serverResponse.RRObject as RemoveConvTemplate;
                                    if (rconvtT != null)
                                    {
                                        RemoveConvTarget(rconvtT);
                                    }
                                    break;
                                case RRType.BlockUnblockUserTarget:
                                    BlockUnblockUserTemplate bunbUT = serverResponse.RRObject as BlockUnblockUserTemplate;
                                    if (bunbUT != null)
                                    {
                                        ChangeBlockUserStatusTarget(bunbUT);
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

        private void ChangeBlockUserStatusTarget(BlockUnblockUserTemplate bunbUT)
        {
            if (bunbUT.UserId == mainPageViewModel.MainViewModel.UserInfo.Info.Id)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (bunbUT.IsBlocked)
                    {
                        MainPageViewModel.clientListener.CloseConnection();
                        mainPageViewModel.MainViewModel.CurrentPage = new BlockPage(mainPageViewModel.MainViewModel);
                    }
                    else
                    {
                        if (mainPageViewModel.MainViewModel.UserInfo.Info.IsBlocked)
                            mainPageViewModel.MainViewModel.CurrentPage = new AuhtorizationPage(mainPageViewModel.MainViewModel);
                    }
                    mainPageViewModel.MainViewModel.UserInfo.Info.IsBlocked = bunbUT.IsBlocked;
                }));
                
            }
            else
            {
                foreach (ContactUI contactUI in mainPageViewModel.ContactsTab.Contacts)
                {
                    if (contactUI.ContactType == ContactType.Chat && contactUI.ContactID == bunbUT.UserId)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            contactUI.IsBlocked = bunbUT.IsBlocked;
                        }));
                        break;
                    }
                }
            }
        }

        private void RemoveConvTarget(RemoveConvTemplate rconvtT)
        {
            ContactUI rConv = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.Contacts)
            {
                if (i.ChatId == rconvtT.ChatId)
                {
                    rConv = i;
                    break;
                }
            }

            if (rConv != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    rConv.ContactLastMessage = (rConv.ConvMembers.Count - 1).ToString() + ' ' + (string)Application.Current.FindResource("l_Members");
                }
                ));
            }
        }

        private void RemoveConv(RemoveConvTemplate rconvT)
        {
            ContactUI rConv = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.Contacts)
            {
                if (i.ChatId == rconvT.ChatId)
                {
                    rConv = i;
                    break;
                }
            }

            if (rConv != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (mainPageViewModel.ContactsTab.SelectedContact != null &&
                        mainPageViewModel.ContactsTab.SelectedContact.Equals(rConv))
                    {
                        mainPageViewModel.CurrentChatMessages.Clear();
                        mainPageViewModel.ContactsTab.SelectedContact = null;
                    }

                    mainPageViewModel.ContactsTab.Contacts.Remove(rConv);
                }
                ));
            }
        }

        private void AddConv(CreateConvTemplate crconvT)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MemoryStream memoryStream = new MemoryStream(crconvT.Image);
                BitmapImage avatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                ContactUI newConv = new ContactUI(crconvT.Title,
                                                  crconvT.ConvMembers.Count.ToString() + ' ' + (string)Application.Current.FindResource("l_Members"),
                                                  avatar,
                                                  Status.Offline,
                                                  -1,
                                                  crconvT.ChatId,
                                                  new ObservableCollection<MessageUI>(),
                                                  false,
                                                  ContactType.Conversation,
                                                  new ObservableCollection<Contact>(crconvT.ConvMembers));

                mainPageViewModel.ContactsTab.Contacts.Add(newConv);
                if (mainPageViewModel.ContactsTab.SearchTextField.Length != 0 || mainPageViewModel.ContactsTab.IsSearchOutsideContacts)
                {
                    newConv.Visibility = Visibility.Collapsed;
                }
                else
                {
                    newConv.Visibility = Visibility.Visible;
                }
            }
            ));
        }

        private void RefreshMessageState(RefreshMessageStateTemplate refrmsT)
        {
            ContactUI contact = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.Contacts)
            {
                if (i.ChatId == refrmsT.ChatId)
                {
                    contact = i;
                    break;
                }
            }

            if (contact != null)
            {
                foreach (MessageUI m in contact.ContactMessages)
                {
                    if (m.MessageId == refrmsT.messageId)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            m.MessageState = refrmsT.MessageState;

                            if (m.MessageState == MessageState.Readed)
                            {
                                if(m.SenderId != mainPageViewModel.MainViewModel.UserInfo.Info.Id)
                                    contact.MessageCounter--;
                            }

                        }));

                        break;
                    }
                }
            }
        }

        private void RemoveContactTarget(AddRemoveContactTargetTemplate rconttT)
        {
            ContactUI rContact = null;
            foreach (ContactUI i in mainPageViewModel.ContactsTab.Contacts)
            {
                if (i.ContactID == rconttT.NewContact.UserId)
                {
                    rContact = i;
                    break;
                }
            }

            if (rContact != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (mainPageViewModel.ContactsTab.SelectedContact != null &&
                        mainPageViewModel.ContactsTab.SelectedContact.Equals(rContact))
                    {
                        mainPageViewModel.CurrentChatMessages.Clear();
                        mainPageViewModel.ContactsTab.SelectedContact = null;
                    }

                    mainPageViewModel.ContactsTab.Contacts.Remove(rContact);
                }
                ));
            }
        }

        private void AddContactTarget(AddRemoveContactTargetTemplate aconttT)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MemoryStream memoryStream = new MemoryStream(aconttT.NewContact.AvatarImage);
                BitmapImage avatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                ContactUI newContact = new ContactUI(aconttT.NewContact.AppName,
                                                     "",
                                                     avatar,
                                                     aconttT.NewContact.Status,
                                                     aconttT.NewContact.UserId,
                                                     aconttT.NewContact.ChatId,
                                                     new ObservableCollection<MessageUI>(),
                                                     aconttT.NewContact.IsBlocked);

                mainPageViewModel.ContactsTab.Contacts.Add(newContact);
                if (mainPageViewModel.ContactsTab.SearchTextField.Length != 0 || mainPageViewModel.ContactsTab.IsSearchOutsideContacts)
                {
                    newContact.Visibility = Visibility.Collapsed;
                }
                else
                {
                    newContact.Visibility = Visibility.Visible;
                }
            }
            ));
        }

        private void RefreshContactStatus(RefreshContactStatusTemplate refrContStT)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (ContactUI contactUI in mainPageViewModel.ContactsTab.Contacts)
                {
                    switch (contactUI.ContactType)
                    {
                        case ContactType.Chat:
                            if (contactUI.ContactID == refrContStT.ContactId)
                            {
                                contactUI.ContactStatus = refrContStT.NewContactStatus;
                                foreach (MessageUI m in contactUI.ContactMessages)
                                {
                                    if (m.SenderId == refrContStT.ContactId)
                                    {
                                        m.ContactStatus = refrContStT.NewContactStatus;
                                    }
                                }
                                break;
                            }
                            break;
                        case ContactType.Conversation:

                            foreach (Contact m in contactUI.ConvMembers)
                            {
                                if (m.UserId == refrContStT.ContactId)
                                {
                                    m.Status = refrContStT.NewContactStatus;
                                    break;
                                }
                            }

                            foreach (MessageUI m in contactUI.ContactMessages)
                            {
                                if (m.SenderId == refrContStT.ContactId)
                                {
                                    m.ContactStatus = refrContStT.NewContactStatus;
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }
            }));
        }

        private void SendMessageReceive(SendMessageTemplate sMessT)
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
                                    m.ChatId = sMessT.ReceiverChatID;
                                    m.MessageId = sMessT.Message.MessageID;

                                    if (m.MessageType == MessageType.File)
                                    {
                                        m.FileSize = sMessT.Message.FileSize;
                                        mainPageViewModel.MainViewModel.NotifyUserByRowState(RowStateResourcesName.l_succsUploaded);
                                    }
                                }));
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                ContactUI chat = null;

                foreach (ContactUI c in mainPageViewModel.ContactsTab.Contacts)
                {
                    if (c.ChatId == sMessT.ReceiverChatID)
                    {
                        chat = c;
                        break;
                    }
                }
                if (chat != null)
                {
                    switch (chat.ContactType)
                    {
                        case ContactType.Chat:

                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                MessageUI messageUI = new MessageUI(mainPageViewModel,
                                                                    false,
                                                                    chat.ContactAvatar,
                                                                    chat.ContactStatus,
                                                                    sMessT.Message.MessageType,
                                                                    sMessT.Message.MessageState,
                                                                    sMessT.Message.Time,
                                                                    sMessT.Message.MessageBody,
                                                                    sMessT.Message.SenderName,
                                                                    sMessT.ReceiverChatID,
                                                                    sMessT.Message.MessageID,
                                                                    chat.ContactID,
                                                                    sMessT.Message.FileName,
                                                                    sMessT.Message.FileSize);

                                chat.ContactMessages.Add(messageUI);
                                chat.MessageCounter++;
                            }));

                            break;
                        case ContactType.Conversation:

                            foreach (Contact i in chat.ConvMembers)
                            {
                                if (i.UserId == sMessT.Message.SenderID)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        MemoryStream memoryStream = new MemoryStream(i.AvatarImage);
                                        BitmapImage senderAvatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                                        MessageUI messageUI = new MessageUI(mainPageViewModel,
                                                                            false,
                                                                            senderAvatar,
                                                                            i.Status,
                                                                            sMessT.Message.MessageType,
                                                                            sMessT.Message.MessageState,
                                                                            sMessT.Message.Time,
                                                                            sMessT.Message.MessageBody,
                                                                            sMessT.Message.SenderName,
                                                                            sMessT.ReceiverChatID,
                                                                            sMessT.Message.MessageID,
                                                                            i.UserId,
                                                                            sMessT.Message.FileName,
                                                                            sMessT.Message.FileSize);

                                        chat.ContactMessages.Add(messageUI);
                                        chat.MessageCounter++;
                                    }));
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }

            }
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
                    

                    if (mainPageViewModel.ContactsTab.SelectedContact != null &&
                        mainPageViewModel.ContactsTab.SelectedContact.Equals(rContact))
                    {
                        mainPageViewModel.CurrentChatMessages = new ObservableCollection<MessageUI>();
                    }

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

                        OutContacts.Add(new ContactUI(item.AppName, "", avatar, item.Status, item.UserId, item.ChatId, new ObservableCollection<MessageUI>(), item.IsBlocked));
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
                        BitmapImage chatAvatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                        switch (contact.ContactType)
                        {
                            case ContactType.Chat:
                                foreach (Message i in contact.Messages)
                                {
                                    tempMessageUI = new MessageUI(mainPageViewModel,
                                                                  refrC.ContactsOwner == i.SenderID,
                                                                  refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Avatar : chatAvatar,
                                                                  refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Info.Status : contact.Status,
                                                                  i.MessageType,
                                                                  i.MessageState,
                                                                  i.Time,
                                                                  i.MessageBody,
                                                                  i.SenderName,
                                                                  contact.ChatId,
                                                                  i.MessageID,
                                                                  i.SenderID,
                                                                  i.FileName,
                                                                  i.FileSize);
                                    tempMessagesUI.Insert(0, tempMessageUI);
                                }
                                RefreshedContacts.Add(new ContactUI(contact.AppName, "", chatAvatar, contact.Status, contact.UserId, contact.ChatId, tempMessagesUI, contact.IsBlocked));
                                break;
                            case ContactType.Conversation:
                                foreach (Message i in contact.Messages)
                                {
                                    Status memberStatus = Status.Offline;
                                    BitmapImage memberImage = null;
                                    bool isUserLeaveConv = false;
                                    foreach (Contact member in contact.Members)
                                    {
                                        if (member.UserId == i.SenderID)
                                        {
                                            memberStatus = member.Status;
                                            MemoryStream ms = new MemoryStream(member.AvatarImage);
                                            memberImage = Converters.BitmapToImageSource(new Bitmap(ms));
                                            i.SenderName = member.AppName;
                                            break;
                                        }
                                    }

                                    if (memberImage == null)
                                    {
                                        memberImage = Converters.BitmapToImageSource(Resources.Katran);
                                        isUserLeaveConv = true;
                                    }

                                    tempMessageUI = new MessageUI(mainPageViewModel,
                                                                  refrC.ContactsOwner == i.SenderID,
                                                                  refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Avatar : memberImage,
                                                                  refrC.ContactsOwner == i.SenderID ? mainPageViewModel.MainViewModel.UserInfo.Info.Status : memberStatus,
                                                                  i.MessageType,
                                                                  i.MessageState,
                                                                  i.Time,
                                                                  i.MessageBody,
                                                                  i.SenderName,
                                                                  contact.ChatId,
                                                                  i.MessageID,
                                                                  i.SenderID,
                                                                  i.FileName,
                                                                  i.FileSize);

                                    if (isUserLeaveConv)
                                    {
                                        tempMessageUI.UserName = (string)Application.Current.FindResource("l_UserLoggedOut");
                                    }

                                    tempMessagesUI.Insert(0, tempMessageUI);
                                }
                                RefreshedContacts.Add(new ContactUI(contact.AppName, contact.Members.Count.ToString() + ' ' + (string)Application.Current.FindResource("l_Members"), chatAvatar, contact.Status, contact.UserId, contact.ChatId, tempMessagesUI, false, ContactType.Conversation, new ObservableCollection<Contact>(contact.Members)));
                                break;
                            default:
                                break;
                        }

                        
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

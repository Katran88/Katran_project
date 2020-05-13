using Katran.UserControlls;
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
        //    if (mainPageViewModel.MainViewModel.UserInfo.Info == null)
        //    {
        //        FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.Open, FileAccess.Read);

        //        BinaryFormatter binForm = new BinaryFormatter();
        //        RegistrationTemplate regTempl = binForm.Deserialize(fs) as RegistrationTemplate;
        //        if (regTempl != null)
        //        {
        //            mainPageViewModel.MainViewModel.UserInfo.Info = regTempl;
        //        }
        //        fs.Close();
        //    }

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

        private void RemoveContact(AddRemoveContactTemplate rContT)
        {
            ContactUI rContact = null;
            foreach (ContactUI i in mainPageViewModel.Contacts)
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
                    mainPageViewModel.Contacts.Remove(rContact);
                    mainPageViewModel.SelectedContact = null;
                }
                ));
            }
        }

        private void AddContact(AddRemoveContactTemplate arContT)
        {
            ContactUI newContact = null;
            foreach (ContactUI i in mainPageViewModel.FilteredNoUserContacts)
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
                    mainPageViewModel.FilteredNoUserContacts.Remove(newContact);
                    mainPageViewModel.Contacts.Add(newContact);
                    newContact.Visibility = Visibility.Collapsed;
                    mainPageViewModel.SelectedNoUserContact = null;
                }
                ));
            }
        }

        private void SearchOutContacts(SearchOutContactsTemplate searchOutC)
        {
            Task.Factory.StartNew(() =>
            {
                MemoryStream memoryStream;
                Bitmap avatar;
                List<ContactUI> OutContacts = new List<ContactUI>();

                foreach (Contact item in searchOutC.Contacts)
                {
                    memoryStream = new MemoryStream(item.AvatarImage);
                    avatar = new Bitmap(memoryStream);

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        OutContacts.Add(new ContactUI(item.AppName, "", Converters.BitmapToImageSource(avatar), item.Status, item.UserId));
                    }
                    ));
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.FilteredNoUserContacts.Clear();
                    foreach (ContactUI item in OutContacts)
                    {
                        mainPageViewModel.FilteredNoUserContacts.Add(item);
                    }
                }
                ));
            });
        }

        private void RefreshContacts(RefreshContactsTemplate refrC)
        {
            Task.Factory.StartNew(() =>
            {
                MemoryStream memoryStream;
                Bitmap avatar;
                List<ContactUI> RefreshedContacts = new List<ContactUI>();

                foreach (Contact item in refrC.Contacts)
                {
                    memoryStream = new MemoryStream(item.AvatarImage);
                    avatar = new Bitmap(memoryStream);

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        RefreshedContacts.Add(new ContactUI(item.AppName, "", Converters.BitmapToImageSource(avatar), item.Status, item.UserId));
                    }
                    ));                    
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainPageViewModel.Contacts.Clear();
                    foreach (ContactUI item in RefreshedContacts)
                    {
                        mainPageViewModel.Contacts.Add(item);
                    }
                }
                ));
            });
        }
    }
}

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

            RefreshClientIP();

            Thread listenerThread = new Thread(new ThreadStart(NotifyHandling));
            listenerThread.Start();
        }

        public void CloseConnection()
        {
            clientStream.Close();
            clientStream.Dispose();
            client.Close();
        }

        void RefreshClientIP()
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
                                case RRType.RefreshContacts:
                                    RefreshContactsTemplate refrC = serverResponse.RRObject as RefreshContactsTemplate;
                                    if (refrC != null)
                                    {
                                        RefreshContacts(refrC);
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

        private void RefreshContacts(RefreshContactsTemplate refrC)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => 
            {
                MemoryStream memoryStream;
                Bitmap avatar;
                List<ContactUI> RefreshedContacts = new List<ContactUI>();

                foreach (Contact item in refrC.Contacts)
                {
                    memoryStream = new MemoryStream(item.AvatarImage);
                    avatar = new Bitmap(memoryStream);
                    RefreshedContacts.Add(new ContactUI(item.AppName, "", Converters.BitmapToImageSource(avatar), item.Status, item.UserId));
                }
            
                mainPageViewModel.Contacts.Clear();
                foreach(ContactUI item in RefreshedContacts)
                {
                    mainPageViewModel.Contacts.Add(item);
                }


                Grid ContactsGrid = mainPageViewModel.MenuContentPresenter.Content as Grid;
                if (ContactsGrid != null)
                {
                    ContactsGrid.Children.Add(new ContactsBorder(mainPageViewModel.SearchOutContacts));
                    Grid.SetRow(ContactsGrid.Children[ContactsGrid.Children.Count - 1], 2);
                }

            }
            ));
        }
    }
}

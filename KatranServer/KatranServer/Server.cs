using KatranClassLibrary;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace KatranServer
{
    class ConectedUser
    {
        public int id;
        public TcpClient userSocket;

        public ConectedUser(int id, TcpClient userSocket)
        {
            this.id = id;
            this.userSocket = userSocket;
        }
    }


    class Server
    {
        internal static List<ConectedUser> conectedUsers;
        const string ip = "127.0.0.1";
        const int port = 8001;
        static OracleConnection connection = OracleDB.GetDBConnection();
        static void Main(string[] args)
        {
            conectedUsers = new List<ConectedUser>();
            TimerCallback tm = new TimerCallback(RefreshForOnlineUsers);
            Timer timer = new Timer(tm, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            connection.Open();

            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(ip), port);
                tcpListener.Start();
                while (true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(new ThreadStart((new ClientService(client)).Service));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (tcpListener != null)
                    tcpListener.Stop();
                connection.Close();
            }
        }

        private static void RefreshForOnlineUsers(object state)
        {
            if (conectedUsers != null && conectedUsers.Count > 0)
            {
                List<int> offlineUsersID = new List<int>();

                foreach (ConectedUser user in conectedUsers)
                {
                    if(!user.userSocket.Connected)
                        offlineUsersID.Add(user.id);
                }

                foreach (int id in offlineUsersID)
                {
                    ConectedUser user = conectedUsers.Find((el) => el.id == id);
                    if (user != null)
                    {
                        user.userSocket.Close();
                        conectedUsers.Remove(user);
                    }
                }

                foreach (int userID in offlineUsersID)
                {
                    ChangeStatus(userID, Status.Offline);
                }
            }
        }

        private static object locker = new object();
        internal static void ChangeStatus(int userID, Status newStatus)
        {
            lock (locker)
            {
                using (OracleCommand refreshStatusCommand = new OracleCommand($"{ClientService.GetPackageName()}.ChangeUserStatusById", connection))
                {
                    refreshStatusCommand.CommandType = CommandType.StoredProcedure;
                    refreshStatusCommand.Parameters.Add(ClientService.CreateParam("inNewUserStatus", newStatus.ToString(), ParameterDirection.Input));
                    refreshStatusCommand.Parameters.Add(ClientService.CreateParam("in_outUserId", userID, ParameterDirection.InputOutput));
                    refreshStatusCommand.ExecuteNonQuery();
                }

                if (newStatus == Status.Offline)
                {
                    conectedUsers.Remove(conectedUsers.Find((x) => x.id == userID));
                }
                    

                foreach (ConectedUser u in conectedUsers)
                {
                    try
                    {
                        if (u.id != userID)
                        {
                            if (u.userSocket.Connected)
                            {
                                BinaryFormatter formatter = new BinaryFormatter();
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshContactStatus, new RefreshContactStatusTemplate(userID, newStatus)));

                                    u.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                                }
                            }
                            else
                            {
                                ChangeStatus(u.id, Status.Offline);
                            }
                        }
                    }
                    catch (IOException ioEx)
                    {
                        ChangeStatus(u.id, Status.Offline);
                    }
                }
            }
        }
    }
}

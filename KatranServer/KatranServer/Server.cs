using KatranClassLibrary;
using System;
using System.Collections.Generic;
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
        internal static SqlConnection sql;
        internal static List<ConectedUser> conectedUsers;
        const string ip = "127.0.0.1";
        const int port = 8001;

        static void Main(string[] args)
        {
            conectedUsers = new List<ConectedUser>();
            TimerCallback tm = new TimerCallback(RefreshForOnlineUsers);
            Timer timer = new Timer(tm, null, TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(0.5));


            TcpListener server = null;
            try
            {
                Server.sql = DBConnection.getInstance();

                server = new TcpListener(IPAddress.Parse(ip), port);
                server.Start();
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
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
                if (server != null)
                    server.Stop();
            }
        }

        private static void RefreshForOnlineUsers(object state)
        {
            if (conectedUsers != null && conectedUsers.Count > 0)
            {
                List<int> offlineUsersID = new List<int>();

                foreach (ConectedUser item in conectedUsers)
                {
                    if(!item.userSocket.Connected)
                        offlineUsersID.Add(item.id);
                }

                foreach (int item in offlineUsersID)
                {
                    ConectedUser user = conectedUsers.Find((el) => el.id == item);
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

        internal static void ChangeStatus(int userID, Status newStatus)
        {
            using (SqlCommand refreshStatusCommand = new SqlCommand("update Users_info set status = @status where id = @id", DBConnection.getInstance()))
            {
                refreshStatusCommand.Parameters.Add(new SqlParameter("@id", userID));
                refreshStatusCommand.Parameters.Add(new SqlParameter("@status", newStatus.ToString()));
                refreshStatusCommand.ExecuteNonQuery();
            }

            foreach (ConectedUser u in conectedUsers)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, new RRTemplate(RRType.RefreshContactStatus, new RefreshContactStatusTemplate(u.id, newStatus)));

                    u.userSocket.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }
            }
        }
    }
}

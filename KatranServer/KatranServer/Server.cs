using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KatranServer
{
    class Server
    {
        internal static SqlConnection sql;
        const string ip = "127.0.0.1";
        const int port = 8001;

        static void Main(string[] args)
        {
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
                Console.ReadKey();
            }
            finally
            {
                if (server != null)
                    server.Stop();
            }
        }
    }
}

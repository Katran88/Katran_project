using System;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
namespace KatranServer
{
    //public class DBConnection
    //{
    //    private static SqlConnection connectionInstance;
    //    const string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Katran;Integrated Security=True";
    //    private static object locker = new object();

    //    private DBConnection()
    //    {

    //    }
    //    public static SqlConnection getInstance()
    //    {
    //        lock (locker)
    //        {
    //            if (connectionInstance == null)
    //            {
    //                connectionInstance = new SqlConnection(connectionString);
    //                connectionInstance.Open();
    //                return connectionInstance;
    //            }
    //        }
    //        return connectionInstance;
    //    }

    //    ~DBConnection()
    //    {
    //        connectionInstance.Close();
    //    }

    //}

    static class OracleDB
    {
        static string host = "192.168.137.8";
        static int port = 1521;
        static string sid = "katran_db";
        static string user_name = "katran_user";
        static string user_password = "fu6djHH763";
        static string admin_name = "katran_admin";
        static string admin_password = "fu6djHH763";

        public static OracleConnection GetDBConnection(bool needAdminPrivs = false)
        {
            Console.WriteLine("Getting Connection ...");

            string username;
            string password;

            if (needAdminPrivs)
            {
                username = admin_name;
                password = admin_password;
            }
            else
            {
                username = user_name;
                password = user_password;
            }

            string connString = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = "
                 + host + ")(PORT = " + port + "))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = "
                 + sid + ")));Password=" + password + ";User ID=" + username;

            OracleConnection oracleConnection = new OracleConnection(connString);
            return oracleConnection;
        }
    }
}
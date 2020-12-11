using System;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
namespace KatranServer
{
    static class OracleDB
    {
        static string host = "192.168.137.8";
        static int port = 1521;
        static string sid = "katran_db";
        static string user_name = "katran_user";
        static string user_password = "fu6djHH763";
        static string admin_name = "katran_admin";
        static string admin_password = "qw2SF24xvGGS";

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
using System;
using System.Data.SqlClient;

namespace KatranServer
{
    public class DBConnection
    {
        private static SqlConnection connectionInstance;
        const string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Katran;Integrated Security=True";
        private static object locker = new object();

        private DBConnection()
        {

        }
        public static SqlConnection getInstance()
        {
            lock (locker)
            {
                if (connectionInstance == null)
                {
                    connectionInstance = new SqlConnection(connectionString);
                    connectionInstance.Open();
                    return connectionInstance;
                }
            }
            return connectionInstance;
        }

        ~DBConnection()
        {
            connectionInstance.Close();
        }

    }
}

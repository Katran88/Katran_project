using KatranClassLibrary;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace KatranServer
{
    class ClientService
    {
        private TcpClient client;
        public ClientService(TcpClient client)
        {
            this.client = client;
        }

        public void Service()
        {
            NetworkStream clientStream = client.GetStream();

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {

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
                RRTemplate clientRequest = formatter.Deserialize(memoryStream) as RRTemplate;

                if (clientRequest != null)
                {
                    switch (clientRequest.RRType)
                    {
                        case RRType.None:
                            break;
                        case RRType.Authorization:
                            AuthtorizationTemplate auth = clientRequest.RRObject as AuthtorizationTemplate;
                            if (auth != null)
                            {
                                Authtorization(auth);
                            }
                            break;
                        case RRType.Registration:
                            break;
                    }
                }
            }

            clientStream.Close();
            client.Close();
        }

        void Authtorization(AuthtorizationTemplate auth)
        {
            //SqlCommand command2 = new SqlCommand("INSERT INTO Users (auth_login, password, law_status) VALUES (@auth_login, @password, 'admin')", Server.sql);

            //command2.Parameters.Add(new SqlParameter("@auth_login", auth.auth_login));
            //command2.Parameters.Add(new SqlParameter("@password", auth.password));
            //command2.ExecuteNonQuery();


            SqlCommand command = new SqlCommand("select id, law_status from Users where auth_login = @auth_login and password = @password", Server.sql);
            command.Parameters.Add(new SqlParameter("@auth_login", auth.AuthLogin));
            command.Parameters.Add(new SqlParameter("@password", auth.Password));
            SqlDataReader reader_Users = command.ExecuteReader();

            if (reader_Users.HasRows)
            {
                reader_Users.Read();
                command = new SqlCommand("select * from Users_info where id = @id", Server.sql);
                command.Parameters.Add(new SqlParameter("@id", (int)reader_Users.GetValue(0)));
                reader_Users.Close();

                SqlDataReader reader_Users_info = command.ExecuteReader();

                reader_Users_info.Read();
                RegistrationTemplate regTempl = new RegistrationTemplate(
                    (int)reader_Users_info.GetValue(0),
                    (string)reader_Users_info.GetValue(1),
                    (string)reader_Users_info.GetValue(2),
                    (string)reader_Users_info.GetValue(3),
                    (byte[])reader_Users_info.GetValue(4),
                    (Status)reader_Users_info.GetValue(5),
                    (LawStatus)reader_Users.GetValue(1));

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, regTempl);

                    client.GetStream().Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);
                }

                reader_Users_info.Close();
            }
            else 
            {
                //обработка неверно введенных данных пользователя
            }
        }
    }
}

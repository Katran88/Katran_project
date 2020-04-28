using Katran.ViewModels;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Katran.Models
{
    public static class Client
    {
        const string serverIP = "127.0.0.1";
        const int serverPort = 8001;

        public static void ServerRequest(RRTemplate request)
        {
            TcpClient client = new TcpClient();
            client.Connect(serverIP, serverPort);
            NetworkStream stream = client.GetStream();

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, request);

                stream.Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);

                memoryStream.Flush();
                memoryStream.Position = 0;

                do
                {
                    byte[] buffer = new byte[256];
                    int bytes;

                    bytes = stream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, bytes);

                    buffer = new byte[256];
                }
                while (stream.DataAvailable);

                memoryStream.Position = 0;
                RRTemplate serverResponse = formatter.Deserialize(memoryStream) as RRTemplate;

                if (serverResponse != null)
                {
                    switch (serverResponse.RRType)
                    {
                        case RRType.Authorization:
                            RegistrationTemplate reg = serverResponse.RRObject as RegistrationTemplate; //RegistrationTemplate служит как шаблон для преднастройки приложения
                            if (reg != null)
                            {
                                Authtorization(reg);
                            }
                            break;
                        case RRType.Error:
                            ErrorReportTemplate error = serverResponse.RRObject as ErrorReportTemplate;
                            if (error != null)
                            {
                                ErrorService(error);
                            }
                            break;
                        default:
                            MessageBox.Show("Получен необработанный ответ с сервера");
                            break;
                    }
                }


            }

            stream.Close();
            client.Close();
        }

        static private void Authtorization(RegistrationTemplate reg)
        {
            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, reg);
            }

            MainViewModel.UserInfo = new UserInfo(reg);
        }

        static private void ErrorService(ErrorReportTemplate error)
        {
            switch (error.ErrorType)
            {
                case ErrorType.Other:
                    MessageBox.Show(error.Error.Message);
                    break;
                case ErrorType.WrongLoginOrPassword:
                    MessageBox.Show("Wrong login or password");
                    break;
                case ErrorType.UserAlreadyRegistr:
                    MessageBox.Show("Login is already busy");
                    break;
            }
        }
    }
}

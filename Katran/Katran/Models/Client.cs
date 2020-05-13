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
        static TcpClient client;
        static NetworkStream clientStream;

        public const string serverIP = "127.0.0.1";
        public const int serverPort = 8001;

        public static RRTemplate ServerRequest(RRTemplate request)
        {
            try
            {
                client = new TcpClient();
                client.Connect(serverIP, serverPort);
                clientStream = client.GetStream();

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    formatter.Serialize(memoryStream, request);

                    clientStream.Write(memoryStream.GetBuffer(), 0, memoryStream.GetBuffer().Length);

                    switch (request.RRType) //если попадает в default, то будет ответ, если нет, то нет
                    {
                        case RRType.RefreshContacts:
                        case RRType.SearchOutContacts:
                        case RRType.UserDisconected:
                        case RRType.AddContact:
                        case RRType.RemoveContact:    
                            clientStream.Close();
                            client.Close();
                            return null;
                            break;
                        default:
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
                                    case RRType.Authorization:
                                    case RRType.RefreshUserData:
                                        RegistrationTemplate reg = serverResponse.RRObject as RegistrationTemplate; //RegistrationTemplate служит как шаблон для преднастройки приложения
                                        if (reg != null)
                                        {
                                            CreateAuthToken(reg);
                                            return serverResponse;
                                        }
                                        break;
                                    case RRType.Error:
                                        ErrorReportTemplate error = serverResponse.RRObject as ErrorReportTemplate;
                                        if (error != null)
                                        {
                                            return new RRTemplate(RRType.Error, error);
                                        }
                                        break;
                                    default:
                                        return new RRTemplate(RRType.Error, new ErrorReportTemplate(ErrorType.Other, new Exception("Unknown problem")));
                                        break;
                                }
                            }
                            else
                            {
                                return new RRTemplate(RRType.Error, new ErrorReportTemplate(ErrorType.UnCorrectServerResponse, new Exception("Uncorrect server response")));
                            }
                            break;
                    }

                }
                return new RRTemplate(RRType.Error, new ErrorReportTemplate(ErrorType.Other, new Exception("Unknown problem")));
            }
            catch (SocketException ex)
            {
                return new RRTemplate(RRType.Error, new ErrorReportTemplate(ErrorType.NoConnectionWithServer, ex));
            }
            catch (Exception ex)
            {
                return new RRTemplate(RRType.Error, new ErrorReportTemplate(ErrorType.Other, ex));
            }
        }

        static private void CreateAuthToken(RegistrationTemplate reg)
        {
            using (FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, reg);
            }
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

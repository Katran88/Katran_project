﻿using Katran.Models;
using KatranClassLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;


namespace Katran.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        const string serverIP = "127.0.0.1";
        const int serverPort = 8001;

        public UserInfo UserInfo;

        public MainViewModel()
        {
            UserInfo = null;

            //ServerRequest(new RRTemplate(RRType.Registration, new RegistrationTemplate(-1,
            //                                                                           "Vasya",
            //                                                                           "vasyaEmail@gmail.com",
            //                                                                           "Hello everyone!",
            //                                                                           null,
            //                                                                           Status.Online,
            //                                                                           LawStatus.User,
            //                                                                           "VasyaL",
            //                                                                           "vasya1234")));

            //ServerRequest(new RRTemplate(RRType.Authorization, new AuthtorizationTemplate("Katran", "12345"))); //для теста


            try
            {
                using (FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    RegistrationTemplate regTempl = formatter.Deserialize(fs) as RegistrationTemplate;
                    if (regTempl != null)
                    {
                        UserInfo = new UserInfo(regTempl);
                        //открыть стандартное окно
                    }
                    else
                    {
                        //открыть окно авторизации
                    }

                }
            }
            catch (FileNotFoundException ex)
            {
                //открыть окно авторизации

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void ServerRequest(RRTemplate request)
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

        private void ErrorService(ErrorReportTemplate error)
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

        private void Authtorization(RegistrationTemplate reg)
        {
            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, reg);
            }

            UserInfo = new UserInfo(reg);
        }

        public ICommand commandExample
        {
            get 
            {
                return new DelegateCommand(obj =>
                {
                    //что будет происходить при вызове команды
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

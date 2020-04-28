using Katran.Models;
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
        public static UserInfo UserInfo;

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

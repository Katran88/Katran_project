﻿using Katran.Models;
using Katran.Pages;
using KatranClassLibrary;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Katran.ViewModels
{
    public enum RowStateResourcesName //енамы полностью идентичны именам ресурсов из словаря Dictionary_RU(EN)
    {
        l_sAuth,                    //Succsessful authtorization
        l_sReg,                     //Succsessful registration
        l_uncorServerResponse,      //Uncorrect server response
        l_wrngLogOrPass,            //Wrong login or password
        l_userAlrReg,               //User already registered
        l_undefError,               //Undefined error
        l_noConWithServer,          //No connection with server
        l_forgotLogin,              //Type your login
        l_forgotUsername,           //Type your username
        l_forgotEmail,              //Type your email
        l_forgotPassword,           //Type your password
        l_uncorrectEmail,           //Uncorrect email
        l_uncorrectPassword,        //Uncorrect password
        l_notSamePasswords,         //Passwords are not same
        l_download,                 //Download file...
        l_succsDownloaded,          //Succsessful download
        l_upload,                   //Upload file...
        l_succsUploaded,            //Succsessful upload
        l_forgotConvTitle,          //Type conversation title
        l_convCreated,              //Conversation has been created, check your Contacts Tab
        l_fileIsTooLarge            //File is too large, you can send only less then 2 Gb file
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        internal static UserInfo userInfo;
        public UserInfo UserInfo
        {
            get
            {
                return userInfo;
            }

            set
            {
                userInfo = value;
                OnPropertyChanged();
            }
        }

        internal Settings settings;
        public Settings CurrentSettings
        {
            get
            {
                return settings;
            }

            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private static Page currentPage;
        public Page CurrentPage
        {
           get 
           {
                return MainViewModel.currentPage;
           }

           set
           {
                MainViewModel.currentPage = value;
                OnPropertyChanged();
            }
        }

        private string cultureTag;
        public string CultureTag
        {
            get
            {
                return cultureTag;
            }

            set
            {
                cultureTag = value;
                OnPropertyChanged();
            }
        }

        private string rowState;
        public string RowState
        {
            get
            {
                return rowState;
            }

            set
            {
                rowState = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            UserInfo = new UserInfo(null);

            try
            {
                XmlSerializer formatter = new XmlSerializer(typeof(Settings));

                using (FileStream fs = new FileStream(Settings.SettingsFileName, FileMode.OpenOrCreate))
                {
                    CurrentSettings = (Settings)formatter.Deserialize(fs);
                    CurrentSettings.ApplySettings();
                }
            }
            catch(Exception ex)
            {
                CurrentSettings = new Settings();
            }
            CultureTag = CurrentSettings.CurrentCulture.ToString();
            TryAuthtorizait(true);
        }

        public void TryAuthtorizait(bool isNeedRefreshUserData)
        {
            try
            {
                FileStream fs = new FileStream(RegistrationTemplate.AuthTokenFileName, FileMode.Open, FileAccess.Read);

                BinaryFormatter formatter = new BinaryFormatter();
                RegistrationTemplate regTempl = formatter.Deserialize(fs) as RegistrationTemplate;
                if (regTempl != null)
                {
                    if (isNeedRefreshUserData)
                    {
                        fs.Close();
                        RRTemplate response = Client.ServerRequest(new RRTemplate(RRType.RefreshUserData, new AuthtorizationTemplate(regTempl.Login, "")));

                        switch (response.RRType)
                        {
                            case RRType.RefreshUserData:
                                RegistrationTemplate regResponseObj = response.RRObject as RegistrationTemplate;
                                if (regResponseObj != null)
                                {
                                    UserInfo.Info = regResponseObj;
                                }
                                break;
                            default:
                                ErrorService(response.RRObject as ErrorReportTemplate);
                                break;
                        }
                    }

                    if (UserInfo.Info != null)
                    {
                        if (UserInfo.Info.IsBlocked)
                        {
                            CurrentPage = new BlockPage(this);
                        }
                        else
                        {
                            CurrentPage = new MainPage(this);
                            NotifyUserByRowState(RowStateResourcesName.l_sAuth);
                        }
                    }               
                }
                else
                {
                    CurrentPage = new AuhtorizationPage(this);
                }
            }
            catch(FileNotFoundException ex)
            {
                CurrentPage = new Pages.AuhtorizationPage(this);
            }
            catch (SocketException ex)
            {
                NotifyUserByRowState(RowStateResourcesName.l_noConWithServer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " (ИЗ MainViewModel!!!!)");
            }
        }

        public void NotifyUserByRowState(RowStateResourcesName notifyString, string additionalNotifyInfo = "")
        {
            try
            {
                if (Application.Current != null)
                {
                    object findedResourse = Application.Current.FindResource(notifyString.ToString());
                    if (findedResourse != null)
                    {
                        this.RowState = (string)findedResourse + " " + additionalNotifyInfo;
                    }
                }
                
            }
            catch
            {
                this.RowState = (string)Application.Current.FindResource(RowStateResourcesName.l_undefError.ToString()) + " " + additionalNotifyInfo;
            }
        }

        public ICommand ChangeCulture
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (CultureTag.Equals("RU"))
                    {
                        App.Language = new CultureInfo("EN");
                        CurrentSettings.CurrentCulture = Settings.Culture.EN;
                        CultureTag = "EN";
                    }
                    else
                    {
                        App.Language = new CultureInfo("RU");
                        CurrentSettings.CurrentCulture = Settings.Culture.RU;
                        CultureTag = "RU";
                    }

                    Settings.SaveSettings(CurrentSettings);

                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public void ErrorService(ErrorReportTemplate errorReportTemplate)
        {
            if (errorReportTemplate != null)
            {
                switch (errorReportTemplate.ErrorType)
                {
                    case ErrorType.Other:
                        this.NotifyUserByRowState(RowStateResourcesName.l_undefError, errorReportTemplate.Error.Message);
                        break;
                    case ErrorType.UnCorrectServerResponse:
                        this.NotifyUserByRowState(RowStateResourcesName.l_uncorServerResponse);
                        break;
                    case ErrorType.WrongLoginOrPassword:
                        this.NotifyUserByRowState(RowStateResourcesName.l_wrngLogOrPass);
                        break;
                    case ErrorType.UserAlreadyRegistr:
                        this.NotifyUserByRowState(RowStateResourcesName.l_userAlrReg);
                        break;
                    case ErrorType.NoConnectionWithServer:
                        this.NotifyUserByRowState(RowStateResourcesName.l_noConWithServer);
                        break;
                }
            }
            else
            {
                this.NotifyUserByRowState(RowStateResourcesName.l_uncorServerResponse);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

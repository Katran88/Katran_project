using Katran.Models;
using Katran.Pages;
using Katran.UserControlls;
using KatranClassLibrary;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class RegPageViewModel : INotifyPropertyChanged
    {
        MainViewModel mainViewModel;
        RegistrationPage regPage;

        public RegPageViewModel()
        {
            this.mainViewModel = null;
            this.regPage = null;
            Login = "";
            Username = "";
            Email = "";
            FileName = "";
        }

        string login;
        public string Login
        {
            get { return login; }
            set { login = value; OnPropertyChanged(); }
        }

        string email;
        public string Email
        {
            get { return email; }
            set { email = value; OnPropertyChanged(); }
        }

        string username;
        public string Username
        {
            get { return username; }
            set { username = value; OnPropertyChanged(); }
        }

        byte[] userAvatar;
        string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; OnPropertyChanged(); }
        }

        public RegPageViewModel(MainViewModel mainViewModel, RegistrationPage regPage)
        {
            
            this.mainViewModel = mainViewModel;
            this.regPage = regPage;
            Login = "";
            Username = "";
            Email = "";
            FileName = "";
        }

        public ICommand SelectImage
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                    dlg.DefaultExt = ".png";
                    dlg.Filter = "Images (*.jpeg, *.jpg, *.png)|*.jpeg;*.jpg;*.png";

                    bool? result = dlg.ShowDialog();

                    if (result.HasValue && result.Value == true)
                    {
                        Image imageIn = new Bitmap(dlg.FileName);
                        using (var ms = new MemoryStream())
                        {
                            imageIn.Save(ms, imageIn.RawFormat);

                            userAvatar = ms.GetBuffer();
                        }

                        string[] splitedStr = dlg.FileName.Split(@"\\".ToCharArray());
                        FileName = splitedStr[splitedStr.Length - 1];
                    }
                    else
                    { 
                        userAvatar = null;
                    }
                        
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand SignInCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (Login.Length == 0)
                    {
                        regPage.loginInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotLogin);
                    }

                    if (Username.Length == 0)
                    {
                        regPage.usernameInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotUsername);
                    }

                    if (Email.Length == 0)
                    {
                        regPage.emailInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotEmail);
                    }

                    if (regPage.Password_1.Length == 0)
                    {
                        regPage.passwordInputField_1.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotPassword);
                    }

                    if (regPage.Password_2.Length == 0)
                    {
                        regPage.passwordInputField_2.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotPassword);
                    }

                    if (regPage.Password_1.CompareTo(regPage.Password_2) != 0)
                    {
                        regPage.passwordInputField_1.Textbox_InputField_UncorrectValueStyle();
                        regPage.passwordInputField_2.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_notSamePasswords);
                    }


                    if (Regex.IsMatch(regPage.Password_1, @"[а-я|А-Я|\s]+"))
                    {
                        regPage.Password_1 = "";
                        regPage.Password_2 = "";
                        regPage.passwordInputField_1.Textbox_InputField_UncorrectValueStyle();
                        regPage.passwordInputField_2.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_uncorrectPassword);
                    }
                    else
                    {
                        if (Login.Length != 0 &&
                            regPage.Password_1.Length != 0 &&
                            regPage.Password_1.CompareTo(regPage.Password_2) == 0 &&
                            Username.Length != 0 &&
                            Email.Length != 0)
                        {

                            Task.Factory.StartNew(() =>
                                {
                                    RRTemplate response = Client.ServerRequest(new RRTemplate(RRType.Registration, new RegistrationTemplate(-1,
                                                                                                                       Username,
                                                                                                                       Email,
                                                                                                                       "",
                                                                                                                       userAvatar,
                                                                                                                       Status.Online,
                                                                                                                       LawStatus.User,
                                                                                                                       Login,
                                                                                                                       regPage.Password_1)));
                                    switch (response.RRType)
                                    {
                                        case RRType.Authorization:
                                            RegistrationTemplate regResponseObj = response.RRObject as RegistrationTemplate;
                                            if (regResponseObj != null)
                                            {
                                                this.mainViewModel.UserInfo.Info = regResponseObj;
                                                Application.Current.Dispatcher.Invoke((Action)delegate { this.mainViewModel.TryAuthtorizait(false); });
                                            }
                                            else
                                            {
                                                this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_uncorServerResponse);
                                            }

                                            break;
                                        case RRType.None:
                                        case RRType.Registration:
                                            this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_uncorServerResponse);
                                            break;
                                        case RRType.Error:
                                            this.mainViewModel.ErrorService(response.RRObject as ErrorReportTemplate);
                                            break;
                                    }
                                }
                            );
                            
                        }
                    }

                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand SignInPageCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    this.mainViewModel.CurrentPage = new AuhtorizationPage(this.mainViewModel);
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

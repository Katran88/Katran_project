using Katran.Models;
using Katran.Pages;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class AuthPageViewModel : INotifyPropertyChanged
    {
        AuhtorizationPage page;
        MainViewModel mainViewModel;

        string login;
        public string Login
        {
            get { return login; }
            set { login = value; OnPropertyChanged(); }
        }

        public AuthPageViewModel()
        {
            Login = "";
        }

        public AuthPageViewModel(MainViewModel mainViewModel, AuhtorizationPage page)
        {
            Login = "";
            this.mainViewModel = mainViewModel;
            this.page = page;
        }

        public ICommand SignInCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    if (Login.Length == 0)
                    {
                        page.loginInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotLogin);
                    }

                    if (page.Password.Length == 0)
                    {
                        page.passwordInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_forgotPassword);
                    }

                    if (Regex.IsMatch(page.Password, @"[а-я|А-Я|\s]+"))
                    {
                        page.Password = "";
                        page.passwordInputField.Textbox_InputField_UncorrectValueStyle();
                        this.mainViewModel.NotifyUserByRowState(RowStateResourcesName.l_uncorrectPassword);
                    }
                    else 
                    {
                        if (Login.Length != 0 && page.Password.Length != 0)
                        {

                            Task.Factory.StartNew(() =>
                            {
                                RRTemplate response = Client.ServerRequest(new RRTemplate(RRType.Authorization, new AuthtorizationTemplate(Login, page.Password)));
                                switch (response.RRType)
                                {
                                    case RRType.Authorization:

                                        RegistrationTemplate regResponseObj = response.RRObject as RegistrationTemplate;
                                        if (regResponseObj != null)
                                        {
                                            Application.Current.Dispatcher.Invoke((Action)delegate { this.mainViewModel.TryAuthtorizait(); });
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
                            });
                        }
                    }

                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand SignUpPageCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    this.mainViewModel.CurrentPage = new RegistrationPage(this.mainViewModel);
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

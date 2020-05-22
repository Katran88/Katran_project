using Katran.UserControlls;
using Katran.ViewModels;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Katran.Models
{
    public class AdminTab : INotifyPropertyChanged
    {
        private Visibility tabVisibility;
        public Visibility TabVisibility
        {
            get { return tabVisibility; }
            set { tabVisibility = value; OnPropertyChanged(); }
        }

        private Visibility blockUserButtonVisibility;
        public Visibility BlockUserButtonVisibility
        {
            get { return blockUserButtonVisibility; }
            set { blockUserButtonVisibility = value; OnPropertyChanged(); }
        }

        private Visibility unblockUserButtonVisibility;
        public Visibility UnblockUserButtonVisibility
        {
            get { return unblockUserButtonVisibility; }
            set { unblockUserButtonVisibility = value; OnPropertyChanged(); }
        }

        private string searchTextField;

        public string SearchTextField
        {
            get { return searchTextField; }
            set { searchTextField = value; OnPropertyChanged(); }
        }

        ObservableCollection<ContactUI> users;
        public ObservableCollection<ContactUI> Users
        {
            get { return users; }
            set { users = value; OnPropertyChanged(); }
        }

        ContactUI selectedUser;
        public ContactUI SelectedUser
        {
            get { return selectedUser; }
            set
            {
                selectedUser = value;
                OnPropertyChanged();

                if (selectedUser != null)
                {


                    if (SelectedUser.IsBlocked)
                    {
                        UnblockUserButtonVisibility = Visibility.Visible;
                        BlockUserButtonVisibility = Visibility.Hidden;
                    }
                    else
                    {
                        UnblockUserButtonVisibility = Visibility.Hidden;
                        BlockUserButtonVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    UnblockUserButtonVisibility = BlockUserButtonVisibility = Visibility.Hidden;
                }
            }
        }

        private MainPageViewModel mainPageViewModel;
        public MainPageViewModel MainPageViewModel
        {
            get { return mainPageViewModel; }
            set { mainPageViewModel = value; }
        }

        public AdminTab()
        {
            MainPageViewModel = null;
            Users = null;
            SelectedUser = null;
            SearchTextField = "";
            UnblockUserButtonVisibility = BlockUserButtonVisibility = Visibility.Hidden;

            TabVisibility = Visibility.Collapsed;
        }

        public AdminTab(MainPageViewModel mainPageViewModel)
        {
            MainPageViewModel = mainPageViewModel;
            Users = new ObservableCollection<ContactUI>();
            SelectedUser = null;
            SearchTextField = "";
            UnblockUserButtonVisibility = BlockUserButtonVisibility = Visibility.Hidden;
            TabVisibility = Visibility.Collapsed;
        }

        public ICommand BlockUser
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        RRTemplate serverResponse = Client.ServerRequest(new RRTemplate(RRType.BlockUnblockUser, new BlockUnblockUserTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id, true, SelectedUser.ContactID)));

                        if (serverResponse != null)
                        {
                            switch (serverResponse.RRType)
                            {
                                case RRType.BlockUnblockUser:
                                    BlockUnblockUserTemplate bunbUT = serverResponse.RRObject as BlockUnblockUserTemplate;
                                    if (bunbUT != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            foreach (ContactUI user in Users)
                                            {
                                                if (bunbUT.UserId == user.ContactID)
                                                {
                                                    user.IsBlocked = bunbUT.IsBlocked;
                                                    UnblockUserButtonVisibility = Visibility.Hidden;
                                                    BlockUserButtonVisibility = Visibility.Hidden;
                                                    SelectedUser = null;
                                                }
                                            }
                                        }));
                                    }
                                    break;
                                default:
                                    mainPageViewModel.MainViewModel.ErrorService(serverResponse.RRObject as ErrorReportTemplate);
                                    break;
                            }
                        }
                    });
                });
            }
        }

        public ICommand UnblockUser
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        RRTemplate serverResponse = Client.ServerRequest(new RRTemplate(RRType.BlockUnblockUser, new BlockUnblockUserTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id, false, SelectedUser.ContactID)));

                        if (serverResponse != null)
                        {
                            switch (serverResponse.RRType)
                            {
                                case RRType.BlockUnblockUser:
                                    BlockUnblockUserTemplate bunbUT = serverResponse.RRObject as BlockUnblockUserTemplate;
                                    if (bunbUT != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            foreach (ContactUI user in Users)
                                            {
                                                if (bunbUT.UserId == user.ContactID)
                                                {
                                                    user.IsBlocked = bunbUT.IsBlocked;
                                                    UnblockUserButtonVisibility = Visibility.Hidden;
                                                    BlockUserButtonVisibility = Visibility.Hidden;
                                                    SelectedUser = null;
                                                }
                                            }
                                        }));
                                    }
                                    break;
                                default:
                                    mainPageViewModel.MainViewModel.ErrorService(serverResponse.RRObject as ErrorReportTemplate);
                                    break;
                            }
                        }
                    });

                });
            }
        }

        public ICommand SearchButton
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        RRTemplate serverResponse = Client.ServerRequest(new RRTemplate(RRType.AdminSearch, new AdminSearchTemplate(mainPageViewModel.MainViewModel.UserInfo.Info.Id, SearchTextField, null)));

                        if (serverResponse != null)
                        {
                            switch (serverResponse.RRType)
                            {
                                case RRType.AdminSearch:
                                    AdminSearchTemplate admST = serverResponse.RRObject as AdminSearchTemplate;
                                    if (admST != null)
                                    {
                                        List<ContactUI> findedUsers = new List<ContactUI>();

                                        foreach (Contact item in admST.Users)
                                        {
                                            Application.Current.Dispatcher.Invoke(new Action(() =>
                                            {
                                                MemoryStream memoryStream = new MemoryStream(item.AvatarImage);
                                                BitmapImage avatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));

                                                findedUsers.Add(new ContactUI(item.AppName, "", avatar, item.Status, item.UserId, item.ChatId, new ObservableCollection<MessageUI>(), item.IsBlocked));
                                            }
                                            ));
                                        }

                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            Users.Clear();
                                            foreach (ContactUI item in findedUsers)
                                            {
                                                Users.Add(item);
                                            }
                                        }
                                        ));
                                    }
                                    break;
                                default:
                                    mainPageViewModel.MainViewModel.ErrorService(serverResponse.RRObject as ErrorReportTemplate);
                                    break;
                            }
                        }
                    });
                }, (obj) =>
                {
                    return SearchTextField.Length != 0;
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

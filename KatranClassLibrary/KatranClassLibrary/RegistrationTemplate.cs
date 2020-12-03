using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public enum Status
    {
        Offline, Online
    }

    [Serializable]
    public enum LawStatus
    {
        User, Admin
    }


    [Serializable]
    public class RegistrationTemplate : INotifyPropertyChanged
    {
        public const string AuthTokenFileName = "AuthToken.dat";

        private string      login;
        private string      password;
        private int         id;
        private string      app_name;
        private string      email;
        private string      user_discription;
        private byte[]      image;
        private Status      status;
        private LawStatus   lawStatus;
        private bool        isBlocked;

        #region Properties
        public string Login
        {
            get
            {
                return login;
            }

            set
            {
                login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
                OnPropertyChanged();
            }
        }

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        public string App_name
        {
            get
            {
                return app_name;
            }

            set
            {
                app_name = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get
            {
                return email;
            }

            set
            {
                email = value;
                OnPropertyChanged();
            }
        }

        public string User_discription 
        {
            get
            {
                return user_discription;
            }

            set
            {
                if (value == null || value.Length == 0)
                { 
                    user_discription = " "; 
                }
                else
                {
                    user_discription = value;
                }
                OnPropertyChanged();
            }
        }

        public byte[] Image
        {
            get
            {
                return image;
            }

            set
            {
                image = value;
                OnPropertyChanged();
            }
        }

        public Status Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        public LawStatus LawStatus
        {
            get
            {
                return lawStatus;
            }

            set
            {
                lawStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsBlocked
        {
            get
            {
                return isBlocked;
            }

            set
            {
                isBlocked = value;
                OnPropertyChanged();
            }
        }
        #endregion


        private RegistrationTemplate()
        {
            Login = "";
            Password = "";
            Id = -1;
            App_name = "";
            Email = "";
            User_discription = " ";
            Image = null;
            Status = Status.Offline;
            LawStatus = LawStatus.User;
            IsBlocked = false;
        }

        public RegistrationTemplate(int id, string app_name, string email, string user_discription, byte[] image, Status status, LawStatus lawStatus, bool isBlocked, string login = "", string password = "")
        {
            Login = login;

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes_password = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder tempBuilder = new StringBuilder(460);
                for (int i = 0; i < bytes_password.Length; i++)
                {
                    tempBuilder.Append(bytes_password[i].ToString("x2"));
                }
                this.Password = tempBuilder.ToString();
            }

            Id = id;
            App_name = app_name;
            Email = email;
            User_discription = user_discription == null || user_discription.Length == 0 ? " " : user_discription;
            Image = image;
            Status = status;
            LawStatus = lawStatus;
            IsBlocked = isBlocked;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}

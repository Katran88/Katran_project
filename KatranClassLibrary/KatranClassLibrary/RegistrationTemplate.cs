using System;
using System.Collections.Generic;
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
    public class RegistrationTemplate
    {
        public const string AuthTokenFileName = "AuthToken.dat";

        public int Id;
        public string App_name;
        public string Email;
        public string User_discription;
        public byte[] Image;
        public Status Status;
        public LawStatus LawStatus;

        private RegistrationTemplate()
        {
            Id = -1;
            App_name = "";
            Email = "";
            User_discription = "";
            Image = null;
            Status = Status.Offline;
            LawStatus = LawStatus.User;
        }

        public RegistrationTemplate(int id, string app_name, string email, string user_discription, byte[] image, Status status, LawStatus lawStatus)
        {
            Id = id;
            App_name = app_name;
            Email = email;
            User_discription = user_discription;
            Image = image;
            Status = status;
            LawStatus = lawStatus;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class Contact
    {
        public int UserId;
        public byte[] AvatarImage;
        public string AppName;
        public Status Status;

        public Contact()
        {
            UserId = -1;
            AvatarImage = null;
            AppName = "";
            Status = Status.Offline;
        }

        public Contact(int userId, byte[] avatarImage, string appName, Status status)
        {
            UserId = userId;
            AvatarImage = avatarImage;
            AppName = appName;
            Status = status;
        }
    }
}

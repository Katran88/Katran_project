using System;
using System.Collections.Generic;

namespace KatranClassLibrary
{
    [Serializable]
    public class Contact
    {
        public int UserId;
        public byte[] AvatarImage;
        public string AppName;
        public Status Status;
        public List<Message> Messages;

        public Contact()
        {
            UserId = -1;
            AvatarImage = null;
            AppName = "";
            Status = Status.Offline;
            Messages = null;
        }

        public Contact(int userId, byte[] avatarImage, string appName, Status status, List<Message> messages)
        {
            UserId = userId;
            AvatarImage = avatarImage;
            AppName = appName;
            Status = status;
            Messages = messages;
        }
    }
}

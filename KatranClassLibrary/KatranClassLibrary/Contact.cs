﻿using System;
using System.Collections.Generic;

namespace KatranClassLibrary
{
    [Serializable]
    public enum ContactType
    {
        Chat,
        Conversation
    }

    [Serializable]
    public class Contact
    {
        public int UserId;
        public byte[] AvatarImage;
        public string AppName;
        public Status Status;
        public int ChatId;
        public List<Message> Messages;
        public ContactType ContactType;
        public List<Contact> Members;


        public Contact()
        {
            UserId = -1;
            AvatarImage = null;
            AppName = "";
            Status = Status.Offline;
            ChatId = -1;
            Messages = null;
            ContactType = ContactType.Chat;
            Members = null;
        }

        public Contact(int userId, byte[] avatarImage, string appName, Status status, int chatId, List<Message> messages, ContactType contactType, List<Contact> members = null)
        {
            UserId = userId;
            AvatarImage = avatarImage;
            AppName = appName;
            Status = status;
            ChatId = chatId;
            Messages = messages;
            ContactType = contactType;
            Members = members;
        }
    }
}

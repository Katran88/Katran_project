﻿using System;
using System.Collections.Generic;

namespace KatranClassLibrary
{
    [Serializable]
    public enum MessageType
    {
        File,
        Text
    }

    [Serializable]
    public enum MessageState
    {
        Readed,  Unreaded,
        Sended,  Unsended
    }

    [Serializable]
    public class Message
    {
        public int MessageID;
        public int SenderID;
        public byte[] MessageBody;
        public MessageType MessageType;
        public string FileName;
        public string FileSize;
        public DateTime Time;
        public MessageState MessageState;

        public Message()
        {
            MessageID = -1;
            SenderID = -1;
            MessageBody = null;
            MessageType = MessageType.Text;
            FileName = "";
            FileSize = "";
            MessageState = MessageState.Unsended;
        }

        public Message(int messageID, int senderID, byte[] messageBody, MessageType messageType, string fileName, string fileSize, DateTime time, MessageState messageState)
        {
            MessageID = messageID;
            SenderID = senderID;
            MessageBody = messageBody;
            MessageType = messageType;
            FileName = fileName;
            FileSize = fileSize;
            Time = time;
            MessageState = messageState;
        }
    }
}
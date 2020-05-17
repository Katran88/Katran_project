using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class DownloadFileTemplate
    {
        public int ChatId;
        public int messageId;
        public Message Message;

        public DownloadFileTemplate()
        {
            ChatId = -1;
            messageId = -1;
            Message = null;
        }

        public DownloadFileTemplate(int chatId, int messageId, Message message)
        {
            ChatId = chatId;
            this.messageId = messageId;
            Message = message;
        }
    }
}

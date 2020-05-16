using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class SendMessageTemplate
    {
        public int ReceiverChatID;
        public Message Message;

        public SendMessageTemplate()
        {
            ReceiverChatID = -1;
            Message = null;
        }

        public SendMessageTemplate(int receiverChatID, Message message)
        {
            ReceiverChatID = receiverChatID;
            Message = message;
        }
    }
}

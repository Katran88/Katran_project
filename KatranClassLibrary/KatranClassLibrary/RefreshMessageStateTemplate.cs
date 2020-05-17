using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class RefreshMessageStateTemplate
    {
        public int ChatId;
        public int messageId;
        public MessageState MessageState;

        public RefreshMessageStateTemplate()
        {
            ChatId = -1;
            messageId = -1;
            MessageState = MessageState.Unreaded;
        }

        public RefreshMessageStateTemplate(int chatId, int messageId, MessageState messageState)
        {
            ChatId = chatId;
            this.messageId = messageId;
            MessageState = messageState;
        }
    }
}

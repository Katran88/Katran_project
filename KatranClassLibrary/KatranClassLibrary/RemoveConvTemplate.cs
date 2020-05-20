using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class RemoveConvTemplate
    {
        public int OwnerId;
        public int ChatId;

        public RemoveConvTemplate()
        {
            OwnerId = -1;
            ChatId = -1;
        }

        public RemoveConvTemplate(int ownerId, int chatId)
        {
            OwnerId = ownerId;
            ChatId = chatId;
        }
    }
}

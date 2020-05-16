using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class AddRemoveContactTemplate
    {
        public int ContactOwnerId;
        public int TargetContactId;
        public int ChatId;

        private AddRemoveContactTemplate()
        {
            ContactOwnerId = -1;
            TargetContactId = -1;
            ChatId = -1;
        }

        public AddRemoveContactTemplate(int userId, int targetContactId, int chatId)
        {
            ContactOwnerId = userId;
            TargetContactId = targetContactId;
            ChatId = chatId;
        }
    }
}

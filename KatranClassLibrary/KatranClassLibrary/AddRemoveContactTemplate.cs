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

        private AddRemoveContactTemplate()
        {
            ContactOwnerId = -1;
            TargetContactId = -1;
        }

        public AddRemoveContactTemplate(int userId, int targetContactId)
        {
            ContactOwnerId = userId;
            TargetContactId = targetContactId;
        }
    }
}

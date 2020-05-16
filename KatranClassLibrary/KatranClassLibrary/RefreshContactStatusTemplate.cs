using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class RefreshContactStatusTemplate
    {
        public int ContactId;
        public Status NewContactStatus;

        public RefreshContactStatusTemplate()
        {
            ContactId = -1;
            NewContactStatus = Status.Offline;
        }

        public RefreshContactStatusTemplate(int contactId, Status newContactStatus)
        {
            ContactId = contactId;
            NewContactStatus = newContactStatus;
        }
    }
}

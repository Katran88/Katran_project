using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class RefreshContactsTemplate
    {
        public int ContactsOwner;
        public List<Contact> Contacts;

        private RefreshContactsTemplate()
        {
            ContactsOwner = -1;
            Contacts = null;
        }

        public RefreshContactsTemplate(int userId, List<Contact> contacts)
        {
            ContactsOwner = userId;
            Contacts = contacts;
        }
    }
}

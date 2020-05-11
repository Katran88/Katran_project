using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class SearchOutContactsTemplate
    {
        public int ContactsOwner;
        public string SearchPattern;
        public List<Contact> Contacts;

        private SearchOutContactsTemplate()
        {
            ContactsOwner = -1;
            SearchPattern = "";
            Contacts = null;
        }

        public SearchOutContactsTemplate(int userId, string searchPattern, List<Contact> contacts)
        {
            ContactsOwner = userId;
            SearchPattern = searchPattern;
            Contacts = contacts;
        }
    }
}

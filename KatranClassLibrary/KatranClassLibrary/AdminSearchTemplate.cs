using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class AdminSearchTemplate
    {
        public int AdminId;
        public string Pattern;
        public List<Contact> Users;

        public AdminSearchTemplate()
        {
            AdminId = -1;
            Pattern = "";
            Users = null;
        }

        public AdminSearchTemplate(int adminId, string pattern, List<Contact> users)
        {
            AdminId = adminId;
            Pattern = pattern;
            Users = users;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class CheckUserNameTemplate
    {
        public int UserId;
        public string UserName;

        public CheckUserNameTemplate()
        {
            UserId = -1;
            UserName = "";
        }

        public CheckUserNameTemplate(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }
}

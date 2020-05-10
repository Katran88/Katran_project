using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class RefreshUserTemplate
    {
        public int UserId;

        private RefreshUserTemplate()
        {
            UserId = -1;
        }

        public RefreshUserTemplate(int userId)
        {
            UserId = userId;
        }
    }
}

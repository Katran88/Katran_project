using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class BlockUnblockUserTemplate
    {
        public int AdminId;
        public bool IsBlocked;
        public int UserId;

        public BlockUnblockUserTemplate()
        {
            AdminId = -1;
            IsBlocked = false;
            UserId = -1;
        }

        public BlockUnblockUserTemplate(int adminId, bool isBlocked, int userId)
        {
            AdminId = adminId;
            IsBlocked = isBlocked;
            UserId = userId;
        }
    }
}

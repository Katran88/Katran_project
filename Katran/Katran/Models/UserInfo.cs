using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katran.Models
{
    class UserInfo
    {
        RegistrationTemplate info;

        public RegistrationTemplate Info 
        {
            get 
            {
                return info;
            }
            set
            {
                info = value;
            }
        }

        public UserInfo(RegistrationTemplate info)
        {
            this.Info = info;
        }
    }
}

using KatranClassLibrary;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Katran.Models
{
    public class UserInfo : INotifyPropertyChanged
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
                OnPropertyChanged();
            }
        }

        public UserInfo(RegistrationTemplate info)
        {
            this.Info = info;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

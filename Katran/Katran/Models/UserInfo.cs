using KatranClassLibrary;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

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
                if (info != null)
                {
                    MemoryStream memoryStream = new MemoryStream(info.Image);
                    Avatar = Converters.BitmapToImageSource(new Bitmap(memoryStream));
                }
                

                OnPropertyChanged();
            }
        }
        BitmapImage avatar;
        public BitmapImage Avatar
        {
            get
            {
                return avatar;
            }
            set
            {
                avatar = value;
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

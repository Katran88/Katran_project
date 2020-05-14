using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для Contact.xaml
    /// </summary>
    public partial class ContactUI : UserControl, INotifyPropertyChanged
    {
        public string ContactUsername
        {
            get { return (string)GetValue(ContactUsernameProperty); }
            set { SetValue(ContactUsernameProperty, value); }
        }
        public static readonly DependencyProperty ContactUsernameProperty =
            DependencyProperty.Register("ContactUsername", typeof(string), typeof(ContactUI),
                                        new FrameworkPropertyMetadata("Username", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public string ContactLastMessage
        {
            get { return (string)GetValue(ContactLastMessageProperty); }
            set { SetValue(ContactLastMessageProperty, value); }
        }
        public static readonly DependencyProperty ContactLastMessageProperty =
            DependencyProperty.Register("ContactLastMessage", typeof(string), typeof(ContactUI),
                                        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public BitmapImage ContactAvatar
        {
            get { return (BitmapImage)GetValue(ContactAvatarProperty); }
            set { SetValue(ContactAvatarProperty, value); }
        }
        public static readonly DependencyProperty ContactAvatarProperty =
            DependencyProperty.Register("ContactAvatar", typeof(BitmapImage), typeof(ContactUI),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));


        private int contactID;
        public int ContactID
        {
            get { return contactID; }
            set { contactID = value; }
        }


        private Status contactStatus;
        public Status ContactStatus
        {
            get { return contactStatus; }
            set 
            { 
                contactStatus = value;

                switch (value)
                {
                    case Status.Online:
                        ContactStatusShower.Fill = (System.Windows.Media.Brush)Application.Current.FindResource("OnlineBrush");
                        break;
                    case Status.Offline:
                    default:
                        ContactStatusShower.Fill = (System.Windows.Media.Brush)Application.Current.FindResource("OfflineBrush");
                        break;
                }

            }
        }

        private ObservableCollection<MessageUI> contactMessages;
        public ObservableCollection<MessageUI> ContactMessages
        {
            get { return contactMessages; }
            set { contactMessages = value; OnPropertyChanged(); }
        }


        public ContactUI()
        {
            InitializeComponent();

            ContactUsername = "";
            ContactLastMessage = "";
            ContactAvatar = null;
            ContactStatus = Status.Offline;
            ContactID = -1;
            ContactMessages = null;
        }

        public ContactUI(string contactUsername, string contactLasMessage, BitmapImage bitmapImage, Status contactStatus, int contactID, ObservableCollection<MessageUI> contactMessages)
        {
            InitializeComponent();

            ContactUsername = contactUsername;
            ContactLastMessage = contactLasMessage;
            ContactAvatar = bitmapImage;
            ContactStatus = contactStatus;
            ContactID = contactID;
            ContactMessages = contactMessages;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

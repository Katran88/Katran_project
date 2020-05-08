using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    public partial class Contact : UserControl
    {
        public string ContactUsername
        {
            get { return (string)GetValue(ContactUsernameProperty); }
            set { SetValue(ContactUsernameProperty, value); }
        }
        public static readonly DependencyProperty ContactUsernameProperty =
            DependencyProperty.Register("ContactUsername", typeof(string), typeof(Contact),
                                        new FrameworkPropertyMetadata("Username", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public string ContactLastMessage
        {
            get { return (string)GetValue(ContactLastMessageProperty); }
            set { SetValue(ContactLastMessageProperty, value); }
        }
        public static readonly DependencyProperty ContactLastMessageProperty =
            DependencyProperty.Register("ContactLastMessage", typeof(string), typeof(Contact),
                                        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public BitmapImage ContactAvatar
        {
            get { return (BitmapImage)GetValue(ContactAvatarProperty); }
            set { SetValue(ContactAvatarProperty, value); }
        }
        public static readonly DependencyProperty ContactAvatarProperty =
            DependencyProperty.Register("ContactAvatar", typeof(BitmapImage), typeof(Contact),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Contact()
        {
            InitializeComponent();

            ContactUsername = "";
            ContactLastMessage = "";
            ContactAvatar = null;
        }

        public Contact(string contactUsername, string contactLasMessage, BitmapImage bitmapImage)
        {
            InitializeComponent();

            ContactUsername = contactUsername;
            ContactLastMessage = contactLasMessage;
            ContactAvatar = bitmapImage;
        }
    }
}

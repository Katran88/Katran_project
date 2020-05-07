using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для InputField.xaml
    /// </summary>
    public partial class CryptInputField : UserControl
    {
        public string FieldName
        {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register("FieldName", typeof(string), typeof(CryptInputField),
                                        new FrameworkPropertyMetadata("Field Name:", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public double InputFieldWidth
        {
            get { return passwordbox_InputField.Width; }
            set { passwordbox_InputField.Width = value; }
        }

        public CryptInputField()
        {
            InitializeComponent();
            passwordbox_InputField.GotFocus += Textbox_InputField_GotFocus;
            passwordbox_InputField.LostFocus += Textbox_InputField_LostFocus;
        }

        public string Password
        {
            get { return passwordbox_InputField.Password; }
            set { passwordbox_InputField.Password = value; }
        }

        public void Textbox_InputField_LostFocus(object sender, RoutedEventArgs e)
        {
            Underline.Style = (Style)Application.Current.FindResource("Underline_LostFocus");
        }

        public void Textbox_InputField_GotFocus(object sender, RoutedEventArgs e)
        {
            Underline.Style = (Style)Application.Current.FindResource("Underline_GotFocus");
        }

        public void Textbox_InputField_UncorrectValueStyle()
        {
            Underline.Style = (Style)Application.Current.FindResource("Underline_Uncorrect");
        }

        private void textbox_InputField_MouseEnter(object sender, MouseEventArgs e)
        {
            Textbox_InputField_GotFocus(sender, e);
        }

        private void textbox_InputField_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!passwordbox_InputField.IsFocused)
            {
                Textbox_InputField_LostFocus(sender, e);
            }
        }
    }
}

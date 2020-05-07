using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для EmailInputField.xaml
    /// </summary>
    public partial class EmailInputField : UserControl
    {
        static string pattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";

        public string FieldName
        {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register("FieldName", typeof(string), typeof(EmailInputField),
                                        new FrameworkPropertyMetadata("Field Name:", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public double InputFieldWidth
        {
            get { return textbox_InputField.Width; }
            set { textbox_InputField.Width = value; }
        }

        public double InputFieldHeight
        {
            get { return textbox_InputField.Height; }
            set { textbox_InputField.Height = value; }
        }

        public string InputFieldBind
        {
            get { return (string)GetValue(InputFieldBindProperty); }
            set { SetValue(InputFieldBindProperty, value); }
        }
        public static readonly DependencyProperty InputFieldBindProperty =
            DependencyProperty.Register("InputFieldBind", typeof(string), typeof(EmailInputField),
                                        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, null, new CoerceValueCallback(CorrectValue)),
                                        new ValidateValueCallback(ValidateValue));

        private static bool ValidateValue(object value)
        {
            return true;
        }

        private static object CorrectValue(DependencyObject d, object baseValue)
        {
            return Regex.Match((string)baseValue, pattern, RegexOptions.IgnoreCase).Value;
        }

        public EmailInputField()
        {
            InitializeComponent();
            textbox_InputField.GotFocus += Textbox_InputField_GotFocus;
            textbox_InputField.LostFocus += Textbox_InputField_LostFocus;
        }

        private void Textbox_InputField_LostFocus(object sender, RoutedEventArgs e)
        {
            Underline.Style = (Style)Application.Current.FindResource("Underline_LostFocus");
        }

        private void Textbox_InputField_GotFocus(object sender, RoutedEventArgs e)
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
            if (!textbox_InputField.IsFocused)
            {
                Textbox_InputField_LostFocus(sender, e);
            }
        }
    }
}

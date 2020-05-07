using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для InputField.xaml
    /// </summary>
    public partial class LoginField : UserControl
    {
        public string FieldName
        {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register("FieldName", typeof(string), typeof(LoginField),
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

        public int InputFieldMaxLength
        {
            get { return textbox_InputField.MaxLength; }
            set { textbox_InputField.MaxLength = value; }
        }

        public string InputFieldBind
        {
            get { return (string)GetValue(InputFieldBindProperty); }
            set { SetValue(InputFieldBindProperty, value); }
        }
        public static readonly DependencyProperty InputFieldBindProperty = 
            DependencyProperty.Register("InputFieldBind", typeof(string), typeof(LoginField), 
                                        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, null, new CoerceValueCallback(CorrectValue)),
                                        new ValidateValueCallback(ValidateValue));

        private static bool ValidateValue(object value)
        {
           return true;
        }

        private static object CorrectValue(DependencyObject d, object baseValue)
        {
            string currentValue = Regex.Replace((string)baseValue, @"[а-я|А-Я]+", "");
            return Regex.Match(currentValue, @"[\w|@|_|\.]+").Value;   
        }

        public LoginField()
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

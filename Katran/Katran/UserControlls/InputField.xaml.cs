using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для InputField.xaml
    /// </summary>
    public partial class InputField : UserControl
    {
        public string FieldName
        {
            get { return textblock_FieldName.Text; }
            set { textblock_FieldName.Text = value; }
        }

        public double InputFieldWidth
        {
            get { return textbox_InputField.Width; }
            set 
            {
                textbox_InputField.Width = value;
            }
        }

        public InputField()
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

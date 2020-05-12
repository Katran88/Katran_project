using Katran.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Katran.Views
{
    /// <summary>
    /// Логика взаимодействия для DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {

        private string confirmText;
        public string ConfirmText
        {
            get { return confirmText; }
            set { confirmText = value; OnPropertyChanged(); }
        }

        public ICommand ConfirmCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    DialogResult = true;
                    Close();
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public ICommand RejectCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    DialogResult = false;
                    Close();
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public DialogWindow(string confirmText)
        {
            InitializeComponent();
            ConfirmText = confirmText;
            ConfirmTextField.Text = ConfirmText;
            ConfirmButton.Command = ConfirmCommand;
            RejectButton.Command = RejectCommand;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

using Katran.ViewModels;
using System.Windows.Controls;

namespace Katran.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new RegPageViewModel(mainViewModel, this);
        }

        internal string Password_1
        {
            get { return passwordInputField_1.Password; }
            set { passwordInputField_1.Password = value; }
        }

        internal string Password_2
        {
            get { return passwordInputField_2.Password; }
            set { passwordInputField_2.Password = value; }
        }
    }
}

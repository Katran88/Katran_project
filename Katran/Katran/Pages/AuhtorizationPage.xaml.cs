using Katran.ViewModels;
using System.Windows.Controls;

namespace Katran.Pages
{
    public partial class AuhtorizationPage : Page
    {
        public AuhtorizationPage(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new AuthPageViewModel(mainViewModel, this);
        }

        internal string Password
        {
            get { return passwordInputField.Password; }
            set { passwordInputField.Password = value; }
        }
    }
}

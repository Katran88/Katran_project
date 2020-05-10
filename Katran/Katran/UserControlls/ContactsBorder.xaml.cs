using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для ContactsBorder.xaml
    /// </summary>
    public partial class ContactsBorder : UserControl, INotifyPropertyChanged
    {
        private Visibility ucVisibility;

        public Visibility UC_Visibility
        {
            get { return ucVisibility; }
            set { ucVisibility = value; OnPropertyChanged(); }
        }


        public ICommand SearchOutContactsBind
        {
            get { return (ICommand)GetValue(SearchOutContactsBindProperty); }
            set { SetValue(SearchOutContactsBindProperty, value); }
        }
        public static readonly DependencyProperty SearchOutContactsBindProperty =
            DependencyProperty.Register("SearchOutContactsBind", typeof(ICommand), typeof(ContactsBorder),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public ContactsBorder()
        {
            InitializeComponent();
            SearchOutContactsBind = null;
            UC_Visibility = Visibility.Visible;
        }

        public ContactsBorder(ICommand searchOutContactsBind)
        {
            SearchOutContactsBind = searchOutContactsBind;
            UC_Visibility = Visibility.Visible;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}

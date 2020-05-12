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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Katran.UserControlls
{
    /// <summary>
    /// Логика взаимодействия для ContactsSearcher.xaml
    /// </summary>
    public partial class ContactsSearcher : UserControl, INotifyPropertyChanged
    {
        public string SearchFieldText
        {
            get { return (string)GetValue(SearchFieldTextProperty); }
            set { SetValue(SearchFieldTextProperty, value); }
        }
        public static readonly DependencyProperty SearchFieldTextProperty =
            DependencyProperty.Register("SearchFieldText", typeof(string), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public ICommand RemoveContactButtonBind
        {
            get { return (ICommand)GetValue(RemoveContactButtonBindProperty); }
            set { SetValue(RemoveContactButtonBindProperty, value); }
        }
        public static readonly DependencyProperty RemoveContactButtonBindProperty =
            DependencyProperty.Register("RemoveContactButtonBind", typeof(ICommand), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Visibility RemoveContact_ButtonVisibility
        {
            get { return (Visibility)GetValue(RemoveContact_ButtonVisibilityProperty); }
            set { SetValue(RemoveContact_ButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty RemoveContact_ButtonVisibilityProperty =
            DependencyProperty.Register("RemoveContact_ButtonVisibility", typeof(Visibility), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(Visibility.Hidden, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));


        public ICommand AddContactButtonBind
        {
            get { return (ICommand)GetValue(AddContactButtonBindProperty); }
            set { SetValue(AddContactButtonBindProperty, value); }
        }
        public static readonly DependencyProperty AddContactButtonBindProperty =
            DependencyProperty.Register("AddContactButtonBind", typeof(ICommand), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Visibility AddContact_ButtonVisibility
        {
            get { return (Visibility)GetValue(AddContact_ButtonVisibilityProperty); }
            set { SetValue(AddContact_ButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty AddContact_ButtonVisibilityProperty =
            DependencyProperty.Register("AddContact_ButtonVisibility", typeof(Visibility), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(Visibility.Hidden, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public ICommand SearchButtonBind
        {
            get { return (ICommand)GetValue(SearchButtonBindProperty); }
            set { SetValue(SearchButtonBindProperty, value); }
        }
        public static readonly DependencyProperty SearchButtonBindProperty =
            DependencyProperty.Register("SearchButtonBind", typeof(ICommand), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public ContactsSearcher()
        {
            InitializeComponent();
            SearchFieldText = "";
            SearchButtonBind = null;
            AddContact_ButtonVisibility = RemoveContact_ButtonVisibility = Visibility.Hidden;
        }

        public ContactsSearcher(string searchFieldText, ICommand searchButtonBind)
        {
            InitializeComponent();
            SearchFieldText = searchFieldText;
            SearchButtonBind = searchButtonBind;
            AddContact_ButtonVisibility = RemoveContact_ButtonVisibility = Visibility.Hidden;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

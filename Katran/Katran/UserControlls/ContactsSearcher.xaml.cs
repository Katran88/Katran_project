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
    /// Логика взаимодействия для ContactsSearcher.xaml
    /// </summary>
    public partial class ContactsSearcher : UserControl
    {
        public string SearchFieldText
        {
            get { return (string)GetValue(SearchFieldTextProperty); }
            set { SetValue(SearchFieldTextProperty, value); }
        }
        public static readonly DependencyProperty SearchFieldTextProperty =
            DependencyProperty.Register("SearchFieldText", typeof(string), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public ICommand SearchButtonBind
        {
            get { return (ICommand)GetValue(SearchButtonBindProperty); }
            set { SetValue(SearchButtonBindProperty, value); }
        }
        public static readonly DependencyProperty SearchButtonBindProperty =
            DependencyProperty.Register("SearchButtonBind", typeof(ICommand), typeof(ContactsSearcher),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        private ContactsSearcher()
        {
            InitializeComponent();
            SearchFieldText = "";
            SearchButtonBind = null;
        }

        public ContactsSearcher(string searchFieldText, ICommand searchButtonBind)
        {
            InitializeComponent();
            SearchFieldText = searchFieldText;
            SearchButtonBind = searchButtonBind;
        }
    }
}

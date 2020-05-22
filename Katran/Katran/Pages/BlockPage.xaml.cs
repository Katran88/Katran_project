using Katran.ViewModels;
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

namespace Katran.Pages
{
    /// <summary>
    /// Логика взаимодействия для BlockPage.xaml
    /// </summary>
    public partial class BlockPage : Page
    {
        public BlockPage(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new BlockPageViewModel(mainViewModel, this);
        }
    }
}

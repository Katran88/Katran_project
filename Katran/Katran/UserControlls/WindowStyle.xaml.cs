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
    /// Логика взаимодействия для WindowStyle.xaml
    /// </summary>
    public partial class WindowStyle : UserControl
    {
        public ControlTemplate ButtonWindowExpand { get; private set; }
        public ControlTemplate ButtonWindowExpand2 { get; private set; }

        public WindowStyle()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.Close();
        }

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            object windowStyle = parentWindow.FindName("WindowStyle");
            if (parentWindow.WindowState == WindowState.Maximized)
            {
                parentWindow.WindowState = WindowState.Normal;
                parentWindow.Height = 500;
                Expand.Template = (ControlTemplate)Application.Current.FindResource("ButtonWindowExpand");
                (windowStyle as WindowStyle).Padding = new Thickness(0, 0, 0, 0);
                (windowStyle as WindowStyle).Height = 20;
            }
            else
            {
                parentWindow.WindowState = WindowState.Maximized;
                Expand.Template = (ControlTemplate)Application.Current.FindResource("ButtonWindowExpand_2");
                (windowStyle as WindowStyle).Padding = new Thickness(0, 2, 2, 2);
                (windowStyle as WindowStyle).Height = 25;
            }
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.WindowState = WindowState.Minimized;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.DragMove();
        }
    }
}

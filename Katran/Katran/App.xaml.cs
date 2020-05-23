using Katran.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Katran
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

                ResourceDictionary dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case "ru":
                        dict.Source = new Uri(@"Resources\Dictionary_RU.xaml", UriKind.Relative);
                        break;
                    case "en":
                    default:
                        dict.Source = new Uri(@"Resources\Dictionary_EN.xaml", UriKind.Relative);
                        break;
                }

                //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.StartsWith(@"Resources\Dictionary_")
                                              select d).First();
                if (oldDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                System.Threading.Thread.CurrentThread.CurrentUICulture = value;
            }


        }

        public static Settings.Theme Theme
        {
            set
            {
                ResourceDictionary myDict = new ResourceDictionary();
                ResourceDictionary matDict = new ResourceDictionary();
                switch (value)
                {
                    case Settings.Theme.Dark:
                        myDict.Source = new Uri(@"Styles\DarkTheme.xaml", UriKind.Relative);
                        matDict.Source = new Uri(@"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml", UriKind.Absolute);
                        break;
                    case Settings.Theme.Light:
                    default:
                        myDict.Source = new Uri(@"Styles\LightTheme.xaml", UriKind.Relative);
                        matDict.Source = new Uri(@"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml", UriKind.Absolute);
                        break;
                }

                
                ResourceDictionary oldMyDict = (from d in Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.EndsWith(@"Theme.xaml")
                                              select d).First();
                if (oldMyDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldMyDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldMyDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, myDict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(myDict);
                }

                ResourceDictionary oldMatDict = (from d in Application.Current.Resources.MergedDictionaries
                                                where d.Source != null && d.Source.OriginalString.StartsWith(@"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.")
                                                select d).First();
                if (oldMatDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldMatDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldMatDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, matDict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(matDict);
                }
            }


        }
    }
}

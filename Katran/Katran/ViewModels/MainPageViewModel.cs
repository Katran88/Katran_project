using Katran.Models;
using Katran.Pages;
using Katran.UserControlls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        MainViewModel mainViewModel;
        MainPage mainPage;

        ObservableCollection<Contact> contacts;
        public ObservableCollection<Contact> Contacts
        {
            get { return contacts; }
            set { contacts = value; OnPropertyChanged(); }
        }

        private ContentPresenter menuContentPresenter;
        public ContentPresenter MenuContentPresenter
        {
            get { return menuContentPresenter; }
            set { menuContentPresenter = value; OnPropertyChanged(); }
        }

        private ContentPresenter messagesContentPresenter;
        public ContentPresenter MessagesContentPresenter
        {
            get { return messagesContentPresenter; }
            set { messagesContentPresenter = value; OnPropertyChanged(); }
        }

        public MainPageViewModel()
        {
            this.mainViewModel = null;
            this.mainPage = null;
            MenuContentPresenter = new ContentPresenter();
            MessagesContentPresenter = new ContentPresenter();
        }

        public MainPageViewModel(MainViewModel mainViewModel, MainPage mainPage)
        {
            this.mainViewModel = mainViewModel;
            this.mainPage = mainPage;
            MenuContentPresenter = new ContentPresenter();
            MessagesContentPresenter = new ContentPresenter();

            Bitmap avatar = new Bitmap(@"G:\OneDrive\учёба\4-й_семестр\курсовой\Katran_project\KatranServer\KatranServer\Resources\defaultUserImage.png");

            Contacts = new ObservableCollection<Contact>()
            {
                new Contact("ячсячсячсчссчсчссссс", "Hello1", Converters.BitmapToImageSource(avatar)),
                new Contact("Pupa2", "Hello2", Converters.BitmapToImageSource(avatar)),
                new Contact("Pupa3", "Hello3", Converters.BitmapToImageSource(avatar)),
                new Contact("Pupa4", "Hello4", Converters.BitmapToImageSource(avatar))
            };
        }

        public ICommand ContactsSelected
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    MessageBox.Show("Hello");
                    MenuContentPresenter.Content = new ListView();
                    ((ListView)MenuContentPresenter.Content).ItemsSource = Contacts;

                    Contacts[1].ContactUsername = "asdas";
                }/*вторым параметром можно задать функцию-условие, которая возвращает булевое значение для доступности кнопки(во вью достаточно просто забиндить команду)*/);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

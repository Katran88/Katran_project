using Katran.Pages;
using KatranClassLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Katran.ViewModels
{
    public class BlockPageViewModel : INotifyPropertyChanged
    {
        private MainViewModel mainViewModel;
        private BlockPage page;

        public BlockPageViewModel()
        {
            this.mainViewModel = null;
            this.page = null;
        }

        public BlockPageViewModel(MainViewModel mainViewModel, BlockPage page)
        {
            this.mainViewModel = mainViewModel;
            this.page = page;
        }

        public ICommand OpenAuthPage
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    File.Delete(RegistrationTemplate.AuthTokenFileName);
                    mainViewModel.CurrentPage = new AuhtorizationPage(mainViewModel);
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

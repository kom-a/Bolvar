using Bolvar.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace Bolvar.Models
{
    class SearchOptionsModel : INotifyPropertyChanged
    {
        public bool CaseSensitive { get; set; }
        public bool IncludeFilesWithoutMatches { get; set; }

        public ICommand OkCommand { get; set; }

        public SearchOptionsModel()
        {
            OkCommand = new RelayCommand(o => ((SearchOptionsWindow)o).Close());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace Bolvar.Models
{
    public class DirectoryOptionsModel : INotifyPropertyChanged
    {
        public String ExcludeDir { get; set; }
        public String FileMask { get; set; }
        public String ExcludeMask { get; set; }
        public bool IncludeSubDirectories { get; set; }

        public ICommand OkCommand { get; set; }

        public DirectoryOptionsModel()
        {
            OkCommand = new RelayCommand(o => ((DirectoryOptionsWindow)o).Close());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Input;
using Bolvar.Models;
using Bolvar.Views;
using Ookii.Dialogs.Wpf;

namespace Bolvar
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string m_RootDirectory;
        private DirectoryOptionsModel m_DirectoryOptions;
        private SearchOptionsModel m_SearchOptions;
        private MainWindow m_OwnerWindow;
        private ObservableCollection<FileMatch> m_FileMatches;
        private string m_FindText;
        private string m_ReplaceText;
        private string m_ConsoleText;


        public ICommand ChooseDirectoryCommand { get; set; }
        public ICommand DirectoryOptionsCommand { get; set; }
        public ICommand FindCommand { get; set; }
        public ICommand SearchOptionsCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }

        public MainViewModel(MainWindow ownerWindow)
        {
            m_OwnerWindow = ownerWindow;
            FileMatches = new ObservableCollection<FileMatch>();
            m_ConsoleText = "";

            ChooseDirectoryCommand = new RelayCommand(o => ChooseDirectoryClick());
            DirectoryOptionsCommand = new RelayCommand(o => DirectoryOptionsClick());
            FindCommand = new RelayCommand(o => FindClick());
            SearchOptionsCommand = new RelayCommand(o => SearchOptionsClick());
            ReplaceCommand = new RelayCommand(o => ReplaceClick());

            m_DirectoryOptions = new DirectoryOptionsModel() {
                IncludeSubDirectories = true,
                ExcludeDir = "",
                FileMask = "*.*",
                ExcludeMask = ""
            };

            m_SearchOptions = new SearchOptionsModel() {
                CaseSensitive = true,
                IncludeFilesWithoutMatches = false
            };

        }

        private void ChooseDirectoryClick(object sender = null)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                RootDirectory = dialog.SelectedPath;
            }
        }

        private void DirectoryOptionsClick(object sender = null)
        {
            DirectoryOptionsWindow directoryOptionsWindow = new DirectoryOptionsWindow();
            directoryOptionsWindow.DataContext = m_DirectoryOptions;
            directoryOptionsWindow.Owner = m_OwnerWindow;
            directoryOptionsWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            
            directoryOptionsWindow.ShowDialog();
        }

        private bool ValidateFields()
        {
            bool success = true;
            ConsoleText = "";

            if(!Directory.Exists(RootDirectory))
            {
                ConsoleText = "Error: Directory does not exists \"" + RootDirectory + "\"";
                success = false;
            }

            return success;
        }

        private IEnumerable<string> GetAllFilenames()
        {
            EnumerationOptions options = new EnumerationOptions()
            {
                IgnoreInaccessible = false,
                RecurseSubdirectories = m_DirectoryOptions.IncludeSubDirectories,
                ReturnSpecialDirectories = false,
            };

            return Directory.GetFiles(RootDirectory, m_DirectoryOptions.FileMask, options);
        }

        private void FindClick()
        {
            FileMatches.Clear();

            if (!ValidateFields())
                return;

            IEnumerable<string> filenames = GetAllFilenames();

            foreach (string filename in filenames)
            {

                using (StreamReader sr = new StreamReader(filename))
                {
                    string fileContents = sr.ReadToEnd();

                    if (fileContents.Contains(FindText))
                    {
                        FileMatches.Add(new FileMatch()
                        {
                            Filename = filename,
                            Matches = 1
                        });
                    }
                }
            }

            if (FileMatches.Count == 0)
            {
                ConsoleText = "There are no any matches.";
            }

            
        }

        private void SearchOptionsClick(object sender = null)
        {
            SearchOptionsWindow searchOptionsWindow = new SearchOptionsWindow();
            searchOptionsWindow.DataContext = m_SearchOptions;
            searchOptionsWindow.Owner = m_OwnerWindow;
            searchOptionsWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            searchOptionsWindow.ShowDialog();
        }

        private void ReplaceClick(object sender = null)
        {
            
        }

        public ObservableCollection<FileMatch> FileMatches
        {
            get { return m_FileMatches; }
            set
            {
                if (m_FileMatches != value)
                    m_FileMatches = value;
                OnPropertyChanged(nameof(FileMatches));
            }
        }
            

        public string RootDirectory
        {
            get { return m_RootDirectory; }
            set
            {
                if (m_RootDirectory != value)
                    m_RootDirectory = value;
                OnPropertyChanged(nameof(RootDirectory));
            }
        }

        public string FindText
        {
            get { return m_FindText; }
            set
            {
                if (m_FindText != value)
                    m_FindText = value;
                OnPropertyChanged(nameof(FindText));
            }
        }

        public string ReplaceText
        {
            get { return m_ReplaceText; }
            set
            {
                if (m_ReplaceText != value)
                    m_ReplaceText = value;
                OnPropertyChanged(nameof(ReplaceText));
            }
        }

        public string ConsoleText
        {
            get { return m_ConsoleText; }
            set
            {
                if (m_ConsoleText != value)
                    m_ConsoleText = value;
                OnPropertyChanged(nameof(ConsoleText));
            }
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

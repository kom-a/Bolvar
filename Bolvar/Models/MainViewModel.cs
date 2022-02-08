using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Input;
using Bolvar.Views;
using Ookii.Dialogs.Wpf;

namespace Bolvar.Models
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
        private string m_SearchStatus;
        private string m_ConsoleText;
        private BackgroundWorker m_GetFilesWorker;
        private BackgroundWorker m_FindWorker;
        private bool m_IsGettingFiles;
        private bool m_IsSearching;
        private int m_ProgeressPercentage;

        public ICommand ChooseDirectoryCommand { get; set; }
        public ICommand DirectoryOptionsCommand { get; set; }
        public ICommand FindCommand { get; set; }
        public ICommand SearchOptionsCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }

        public MainViewModel(MainWindow ownerWindow)
        {
            m_OwnerWindow = ownerWindow;
            FileMatches = new ObservableCollection<FileMatch>();
            ConsoleText = "";
            IsGettingFiles = false;
            IsSearching = false;
            ProgeressPercentage = 0;

            m_GetFilesWorker = new BackgroundWorker();
            m_GetFilesWorker.DoWork += GetFilesWork;
            m_GetFilesWorker.RunWorkerCompleted += GetFilesCompleted;

            m_FindWorker = new BackgroundWorker();
            m_FindWorker.WorkerSupportsCancellation = true;
            m_FindWorker.WorkerReportsProgress = true;
            m_FindWorker.DoWork += FindWork;
            m_FindWorker.ProgressChanged += FindProgressChanged;
            m_FindWorker.RunWorkerCompleted += FindCompleted;
            


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

        private void FindCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsSearching = false;
            SearchStatus = "";
        }

        private void FindProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileMatch match = e.UserState as FileMatch;

            ProgeressPercentage = e.ProgressPercentage;
            SearchStatus = match.Filename;

            FileMatches.Add(match);
        }

        private void FindWork(object sender, DoWorkEventArgs e)
        {
            string[] filenames = e.Argument as string[];

            for (int i = 0; i < filenames.Length; i++)
            {
                if (m_FindWorker.CancellationPending)
                    break;

                string filename = filenames[i];
                using (StreamReader sr = new StreamReader(filename))
                {
                    string fileContents = sr.ReadToEnd();

                    if (fileContents.Contains(FindText))
                    {
                        int percentProgress = (int)((float)i / filenames.Length * 100 + 1);
                        m_FindWorker.ReportProgress(percentProgress, new FileMatch()
                        {
                            Filename = filename,
                            Matches = 1
                        });
                    }
                }
            }
        }

        private void GetFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsGettingFiles = false;
            SearchStatus = "";
            IEnumerable<string> filenames = e.Result as IEnumerable<string>;
            m_FindWorker.RunWorkerAsync(filenames);
        }

        private void GetFilesWork(object sender, DoWorkEventArgs e)
        {
            IEnumerable<string> filenames = GetAllFilenames();
            e.Result = filenames;
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
                IgnoreInaccessible = true,
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

            if(!m_GetFilesWorker.IsBusy)
            {
                IsSearching = true;
                IsGettingFiles = true;
                SearchStatus = "Getting file list.";
                m_GetFilesWorker.RunWorkerAsync();
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

        public bool IsGettingFiles
        {
            get { return m_IsGettingFiles; }
            set
            {
                if (m_IsGettingFiles != value)
                    m_IsGettingFiles = value;
                OnPropertyChanged(nameof(IsGettingFiles));
            }
        }

        public bool IsSearching
        {
            get { return m_IsSearching; }
            set
            {
                if (m_IsSearching != value)
                    m_IsSearching = value;
                OnPropertyChanged(nameof(IsSearching));
            }
        }

        public string SearchStatus
        {
            get { return m_SearchStatus; }
            set
            {
                if (m_SearchStatus != value)
                    m_SearchStatus = value;
                OnPropertyChanged(nameof(SearchStatus));
            }
        }

        public int ProgeressPercentage
        {
            get { return m_ProgeressPercentage; }
            set
            {
                if (m_ProgeressPercentage != value)
                    m_ProgeressPercentage = value;
                OnPropertyChanged(nameof(ProgeressPercentage));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

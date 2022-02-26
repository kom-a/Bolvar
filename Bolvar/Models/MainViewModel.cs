using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Bolvar.Utils;
using Bolvar.Views;
using Bolvar.Workers;
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
        private FindWorker m_FindWorker;
        private FindWorker m_FindReplaceWorker;
        private PreviewWorker m_PreviewWorker;
        private bool m_IsGettingFiles;
        private bool m_IsSearching;
        private int m_ProgressPercentage;
        private ConsoleMessage m_ConsoleMessage;
        private int m_FilesProcessed;
        private int m_TotalMatches;
        private FileMatch m_SelectedMatch;
        private PreviewData m_Preview;
        private PreviewData m_LoadingPreviewData;

        public ICommand ChooseDirectoryCommand { get; set; }
        public ICommand DirectoryOptionsCommand { get; set; }
        public ICommand FindCommand { get; set; }
        public ICommand SearchOptionsCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand FirstMatchCommand { get; set; }
        public ICommand PreviousMatchCommand { get; set; }
        public ICommand NextMatchCommand { get; set; }
        public ICommand LastMatchCommand { get; set; }

        public MainViewModel(MainWindow ownerWindow)
        {
            // Init private fileds
            m_OwnerWindow = ownerWindow;
            FileMatches = new ObservableCollection<FileMatch>();
            IsGettingFiles = false;
            IsSearching = false;
            TotalMatches = 0;
            FilesProcessed = 0;
            ProgressPercentage = 0;
            RootDirectory = "C:\\Users\\kamil\\Desktop\\test bolvar";
            FindText = "";
            ReplaceText = "";
            Preview = null;

            // Init loading preview
            m_LoadingPreviewData = new PreviewData();
            m_LoadingPreviewData.Index = 0;
            m_LoadingPreviewData.Documents = new FlowDocument[1];
            m_LoadingPreviewData.Documents[0] = new FlowDocument();
            Paragraph loadingParagraph = new Paragraph();
            loadingParagraph.Inlines.Add(new Run("Loading preview..."));
            m_LoadingPreviewData.Documents[0].Blocks.Add(loadingParagraph);

            // Init background workers
            m_FindWorker = new FindWorker(GetFilesCompleted, FindProgressChanged, FindCompleted);
            m_FindReplaceWorker = new FindWorker(GetFilesCompleted, ReplaceProgressChanged, FindCompleted);
            m_PreviewWorker = new PreviewWorker(PreviewCompleted);

            // Init commands
            ChooseDirectoryCommand = new RelayCommand(o => ChooseDirectoryClick());
            DirectoryOptionsCommand = new RelayCommand(o => DirectoryOptionsClick());
            FindCommand = new RelayCommand(o => FindClick());
            SearchOptionsCommand = new RelayCommand(o => SearchOptionsClick());
            ReplaceCommand = new RelayCommand(o => ReplaceClick());
            CancelCommand = new RelayCommand(o => CancelClick());
            FirstMatchCommand = new RelayCommand(o => FirstMatchClick());
            PreviousMatchCommand = new RelayCommand(o => PreviousMatchClick());
            NextMatchCommand = new RelayCommand(o => NextMatchClick());
            LastMatchCommand = new RelayCommand(o => LastMatchClick());

            // Init pop-up windows models
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

            // Print welcome messsage
            Trace("Welcome to Bolvar find and replace tool");
        }

        private bool ValidateFields()
        {
            bool success = true;

            if (!Directory.Exists(RootDirectory))
            {
                Error($"Directory \"{RootDirectory}\" does not exist.");
                success = false;
            }
            if(String.IsNullOrEmpty(FindText))
            {
                Error("Find field can not be empty");
                success = false;
            }

            return success;
        }
        
        #region worker_callbacks

        private void PreviewCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // TODO: rewrite this to get FlowDocument that's ready to show
            PreviewPage[] pages = e.Result as PreviewPage[];

            if (pages == null)
            {
                Preview = null;
                return;
            }
                

            PreviewData tmp = new PreviewData();
            tmp.Documents = new FlowDocument[pages.Length];

            for (int i = 0; i < tmp.Documents.Length; i++)
            {
                tmp.Documents[i] = new FlowDocument();
                Paragraph paragraph = new Paragraph();
                Run prefix = new Run(pages[i].Prefix);
                Bold highlighted = new Bold(new Run(pages[i].Highlighted));
                Run postfix = new Run(pages[i].Postfix);

                paragraph.Inlines.Add(prefix);
                paragraph.Inlines.Add(highlighted);
                paragraph.Inlines.Add(postfix);

                tmp.Documents[i].Blocks.Add(paragraph);
            }

            tmp.Index = 0;

            Preview = tmp;
        }

        private void FindCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsSearching = false;
            
            if (e.Cancelled)
            {
                SearchStatus = "Canceled";
                Info($"Searching has been canceled at " + DateTime.Now);
            }
            else
            {
                SearchStatus = "Complete";
                ProgressPercentage = 100;
                Info($"Search Completed at {DateTime.Now}. {TotalMatches} matches found.");
            }

            Info($"{FilesProcessed} files processed.");
        }

        private void FindProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FindWorkerReportProgress report = e.UserState as FindWorkerReportProgress;

            ProgressPercentage = e.ProgressPercentage;
            FilesProcessed++;
            
            if(report.Match != null)
                SearchStatus = report.Match.Filename;

            if (report.Error)
            {
                if (report.ErrorMessage == "Binary")
                    Warn($"Binary file {report.Match.Filename} skipped.");
                else
                    Warn(report.ErrorMessage);
            }
            else if(report.Match.Matches.Count > 0 || m_SearchOptions.IncludeFilesWithoutMatches)
            {
                TotalMatches += report.Match.Matches.Count;
                FileMatches.Add(report.Match);
            }
        }

        private void ReplaceFileCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            FileMatch match = e.Result as FileMatch;
            FileMatches.Add(match);
        }

        private void ReplaceProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FindWorkerReportProgress report = e.UserState as FindWorkerReportProgress;

            ProgressPercentage = e.ProgressPercentage;
            FilesProcessed++;

            if (report.Match != null)
                SearchStatus = report.Match.Filename;

            if (report.Error)
            {
                if (report.ErrorMessage == "Binary")
                    Warn($"Binary file {report.Match.Filename} skipped.");
                else
                    Warn(report.ErrorMessage);
            }
            else if (report.Match.Matches.Count > 0 || m_SearchOptions.IncludeFilesWithoutMatches)
            {
                ReplaceWorkerData replaceData = new ReplaceWorkerData()
                { 
                    SourceText = FindText,
                    ReplaceText = ReplaceText
                };

                TotalMatches += report.Match.Matches.Count;
                ReplaceWorker replaceWorker = new ReplaceWorker(report.Match, replaceData, ReplaceFileCompleted);
                replaceWorker.Run();
            }
        }

        private void GetFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsGettingFiles = false;
            SearchStatus = "";
            string[] filenames = e.Result as string[];
            Info($"Got {filenames.Length} files");
        }

        #endregion

        #region button_callbacks

        private void FirstMatchClick(object sender = null)
        {

        }

        private void PreviousMatchClick(object sender = null)
        {
            Preview.Index--;
            OnPropertyChanged(nameof(Preview));
        }

        private void NextMatchClick(object sender = null)
        {
            Preview.Index++;
            OnPropertyChanged(nameof(Preview));
        }

        private void LastMatchClick(object sender = null)
        {

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

        private void NewSearchPrepareUI()
        {
            FileMatches.Clear();
            TotalMatches = 0;
            FilesProcessed = 0;
            IsGettingFiles = true;
            IsSearching = true;
            SearchStatus = "Getting files...";
        }

        private void FindClick()
        {
            if (!ValidateFields())
                return;

            NewSearchPrepareUI();
            LogNewSearch();
            
            GetFilesWorkerArgument filesSettings = new GetFilesWorkerArgument()
            {
                Root = m_RootDirectory,
                Options = m_DirectoryOptions
            };
            SearchData searchInfo = new SearchData()
            {
                Pattern = m_FindText,
                CaseSensitive = m_SearchOptions.CaseSensitive
            };

            m_FindWorker.RunAsync(filesSettings, searchInfo);
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
            if (!ValidateFields())
                return;

            NewSearchPrepareUI();
            LogNewSearch();

            GetFilesWorkerArgument filesSettings = new GetFilesWorkerArgument()
            {
                Root = m_RootDirectory,
                Options = m_DirectoryOptions
            };
            SearchData searchInfo = new SearchData()
            {
                Pattern = m_FindText,
                CaseSensitive = m_SearchOptions.CaseSensitive
            };

            m_FindReplaceWorker.RunAsync(filesSettings, searchInfo);
        }

        private void CancelClick(object sender = null)
        {
            m_FindWorker.Cancel();
            m_FindReplaceWorker.Cancel();
        }

        #endregion

        #region properties
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

        public ConsoleMessage ConsoleMSG
        {
            get { return m_ConsoleMessage; }
            set
            {
                if (m_ConsoleMessage != value)
                    m_ConsoleMessage = value;
                OnPropertyChanged(nameof(ConsoleMSG));
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

        public int ProgressPercentage
        {
            get { return m_ProgressPercentage; }
            set
            {
                if (m_ProgressPercentage != value)
                    m_ProgressPercentage = value;
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public int TotalMatches
        { 
            get { return m_TotalMatches; }
            set
            {
                if (m_TotalMatches != value)
                    m_TotalMatches = value;
                OnPropertyChanged(nameof(TotalMatches));
            }
        }

        public int FilesProcessed
        {
            get { return m_FilesProcessed; }
            set
            {
                if (m_FilesProcessed != value)
                    m_FilesProcessed = value;
                OnPropertyChanged(nameof(FilesProcessed));
            }
        }

        public FileMatch SelectedMatch
        {
            get { return m_SelectedMatch; }
            set
            {
                if (m_SelectedMatch != value)
                    m_SelectedMatch = value;
                Preview = m_LoadingPreviewData;
                m_PreviewWorker.Run(SelectedMatch);
                OnPropertyChanged(nameof(SelectedMatch));
            }
        }

        public PreviewData Preview
        {
            get { return m_Preview; }
            set
            {
                if (m_Preview != value)
                    m_Preview = value;
                OnPropertyChanged(nameof(Preview));
            }
        }
        

        #endregion

        #region logger_helpers
        private void Trace(string message)
        {
            ConsoleMSG = new ConsoleMessage(message, ConsoleMessage.LogLevel.Trace);
        }

        private void Info(string message)
        {
            ConsoleMSG = new ConsoleMessage(message, ConsoleMessage.LogLevel.Info);
        }

        private void Warn(string message)
        {
            ConsoleMSG = new ConsoleMessage(message, ConsoleMessage.LogLevel.Warning);
        }

        private void Error(string message)
        {
            ConsoleMSG = new ConsoleMessage(message, ConsoleMessage.LogLevel.Error);
        }

        private void LogNewSearch()
        {
            Trace("---------------------------------------");
            Info("Searching started at " + DateTime.Now);

            Info("Directory: " + RootDirectory);
            Info("Include sub-directories: " + m_DirectoryOptions.IncludeSubDirectories);

            if (!String.IsNullOrWhiteSpace(m_DirectoryOptions.FileMask))
                Info("File mask: " + m_DirectoryOptions.FileMask);
            if(!String.IsNullOrWhiteSpace(m_DirectoryOptions.ExcludeDir))
                Info("Excluded dirs: " + m_DirectoryOptions.ExcludeDir);
            if(!String.IsNullOrWhiteSpace(m_DirectoryOptions.ExcludeMask))
                Info("Excluded mask: " + m_DirectoryOptions.ExcludeMask);

            Info("Case sensitive: " + m_SearchOptions.CaseSensitive);
            Info("Include Files Without Matches: " + m_SearchOptions.IncludeFilesWithoutMatches);
        }
         
        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

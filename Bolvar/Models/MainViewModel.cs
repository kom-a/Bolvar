﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
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
        private GetFilesWorker m_GetFilesWorker;
        private FindWorker m_FindWorker;
        private bool m_IsGettingFiles;
        private bool m_IsSearching;
        private int m_ProgressPercentage;
        private ConsoleLogger m_ConsoleLogger;
        private int m_FilesProcessed;
        private int m_TotalMatches;

        public ICommand ChooseDirectoryCommand { get; set; }
        public ICommand DirectoryOptionsCommand { get; set; }
        public ICommand FindCommand { get; set; }
        public ICommand SearchOptionsCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        public MainViewModel(MainWindow ownerWindow)
        {
            m_OwnerWindow = ownerWindow;
            FileMatches = new ObservableCollection<FileMatch>();
            IsGettingFiles = false;
            IsSearching = false;
            TotalMatches = 0;
            FilesProcessed = 0;
            ProgressPercentage = 0;

            m_GetFilesWorker = new GetFilesWorker(GetFilesCompleted);
            m_FindWorker = new FindWorker(FindProgressChanged, FindCompleted);

            ChooseDirectoryCommand = new RelayCommand(o => ChooseDirectoryClick());
            DirectoryOptionsCommand = new RelayCommand(o => DirectoryOptionsClick());
            FindCommand = new RelayCommand(o => FindClick());
            SearchOptionsCommand = new RelayCommand(o => SearchOptionsClick());
            ReplaceCommand = new RelayCommand(o => ReplaceClick());
            CancelCommand = new RelayCommand(o => CancelClick());

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

            m_ConsoleLogger = new ConsoleLogger();
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

            return success;
        }

        #region worker_callbacks

        private void FindCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsSearching = false;
            SearchStatus = "";
            ProgressPercentage = 100;

            if (e.Cancelled)
            {
                Info($"Searching has been canceled.");
            }
            else
            {
                Info($"Search Completed. {TotalMatches} matches found.");
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
                Warn(report.ErrorMessage);
            }
            else if(report.Match.Matches > 0 || m_SearchOptions.IncludeFilesWithoutMatches)
            {
                TotalMatches += report.Match.Matches;
                FileMatches.Add(report.Match);
            }
        }

        private void GetFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsGettingFiles = false;
            SearchStatus = "";
            string[] filenames = e.Result as string[];
            Info($"Got {filenames.Length} files");
            m_FindWorker.RunAsync(new FindWorkerArgument()
            {
                Filenames = filenames,
                Pattern = FindText
            });
        }

        #endregion

        #region button_callbacks

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

        private void FindClick()
        {
            FileMatches.Clear();
            TotalMatches = 0;
            FilesProcessed = 0;

            if (!ValidateFields())
                return;

            if(!m_GetFilesWorker.IsBusy())
            {
                IsSearching = true;
                IsGettingFiles = true;
                SearchStatus = "Getting file list.";
                LogNewSearch();
                m_GetFilesWorker.RunAsync(new GetFilesWorkerArgument()
                {
                    Root = RootDirectory,
                    Options = m_DirectoryOptions
                });
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
            Warn("HEllo world, this is a warning. Do not pay attention to it. THis is not an error LOL");
        }

        private void CancelClick(object sender = null)
        {
            m_FindWorker.Cancel();
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

        public ConsoleLogger Logger
        {
            get { return m_ConsoleLogger; }
            set
            {
                OnPropertyChanged(nameof(Logger));
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

        #endregion

        #region logger_helpers
        private void Trace(string message)
        {
            Logger.Trace(message);
            OnPropertyChanged(nameof(Logger));
        }

        private void Info(string message)
        {
            Logger.Info(message);
            OnPropertyChanged(nameof(Logger));
        }

        private void Warn(string message)
        {
            Logger.Warn(message);
            OnPropertyChanged(nameof(Logger));
        }

        private void Error(string message)
        {
            Logger.Error(message);
            OnPropertyChanged(nameof(Logger));
        }

        private void LogNewSearch()
        {
            Logger.Trace("---------------------------------------");
            Logger.Info("Searching started at " + DateTime.Now);

            Logger.Info("Directory: " + RootDirectory);
            Logger.Info("Include sub-directories: " + m_DirectoryOptions.IncludeSubDirectories);

            if (!String.IsNullOrWhiteSpace(m_DirectoryOptions.FileMask))
                Logger.Info("File mask: " + m_DirectoryOptions.FileMask);
            if(!String.IsNullOrWhiteSpace(m_DirectoryOptions.ExcludeDir))
                Logger.Info("Excluded dirs: " + m_DirectoryOptions.ExcludeDir);
            if(!String.IsNullOrWhiteSpace(m_DirectoryOptions.ExcludeMask))
                Logger.Info("Excluded mask: " + m_DirectoryOptions.ExcludeMask);

            Logger.Info("Case sensitive: " + m_SearchOptions.CaseSensitive);
            Logger.Info("Include Files Without Matches: " + m_SearchOptions.IncludeFilesWithoutMatches);

            OnPropertyChanged(nameof(Logger));
        }
         
        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

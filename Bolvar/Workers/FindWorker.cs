using Bolvar.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Bolvar.Workers
{
    public class SearchData
    {
        public string Pattern { get; set; }
        public bool CaseSensitive { get; set; }
    }

    public class FindWorkerArgument
    {
        public string[] Filenames { get; set; }
        public SearchData SearchInfo { get; set; }
    }

    public class FindWorkerReportProgress
    {
        public FileMatch Match { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FindWorker
    {
        private GetFilesWorker m_GetFilesWorker;
        private BackgroundWorker m_Worker;
        private SearchData m_SearchInfo;


        public FindWorker(RunWorkerCompletedEventHandler gotFiles, ProgressChangedEventHandler progressChanged, RunWorkerCompletedEventHandler completed)
        {
            m_GetFilesWorker = new GetFilesWorker(gotFiles, GetFilesCompleted);

            m_Worker = new BackgroundWorker();
            m_Worker.WorkerSupportsCancellation = true;
            m_Worker.WorkerReportsProgress = true;
            m_Worker.DoWork += FindWork;
            m_Worker.ProgressChanged += progressChanged;
            m_Worker.RunWorkerCompleted += completed;
        }

        private void GetFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string[] filenames = e.Result as string[];
            FindWorkerArgument argument = new FindWorkerArgument()
            {
                Filenames = filenames, 
                SearchInfo = m_SearchInfo
            };

            m_Worker.RunWorkerAsync(argument);
        }

        private List<int> AllIndexesOf(string str, string value, bool caseSensitive)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        private void FindSingleLine(FindWorkerArgument argument, DoWorkEventArgs e)
        {
            string[] filenames = argument.Filenames;

            for (int i = 0; i < filenames.Length; i++)
            {
                if (m_Worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                int percentProgress = (int)((float)i / filenames.Length * 100) + 1;

                try
                {
                    string filename = filenames[i];

                    if (FileUtils.IsBinary(filename))
                    {
                        m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                        {
                            Match = new FileMatch()
                            {
                                Filename = filename,
                                Pattern = argument.SearchInfo.Pattern,
                                Matches = new List<Match>()

                            },
                            Error = true,
                            ErrorMessage = "Binary"
                        });

                        continue;
                    }

                    using (StreamReader sr = new StreamReader(filename))
                    {
                        string lineContent;
                        List<Match> matches = new List<Match>();

                        int lineIndex = 0;
                        while ((lineContent = sr.ReadLine()) != null)
                        {
                            List<int> indices = AllIndexesOf(lineContent, argument.SearchInfo.Pattern, argument.SearchInfo.CaseSensitive);
                            
                            foreach (int index in indices)
                            {
                                matches.Add(new Match()
                                {
                                    Line = lineIndex,
                                    Position = index
                                });
                            }
                            lineIndex++;
                        }

                        m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                        {
                            Match = new FileMatch()
                            {
                                Filename = filename,
                                Pattern = argument.SearchInfo.Pattern,
                                Matches = matches
                            },
                            Error = false,
                            ErrorMessage = ""
                        });
                    }
                }
                catch (Exception ex)
                {
                    m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                    {
                        Match = null,
                        Error = true,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }

        private void FindMultipleLines(FindWorkerArgument argument, DoWorkEventArgs e)
        {
            // TODO: rewirte this completely (it does not work in some very specific cases)
            string[] filenames = argument.Filenames;
            string[] patternLines = argument.SearchInfo.Pattern.Split("\n");
            StringComparison comparison = argument.SearchInfo.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            for (int i = 0; i < filenames.Length; i++)
            {
                if (m_Worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                int percentProgress = (int)((float)i / filenames.Length * 100) + 1;

                try
                {
                    int totalOccurences = 0;
                    string filename = filenames[i];

                    if (FileUtils.IsBinary(filename))
                    {
                        m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                        {
                            Match = new FileMatch()
                            {
                                Filename = filename,
                                Matches = new List<Match>()
                            },
                            Error = true,
                            ErrorMessage = "Binary"
                        });

                        continue;
                    }

                    LinkedList<string> cache = new LinkedList<string>();
                    List<Match> matches = new List<Match>();
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        int lineIndex = 0;
                        while (sr.Peek() > -1)
                        {
                            string line;
                            if (cache.Count != 0)
                                line = cache.First.Value;
                            else
                                line = sr.ReadLine();

                            if(line.EndsWith(patternLines[0].Replace("\r", ""), comparison))
                            {
                                int position = line.LastIndexOf(patternLines[0].Replace("\r", ""), comparison);
                                for (int j = 1; j < patternLines.Length; j++)
                                {
                                    line = sr.ReadLine();
                                    cache.AddLast(line);
                                    if(j == patternLines.Length - 1)
                                    {
                                        if (line != null && line.StartsWith(patternLines[j].Replace("\r", ""), comparison))
                                        {
                                            // MATCH
                                            totalOccurences++;

                                            matches.Add(new Match()
                                            {
                                                Line = lineIndex,
                                                Position = position
                                            });

                                            if (cache.Count != 0)
                                                cache.RemoveFirst();
                                        }
                                        else
                                        {
                                            // NO MATCH
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if(line != null && line.Equals(patternLines[j].Replace("\r", ""), comparison))
                                        {
                                            // MATCH
                                        }
                                        else
                                        {
                                            // NO MATCH
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (cache.Count != 0)
                                    cache.RemoveFirst();
                            }
                            lineIndex++;
                        }
                        m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                        {
                            Match = new FileMatch()
                            {
                                Filename = filename,
                                Pattern = argument.SearchInfo.Pattern,
                                Matches = matches
                            },
                            Error = false,
                            ErrorMessage = ""
                        });
                    }
                }
                catch (Exception ex)
                {
                    m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                    {
                        Match = null,
                        Error = true,
                        ErrorMessage = ex.Message
                    });
                }
            }

        }

        private void FindWork(object sender, DoWorkEventArgs e)
        {
            FindWorkerArgument argument = e.Argument as FindWorkerArgument;
            string pattern = argument.SearchInfo.Pattern;
            string[] patternLines = pattern.Split("\n");

            if(patternLines.Length == 1)
                FindSingleLine(argument, e);
            else if (patternLines.Length > 1)
                FindMultipleLines(argument, e);
        }

        public bool IsBusy()
        {
            return m_Worker.IsBusy;
        }

        public void RunAsync(GetFilesWorkerArgument filesSettings, SearchData searchInfo)
        {
            if (m_GetFilesWorker.IsBusy() || m_Worker.IsBusy)
                return;

            m_SearchInfo = searchInfo;
            m_GetFilesWorker.RunAsync(filesSettings);
        }

        public void Cancel()
        {
            m_Worker.CancelAsync();
        }
    }
}

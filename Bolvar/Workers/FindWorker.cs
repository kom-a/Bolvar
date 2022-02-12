using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Bolvar.Workers
{
    class FindWorkerArgument
    {
        public string[] Filenames { get; set; }
        public string Pattern { get; set; }
        public bool CaseSensitive { get; set; }
    }

    class FindWorkerReportProgress
    {
        public FileMatch Match { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
    }

    class FindWorker
    {
        private BackgroundWorker m_Worker;

        public FindWorker(ProgressChangedEventHandler progeress, RunWorkerCompletedEventHandler completed)
        {
            m_Worker = new BackgroundWorker();
            m_Worker.WorkerSupportsCancellation = true;
            m_Worker.WorkerReportsProgress = true;
            m_Worker.DoWork += FindWork;
            m_Worker.ProgressChanged += progeress;
            m_Worker.RunWorkerCompleted += completed;
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

        private void FindWork(object sender, DoWorkEventArgs e)
        {
            FindWorkerArgument argument = e.Argument as FindWorkerArgument;
            string[] filenames = argument.Filenames;
            string pattern = argument.Pattern;

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
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        string line;
                        int totalOccurences = 0;

                        while ((line = sr.ReadLine()) != null)
                        {
                            List<int> indices = AllIndexesOf(line, pattern, argument.CaseSensitive);
                            totalOccurences += indices.Count;
                        }

                        m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                        {
                            Match = new FileMatch()
                            {
                                Filename = filename,
                                Matches = totalOccurences
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

        public bool IsBusy()
        {
            return m_Worker.IsBusy;
        }

        public void RunAsync(FindWorkerArgument argument)
        {
            m_Worker.RunWorkerAsync(argument);
        }

        public void Cancel()
        {
            m_Worker.CancelAsync();
        }
    }
}

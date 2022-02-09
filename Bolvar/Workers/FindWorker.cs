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

        private void FindWork(object sender, DoWorkEventArgs e)
        {
            FindWorkerArgument argument = e.Argument as FindWorkerArgument;
            string[] filenames = argument.Filenames;
            string pattern = argument.Pattern;

            for (int i = 0; i < filenames.Length; i++)
            {
                if (m_Worker.CancellationPending)
                    break;

                int percentProgress = (int)((float)i / filenames.Length * 100 + 1);

                try
                {
                    string filename = filenames[i];
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        string fileContents = sr.ReadToEnd();

                        if (fileContents.Contains(pattern))
                        {
                            m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                            {
                                Match = new FileMatch()
                                {
                                    Filename = filename,
                                    Matches = 1
                                },
                                Error = false,
                                ErrorMessage = null
                            });
                        }
                        else
                        {
                            m_Worker.ReportProgress(percentProgress, new FindWorkerReportProgress()
                            {
                                Match = new FileMatch()
                                {
                                    Filename = filename,
                                    Matches = 0
                                },
                                Error = false,
                                ErrorMessage = null
                            });
                        }
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
    }
}

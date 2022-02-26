using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Bolvar.Workers
{
    public class ReplaceWorkerData
    {
        public string SourceText { get; set; }
        public string ReplaceText { get; set; }
    }


    public class ReplaceWorker
    {
        private BackgroundWorker m_Worker;
        private FileMatch m_FileMatch;
        private ReplaceWorkerData m_ReplaceWorkerData;

        public ReplaceWorker(FileMatch fileMatch, ReplaceWorkerData replaceData, RunWorkerCompletedEventHandler replaceCompleted)
        {
            m_Worker = new BackgroundWorker();
            m_Worker.DoWork += ReplaceWork;
            m_Worker.RunWorkerCompleted += replaceCompleted;

            m_FileMatch = fileMatch;
            m_ReplaceWorkerData = replaceData;
        }

        private void ReplaceWork(object sender, DoWorkEventArgs e)
        {
            if (!File.Exists(m_FileMatch.Filename))
                return;

            // TODO: rewrite this
            string text = File.ReadAllText(m_FileMatch.Filename);
            text = text.Replace(m_ReplaceWorkerData.SourceText, m_ReplaceWorkerData.ReplaceText);
            File.WriteAllText(m_FileMatch.Filename, text);

            m_FileMatch.Pattern = m_ReplaceWorkerData.ReplaceText;

            e.Result = m_FileMatch;
        }

        public void Run()
        {
            m_Worker.RunWorkerAsync();
        }
    }
}

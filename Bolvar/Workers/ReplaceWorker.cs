using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Bolvar.Workers
{
    class ReplaceWorker
    {
        private BackgroundWorker m_Worker;
        private FileMatch m_FileMatch;
        private string m_ReplaceText;

        public ReplaceWorker(FileMatch fileMatch, string replaceText, RunWorkerCompletedEventHandler replaceCompleted)
        {
            m_Worker = new BackgroundWorker();
            m_Worker.DoWork += ReplaceWork;
            m_Worker.RunWorkerCompleted += replaceCompleted;

            m_FileMatch = fileMatch;
            m_ReplaceText = replaceText;
        }

        private void ReplaceWork(object sender, DoWorkEventArgs e)
        {
            e.Result = new FileMatch()
            {
                Filename = "Hello world.txt",
                Matches = 123
            };
        }

        public void Run()
        {
            m_Worker.RunWorkerAsync();
        }
    }
}

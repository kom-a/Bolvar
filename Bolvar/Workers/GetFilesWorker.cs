using Bolvar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Bolvar.Workers
{
    public class GetFilesWorkerArgument
    {
        public string Root { get; set; }
        public DirectoryOptionsModel Options { get; set; }
    }

    class GetFilesWorker
    {
        private BackgroundWorker m_Worker;

        public GetFilesWorker(RunWorkerCompletedEventHandler completed)
        {
            m_Worker = new BackgroundWorker();
            m_Worker.DoWork += GetFilesWork;
            m_Worker.RunWorkerCompleted += completed;
        }

        private void GetFilesWork(object sender, DoWorkEventArgs e)
        {
            GetFilesWorkerArgument settings = e.Argument as GetFilesWorkerArgument;

            EnumerationOptions options = new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = settings.Options.IncludeSubDirectories,
                ReturnSpecialDirectories = false,
            };

            e.Result = Directory.GetFiles(settings.Root, settings.Options.FileMask, options);
        }

        public bool IsBusy()
        {
            return m_Worker.IsBusy;
        }

        public void RunAsync(GetFilesWorkerArgument settings)
        {
            m_Worker.RunWorkerAsync(settings);
        }


    }
}

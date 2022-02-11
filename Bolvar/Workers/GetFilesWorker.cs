using Bolvar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

            string[] masks = settings.Options.ExcludeMask.Split(',');
            List<Regex> regexMasks = new List<Regex>();
            foreach (var m in masks)
            {
                regexMasks.Add(new Regex(
                '^' +
                    Regex.Replace(m, @"\s+", "")
                    .Replace(".", "[.]")
                    .Replace("*", ".*")
                    .Replace("?", ".")
                + '$',
                RegexOptions.IgnoreCase));
            }

            string[] files = Directory.GetFiles(settings.Root, settings.Options.FileMask, options);
            List<string> res = new List<string>();

            foreach (var f in files)
            {
                bool matchExclude = false;
                foreach (var m in regexMasks)
                {
                    if (m.IsMatch(f))
                    {
                        matchExclude = true;
                        break;
                    }
                }
                if (!matchExclude)
                    res.Add(f);
            }

            e.Result = res.ToArray();
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

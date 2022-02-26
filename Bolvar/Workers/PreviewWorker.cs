using Bolvar.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Bolvar.Workers
{
    public class PreviewPage
    {
        public string Prefix;
        public string Highlighted;
        public string Postfix;
    }

    class PreviewWorker
    {
        private BackgroundWorker m_Worker;

        public PreviewWorker(RunWorkerCompletedEventHandler completed)
        {
            m_Worker = new BackgroundWorker();
            m_Worker.DoWork += PreviewWork;
            m_Worker.RunWorkerCompleted += completed;

        }

        private void PreviewWork(object sender, DoWorkEventArgs e)
        {
            FileMatch match = e.Argument as FileMatch;

            if (match == null || !File.Exists(match.Filename))
            {
                e.Result = null;
                return;
            }

            PreviewPage[] pages = new PreviewPage[match.Matches.Count];

            const int nonHighlighedLength = 4;

            using (StreamReader sr = new StreamReader(match.Filename))
            {
                string line;
                int lineIndex = 0;

                while ((line = sr.ReadLine()) != null)
                {
                    for (int i = 0; i < pages.Length; i++)
                    {
                        if (pages[i] == null)
                        {
                            pages[i] = new PreviewPage()
                            {
                                Prefix = string.Empty,
                                Highlighted = string.Empty,
                                Postfix = string.Empty,
                            };
                        }

                        string[] splittedPattern = match.Pattern.Split('\n');
                        string lastPatternLine = splittedPattern[splittedPattern.Length - 1];

                        if (lineIndex > match.Matches[i].Line - nonHighlighedLength && lineIndex < match.Matches[i].Line)
                        {
                            pages[i].Prefix += (lineIndex + 1).ToString() + ". " + line + '\n';
                        }

                        if (lineIndex == match.Matches[i].Line)
                        {
                            pages[i].Prefix += (lineIndex + 1).ToString() + ". "  + line.Substring(0, match.Matches[i].Position);

                            for(int j = 0; j < splittedPattern.Length; j++)
                            {
                                if (j != 0)
                                    pages[i].Highlighted += (lineIndex + j + 1).ToString() + ". ";
                                pages[i].Highlighted += splittedPattern[j];
                            }
                        }
                        if (lineIndex == match.Matches[i].Line + splittedPattern.Length - 1)
                        {    
                            if (splittedPattern.Length == 1)
                            {
                                pages[i].Postfix += line.Substring(lastPatternLine.Length + match.Matches[i].Position) + '\n';
                            }
                            else
                            {
                                pages[i].Postfix += line.Substring(lastPatternLine.Length) + '\n';
                            }
                        }

                        if(lineIndex > match.Matches[i].Line + splittedPattern.Length - 1 && lineIndex < match.Matches[i].Line + nonHighlighedLength)
                        {
                            pages[i].Postfix += (lineIndex + 1).ToString() + ". " + line + '\n';
                        }
                    }

                    lineIndex++;
                }
            }

            e.Result = pages;
        }

        public void Run(FileMatch match)
        {
            m_Worker.RunWorkerAsync(match);
        }
    }
}

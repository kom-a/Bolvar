using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Bolvar.Workers
{
    public class Callbacks
    {
        public RunWorkerCompletedEventHandler GotFiles { get; set; }
        public ProgressChangedEventHandler ProgressChanged { get; set; }
        public RunWorkerCompletedEventHandler Completed { get; set; }
    }
}

using System.Collections.Generic;

namespace Bolvar.Utils
{ 
    public class ConsoleMessage
    {
        public enum LogLevel
        {
            Trace,
            Info,
            Warning,
            Error,
        }

        public string Msg { get; set; }
        public LogLevel Level { get; set; }
        

        public ConsoleMessage(string msg, LogLevel level)
        {
            Msg = msg;
            Level = level;
        }
    }
}

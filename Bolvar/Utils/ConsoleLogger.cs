using System.Collections.Generic;

namespace Bolvar.Utils
{ 
    public class Message
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
        

        public Message(string msg, LogLevel level)
        {
            Msg = msg;
            Level = level;
        }
    }


    class ConsoleLogger 
    {
        public List<Message> Messages { get; set; }

        public ConsoleLogger()
        {
            Messages = new List<Message>();
        }
        
        public void Trace(string message)
        {
            Messages.Add(new Message(message, Message.LogLevel.Trace));
        }

        public void Info(string message)
        {
            Messages.Add(new Message(message, Message.LogLevel.Info));
        }

        public void Warn(string message)
        {
            Messages.Add(new Message(message, Message.LogLevel.Warning));
        }

        public void Error(string message)
        {
            Messages.Add(new Message(message, Message.LogLevel.Error));
        }
    }
}

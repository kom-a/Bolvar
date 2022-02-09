using Bolvar.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(ConsoleLogger), typeof(FlowDocument))]
    class ConsoleLoggerToFlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
    object parameter, System.Globalization.CultureInfo culture)
        {
            FlowDocument doc = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            ConsoleLogger logger = value as ConsoleLogger;

            if (logger == null)
            {
                doc.Blocks.Add(paragraph);
                return doc;
            }

            List<Message> messages = logger.Messages;

            foreach (Message msg in messages)
            {
                Run run = new Run();

                if (msg.Level != Message.LogLevel.Trace)
                    run.Text = msg.Level.ToString() + ": " + msg.Msg;
                else
                    run.Text = msg.Msg;

                switch (msg.Level)
                {
                    case Message.LogLevel.Trace: { run.Foreground = new SolidColorBrush(Colors.White); break; }
                    case Message.LogLevel.Info: { run.Foreground = new SolidColorBrush(Colors.LawnGreen); break; }
                    case Message.LogLevel.Warning: { run.Foreground = new SolidColorBrush(Colors.Orange); break; }
                    case Message.LogLevel.Error: { run.Foreground = new SolidColorBrush(Colors.Red); break; }
                }

                paragraph.Inlines.Add(run);
                paragraph.Inlines.Add(new LineBreak());
            }

            doc.Blocks.Add(paragraph);
            return doc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

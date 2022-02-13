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
    [ValueConversion(typeof(ConsoleMessage), typeof(FlowDocument))]
    class ConsoleMessageToFlowDocumentConverter : IValueConverter
    {
        private FlowDocument m_Document;
        private Paragraph m_Paragraph;
        ConsoleMessageToFlowDocumentConverter()
        {
            m_Document = new FlowDocument();
            m_Paragraph = new Paragraph();
            m_Document.Blocks.Add(m_Paragraph);
        }

        public object Convert(object value, Type targetType,
    object parameter, System.Globalization.CultureInfo culture)
        {
            ConsoleMessage message = value as ConsoleMessage;

            if (message == null)
                return m_Document;

            Run run;

            switch (message.Level)
            {
                case ConsoleMessage.LogLevel.Trace:
                {
                    run = new Run(message.Msg);
                    run.Foreground = new SolidColorBrush(Colors.White);
                } break;
                case ConsoleMessage.LogLevel.Info:
                {
                    run = new Run(message.Msg);
                    run.Foreground = new SolidColorBrush(Colors.LawnGreen);
                } break;
                case ConsoleMessage.LogLevel.Warning:
                {
                    run = new Run(message.Msg);
                    run.Foreground = new SolidColorBrush(Colors.Orange);
                } break;
                case ConsoleMessage.LogLevel.Error:
                {
                    run = new Run(message.Msg);
                    run.Foreground = new SolidColorBrush(Colors.Red);
                } break;
                default:
                {
                    run = new Run("Failed to log message");
                    run.Foreground = new SolidColorBrush(Colors.Violet);
                } break;
            }

            m_Paragraph.Inlines.Add(run);
            m_Paragraph.Inlines.Add(new LineBreak());

            return m_Document;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

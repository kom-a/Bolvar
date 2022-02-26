using Bolvar.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(PreviewData), typeof(FlowDocument))]
    class PreviewDataToFlowDocumentConverter : IValueConverter
    {
        private FlowDocument ErrorDocument()
        {
            FlowDocument errorDocument = new FlowDocument();
            Paragraph paragraph = new Paragraph();
            Run run = new Run("Failed to preview document.");
            run.Foreground = new SolidColorBrush(Colors.Red);
            paragraph.Inlines.Add(run);
            errorDocument.Blocks.Add(paragraph);

            return errorDocument;
        }

        private FlowDocument EmptyDocument()
        {
            FlowDocument errorDocument = new FlowDocument();
            Paragraph paragraph = new Paragraph();
            Run run = new Run("Nothing to preview.");
            paragraph.Inlines.Add(run);
            errorDocument.Blocks.Add(paragraph);

            return errorDocument;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PreviewData previewData = value as PreviewData;
            if (previewData == null || previewData.Documents == null || previewData.Documents.Length == 0)
                return EmptyDocument();
            else if (previewData.Index < 0 || previewData.Index >= previewData.Documents.Length)
                return ErrorDocument();

            return previewData.Documents[previewData.Index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

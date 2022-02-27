using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using Bolvar.Utils;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(PreviewData), typeof(string))]
    class PreviewToIndexLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PreviewData preview = value as PreviewData;
            if (preview == null)
                return "0/0";

            string current = (preview.Index + 1).ToString();
            string total = preview.Documents.Length.ToString();

            return current + "/" + total;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

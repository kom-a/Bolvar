using Bolvar.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(PreviewData), typeof(bool))]
    class PreviewToIsPreviousPageAvailableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PreviewData preview = value as PreviewData;
            if (preview == null)
                return false;

            return preview.Index > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

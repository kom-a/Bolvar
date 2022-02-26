using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using Bolvar.Utils;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(List<object>), typeof(int))]
    class MatchListToMatchListCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<Match> list = value as List<Match>;

            if (list == null)
                return -1;

            return list.Count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

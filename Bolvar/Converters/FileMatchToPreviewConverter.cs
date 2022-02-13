using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;

namespace Bolvar.Converters
{
    [ValueConversion(typeof(FileMatch), typeof(string))]
    class FileMatchToPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FileMatch match = value as FileMatch;

            if (match == null)
                return "Nothing to preview";

            StringBuilder stringBuilder = new StringBuilder();

            using (StreamReader sr = new StreamReader(match.Filename))
            {
                string line;
                int index = 1;
                while((line = sr.ReadLine()) != null)
                {
                    stringBuilder.Append(index.ToString());
                    stringBuilder.Append(". ");
                    stringBuilder.AppendLine(line);
                    index++;
                }
            }

            return stringBuilder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

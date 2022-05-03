using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace UWP.Converters
{
    internal class DictValueByKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IDictionary<string, string> dict = parameter as IDictionary<string, string>;
            string key = value as string;

            string s;
            if (dict != null && !string.IsNullOrEmpty(key) && dict.TryGetValue(key, out s))
                return s;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

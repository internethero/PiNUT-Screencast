﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UWP.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
                return Visibility.Collapsed;

            bool result = false;
            var currentString = value.ToString().Trim();
            var paramString = parameter.ToString().Trim();


            if (paramString.IndexOf(",") != -1)
            {
                string[] parameters = paramString.Split(",");

                foreach (var param in parameters)
                {
                    result = currentString == param ? true : false;

                    if (result)
                        break;
                }
            }
            else
            {
                if (currentString == paramString)
                    result = true;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            string language)
        {
            throw new NotImplementedException();
        }
    }
}

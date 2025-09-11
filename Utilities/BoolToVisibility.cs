using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace SistemaInstituto.Utilities
{
    class BoolToVisibility : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = false;
            if (value is bool vb)
                b = vb;
            if (parameter?.ToString() == "Invert")
                b = !b;
            if (Invert)
                b = !b;

            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }
}

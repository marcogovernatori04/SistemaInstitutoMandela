using System.Globalization;
using System.Windows.Data;

namespace SistemaInstituto.Utilities
{
    public class EnumToBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && parameter != null)
            {
                return parameter;
            }

            return Binding.DoNothing;
        }
    }
}

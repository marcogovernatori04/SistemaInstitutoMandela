using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;


namespace SistemaInstituto.Utilities
{
    public class SaldoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal saldo)
            {
                return saldo >= 0 ? Brushes.Green : Brushes.Red;
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


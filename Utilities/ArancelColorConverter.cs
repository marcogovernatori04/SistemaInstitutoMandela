using System.Windows.Data;
using System.Windows.Media;

namespace SistemaInstituto.Utilities
{
    public class ArancelColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value?.ToString())
            {
                case "Hasta el 10": return Brushes.Green;
                case "Del 10 al 30": return Brushes.DarkGoldenrod;
                case "Mes vencido": return Brushes.Red;
                case "Año completo": return Brushes.DarkGreen;
                default: return Brushes.Black; 
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

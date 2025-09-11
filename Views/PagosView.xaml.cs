using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SistemaInstituto.Views
{
    /// <summary>
    /// Lógica de interacción para PagosView.xaml
    /// </summary>
    public partial class PagosView : UserControl
    {
        public PagosView()
        {
            InitializeComponent();
        }

        private void MonthYearPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker dp && dp.Template.FindName("PART_Calendar", dp) is Calendar cal)
            {
                cal.DisplayMode = CalendarMode.Year;
                cal.DisplayModeChanged += (s, ev) =>
                {
                    if (cal.DisplayMode != CalendarMode.Year)
                        cal.DisplayMode = CalendarMode.Year;
                };

                cal.SelectedDatesChanged += (s, ev) =>
                {
                    if (cal.SelectedDate.HasValue)
                    {
                        dp.SelectedDate = new DateTime(
                            cal.SelectedDate.Value.Year,
                            cal.SelectedDate.Value.Month,
                            1);
                    }
                };
            }

        }
    }
}

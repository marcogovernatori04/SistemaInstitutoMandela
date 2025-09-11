using SistemaInstituto.ViewModels;
using System.Windows;

namespace SistemaInstituto.Views
{
    public partial class SeleccionPagoView : Window
    {
        public SeleccionPagoView(SeleccionPagoViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            vm.CloseAction = result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}

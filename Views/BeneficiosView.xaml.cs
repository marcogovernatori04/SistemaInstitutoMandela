using SistemaInstituto.Models;
using SistemaInstituto.ViewModels;
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
    /// Lógica de interacción para BeneficiosView.xaml
    /// </summary>
    public partial class BeneficiosView : UserControl
    {
        public BeneficiosView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is BeneficiosViewModel vm && vm.BeneficioSeleccionado != null)
            {
                var viewModel = new BeneficioDetalleViewModel(vm.BeneficioSeleccionado.Id);

                var ventanaDetalle = new BeneficioDetalleView
                {
                    DataContext = viewModel
                };

                ventanaDetalle.ShowDialog();
            }
        }
    }
}

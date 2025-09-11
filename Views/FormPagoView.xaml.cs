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
using System.Windows.Shapes;

namespace SistemaInstituto.Views
{
    /// <summary>
    /// Lógica de interacción para FormPagoView.xaml
    /// </summary>
    public partial class FormPagoView : Window
    {
        public FormPagoView(FormPagoViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            vm.CloseAction = result =>
            {
                DialogResult = result;
                Close();
            };
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

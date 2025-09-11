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
    /// Lógica de interacción para AlumnosView.xaml
    /// </summary>
    public partial class AlumnosView : UserControl
    {
        public AlumnosView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Alumno alumno)
            {
                var vm = new PerfilAlumnoViewModel(alumno);
                var ventanaPerfil = new PerfilAlumnoView { DataContext = vm };
                ventanaPerfil.ShowDialog();
            }
        }
    }
}

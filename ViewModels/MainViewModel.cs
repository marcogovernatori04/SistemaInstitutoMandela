using SistemaInstituto.Commands;
using SistemaInstituto.Views;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _vistaActual = null!;
        public object VistaActual
        {
            get => _vistaActual;
            set => SetProperty(ref _vistaActual, value);
        }

        public ICommand MostrarAlumnosCommand { get; }
        public ICommand MostrarPagosCommand { get; }
        public ICommand MostrarArancelesCommand { get; }
        public ICommand MostrarSeleccionPagoCommand { get; }
        public ICommand MostrarBeneficiosCommand { get; }

        public MainViewModel()
        {
            MostrarAlumnosCommand = new RelayCommand(_ => VistaActual = new AlumnosViewModel());
            MostrarPagosCommand = new RelayCommand(_ => VistaActual = new PagosViewModel());
            MostrarArancelesCommand = new RelayCommand(_ => VistaActual = new ArancelesViewModel());
            MostrarSeleccionPagoCommand = new RelayCommand(o => AbrirFormPago());
            MostrarBeneficiosCommand = new RelayCommand(_ => VistaActual = new BeneficiosViewModel());

            VistaActual = new AlumnosViewModel();
        }

        private void AbrirFormPago()
        {
            var viewModel = new SeleccionPagoViewModel();
            var ventana = new SeleccionPagoView(viewModel)
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                VistaActual = new PagosViewModel();
            }


        }
    }
}

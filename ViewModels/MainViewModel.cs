using Microsoft.Win32;
using SistemaInstituto.Commands;
using SistemaInstituto.Generators;
using SistemaInstituto.Interfaces;
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
            set
            {
                SetProperty(ref _vistaActual, value);
                (ExportarPdfCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand MostrarAlumnosCommand { get; }
        public ICommand MostrarPagosCommand { get; }
        public ICommand MostrarMovimientosCommand { get; }
        public ICommand MostrarArancelesCommand { get; }
        public ICommand MostrarSeleccionPagoCommand { get; }
        public ICommand MostrarBeneficiosCommand { get; }
        public ICommand MostrarCursosCommand { get; }
        public ICommand ExportarPdfCommand { get; }

        public MainViewModel()
        {
            MostrarAlumnosCommand = new RelayCommand(_ => VistaActual = new AlumnosViewModel());
            MostrarPagosCommand = new RelayCommand(_ => VistaActual = new PagosViewModel());
            MostrarMovimientosCommand = new RelayCommand(_ => VistaActual = new MovimientosViewModel());
            MostrarArancelesCommand = new RelayCommand(_ => VistaActual = new ArancelesViewModel());
            MostrarSeleccionPagoCommand = new RelayCommand(o => AbrirFormPago());
            MostrarBeneficiosCommand = new RelayCommand(_ => VistaActual = new BeneficiosViewModel());
            MostrarCursosCommand = new RelayCommand(_ => VistaActual = new CursosViewModel());

            ExportarPdfCommand = new RelayCommand(
                _ => ExportarPdf(),
                _ => VistaActual is IExportable
            );

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

        private void ExportarPdf()
        {
            if (VistaActual is not IExportable exportable)
                return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                FileName = "export.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var datos = exportable.GetExportableData();
                PdfExporter.Exportar(saveDialog.FileName, datos);
                System.Windows.MessageBox.Show("Exportación completada.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            }
        }
    }
}

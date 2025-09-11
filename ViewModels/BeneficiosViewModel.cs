using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace SistemaInstituto.ViewModels
{
    public class BeneficiosViewModel : BaseViewModel
    {
        public RelayCommand AgregarBeneficioCommand { get; }
        public RelayCommand EliminarBeneficioCommand { get; }

        public ObservableCollection<Beneficio> Beneficios { get; set; } = null!;

        private InstitutoDbContext _context = new InstitutoDbContext();

        private ICollectionView _beneficiosView {  get; set; } = null!;
        public ICollectionView BeneficiosView
        {
            get => _beneficiosView;
            set
            {
                _beneficiosView = value;
                OnPropertyChanged();
            }
        }

        private string _busqNombre = null!;
        public string BusqNombre
        {
            get => _busqNombre;
            set
            {
                _busqNombre = value;
                OnPropertyChanged();
                BeneficiosView.Refresh();
            }
        }

        private Beneficio _beneficioSeleccionado = null!;
        public Beneficio BeneficioSeleccionado
        {
            get => _beneficioSeleccionado;
            set
            {
                _beneficioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public BeneficiosViewModel()
        {
            AgregarBeneficioCommand = new RelayCommand(o => AgregarBeneficio());
            EliminarBeneficioCommand = new RelayCommand(EliminarBeneficio, PuedeEliminar);

            CargarDatos();
        }

        private void AgregarBeneficio()
        {
            var viewModel = new FormBeneficioViewModel();
            var ventana = new FormBeneficioView()
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                if (viewModel.BeneficioGuardado != null)
                {
                    Beneficios.Add(viewModel.BeneficioGuardado);
                    BeneficiosView.Refresh();
                    BeneficioSeleccionado = viewModel.BeneficioGuardado;
                }
            }
        }

        private bool PuedeEliminar(object? obj) => obj is Beneficio;

        private void EliminarBeneficio(object? obj)
        {
            if (obj is not Beneficio beneficio) return;

            var confirm = MessageBox.Show("¿Está seguro de eliminar este beneficio?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            if (_context.Entry(beneficio).State == EntityState.Detached)
                _context.Attach(beneficio);

            _context.Beneficios.Remove(beneficio);
            _context.SaveChanges();

            Beneficios.Remove(beneficio);
            BeneficiosView.Refresh();
            MessageBox.Show("Beneficio eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void CargarDatos()
        {
            var beneficiosDb = _context.Beneficios.ToList();
            Beneficios = new ObservableCollection<Beneficio>(beneficiosDb);

            BeneficiosView = CollectionViewSource.GetDefaultView(Beneficios);
            BeneficiosView.Filter = FiltrarBeneficios;
        }

        private bool FiltrarBeneficios(object obj)
        {
            if (obj is Beneficio beneficio)
            {
                bool nombreCoincide = string.IsNullOrWhiteSpace(BusqNombre) || beneficio.Nombre.ToLower().Contains(BusqNombre.ToLower());

                return nombreCoincide;
            }
            return false;
        }
    }
}

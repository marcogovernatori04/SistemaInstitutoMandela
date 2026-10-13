using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using SistemaInstituto.Interfaces;
using System.Collections;

namespace SistemaInstituto.ViewModels
{
    public class PagosViewModel : BaseViewModel, IExportable
    {

        public RelayCommand SeleccionarPagoCommand { get; }
        public RelayCommand EliminarPagoCommand { get; }
        public RelayCommand VerFacturaCommand { get; }

        public InstitutoDbContext _context { get; set; } = new();

        public ObservableCollection<Pago> _pagos { get; set; } = new();
        public ObservableCollection<Pago> Pagos
        {
            get => _pagos;
            set { _pagos = value; OnPropertyChanged(); }
        }

        private ICollectionView _pagosView { get; set; } = null!;
        public ICollectionView PagosView
        {
            get => _pagosView;
            set
            {
                _pagosView = value;
                OnPropertyChanged(nameof(PagosView));
                OnPropertyChanged(nameof(CantRegistros));

            }
        }

        public int CantRegistros => PagosView?.Cast<object>().Count() ?? 0;
        public decimal TotalMontos => PagosView?.Cast<Pago>().Sum(p => p.MontoPagado) ?? 0;

        public ObservableCollection<string> Cursos { get; set; } = new();
        public ObservableCollection<string> MetodosPago { get; set; } = new();
        public ObservableCollection<string> Profesores { get; set; } = new();


        private string _filtroAlumno = string.Empty;
        public string FiltroAlumno
        {
            get => _filtroAlumno;
            set
            {
                _filtroAlumno = value;
                OnPropertyChanged();
                PagosView?.Refresh();
            }
        }

        private string? _cursoSeleccionado;
        public string? CursoSeleccionado
        {
            get => _cursoSeleccionado;
            set
            {
                _cursoSeleccionado = value;
                OnPropertyChanged();
                PagosView?.Refresh();
            }
        }

        private string? _metodoPagoSeleccionado;
        public string? MetodoPagoSeleccionado
        {
            get => _metodoPagoSeleccionado;
            set
            {
                _metodoPagoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarComboProfesores));
                PagosView?.Refresh();
            }
        }

        public bool MostrarComboProfesores => MetodoPagoSeleccionado == "A profesor";

        private string? _profesorSeleccionado;
        public string? ProfesorSeleccionado
        {
            get => _profesorSeleccionado;
            set
            {
                _profesorSeleccionado = value;
                OnPropertyChanged();
                PagosView.Refresh();
            }
        }

        public ObservableCollection<string> Meses { get; } = new ObservableCollection<string>();

        private string? _mesSeleccionado;
        public string? MesSeleccionado
        {
            get => _mesSeleccionado;
            set
            {
                _mesSeleccionado = value;
                OnPropertyChanged();
                PagosView?.Refresh();
            }
        }

        public PagosViewModel()
        {
            SeleccionarPagoCommand = new RelayCommand(o => SeleccionarPago());
            VerFacturaCommand = new RelayCommand(VerFactura);
            EliminarPagoCommand = new RelayCommand(EliminarPago, PuedeEliminar);

            CargarDatos();
        }

        private void SeleccionarPago()
        {
            var viewModel = new SeleccionPagoViewModel();
            var ventana = new SeleccionPagoView(viewModel)
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                if (viewModel.PagoIndividual != null)
                {
                    Pagos.Add(viewModel.PagoIndividual);
                    PagosView.Refresh();
                    OnPropertyChanged(nameof(TotalMontos));
                }
                else if (viewModel.PagosGrupales != null)
                {
                    foreach (var p in viewModel.PagosGrupales)
                        Pagos.Add(p);
                    PagosView.Refresh();
                    OnPropertyChanged(nameof(TotalMontos));
                }
            }
        }

        private bool PuedeEliminar(object? obj) => obj is Pago;

        private void EliminarPago(object? obj)
        {
            if (obj is not Pago pago) return;

            var confirm = MessageBox.Show("¿Está seguro de eliminar este pago?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            using var context = new InstitutoDbContext();

            var movimientos = context.Movimientos.Where(m => m.IdPago == pago.Id).ToList();
            if (movimientos.Any())
            {
                context.Movimientos.RemoveRange(movimientos);
            }

            var pagoDb = context.Pagos.FirstOrDefault(p => p.Id == pago.Id);
            if (pagoDb == null) return;

            context.Pagos.Remove(pagoDb);
            context.SaveChanges();

            Pagos.Remove(pago);
            PagosView.Refresh();
            OnPropertyChanged(nameof(TotalMontos));

            MessageBox.Show("Pago eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void CargarDatos()
        {
            var pagosDb = _context.Pagos
                .Include(p => p.IdAlumnoNavigation)
                    .ThenInclude(a => a.IdCursoNavigation)
                .Include(p => p.IdMetodoPagoNavigation)
                .Include(p => p.IdArancelNavigation)
                .OrderByDescending(p => p.FechaPago)
                .ThenBy(p => p.IdAlumnoNavigation.Nombre)
                .AsNoTracking()
                .ToList();

            Pagos = new ObservableCollection<Pago>(pagosDb);

            Cursos = new ObservableCollection<string>(
                pagosDb.Select(p => p.IdAlumnoNavigation.IdCursoNavigation)
                .DistinctBy(c => c.Id)
                .OrderBy(c => c.Orden)
                .Select(c => c.Nombre));

            Cursos.Insert(0, "Todos");

            MetodosPago = new ObservableCollection<string>(
                pagosDb.Select(p => p.IdMetodoPagoNavigation.Nombre).Distinct());

            MetodosPago.Insert(0, "Todos");

            Profesores = new ObservableCollection<string>(
                _context.Profesores
                    .Select(p => p.Nombre)
                    .OrderBy(n => n)
                    );

            Profesores.Insert(0, "Todos");

            CargarMeses();


            PagosView = CollectionViewSource.GetDefaultView(Pagos);
            PagosView.Filter = FiltroPagos;

            FiltroAlumno = string.Empty;
            CursoSeleccionado = Cursos.FirstOrDefault();
            MetodoPagoSeleccionado = MetodosPago.FirstOrDefault();

            PagosView.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalMontos));
            PagosView.CurrentChanged += (s, e) => OnPropertyChanged(nameof(TotalMontos));

            PagosView.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CantRegistros));
            PagosView.CurrentChanged += (s, e) => OnPropertyChanged(nameof(CantRegistros));

            PagosView.Refresh();
            OnPropertyChanged(nameof(TotalMontos));
            OnPropertyChanged(nameof(CantRegistros));
        }

        private void CargarMeses()
        {
            Meses.Clear();

            var fechaInicio = new DateTime(2025, 2, 1);
            var fechaFinal = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var dtf = CultureInfo.CurrentCulture.DateTimeFormat;

            for (var mes = fechaInicio; mes <= fechaFinal; mes = mes.AddMonths(1))
            {
                var nombreMes = dtf.GetAbbreviatedMonthName(mes.Month);
                Meses.Add($"{nombreMes}-{mes:yy}".ToLower());
            }

            Meses.Insert(0, "Todos");

            MesSeleccionado = Meses[0];
        }

        private bool FiltroPagos(object obj)
        {
            if (obj is not Pago pago) return false;

            if (!string.IsNullOrWhiteSpace(FiltroAlumno))
            {
                if (!pago.IdAlumnoNavigation.Nombre.Contains(FiltroAlumno, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(CursoSeleccionado) && CursoSeleccionado != "Todos")
            {
                if (!pago.IdAlumnoNavigation.IdCursoNavigation.Nombre.Equals(CursoSeleccionado, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(MetodoPagoSeleccionado) && MetodoPagoSeleccionado != "Todos")
            {
                if (MetodoPagoSeleccionado == "A profesor")
                {
                    if (!string.IsNullOrWhiteSpace(ProfesorSeleccionado) && ProfesorSeleccionado != "Todos")
                    {
                        var palabras = ProfesorSeleccionado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        bool coincide = palabras.Any(p => (pago.Notas ?? "")
                                                            .Contains(p, StringComparison.OrdinalIgnoreCase));
                        if (!coincide)
                            return false;
                    }
                }
                else
                {
                    if (!pago.IdMetodoPagoNavigation.Nombre.Equals(MetodoPagoSeleccionado, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MesSeleccionado) && MesSeleccionado != "Todos")
            {
                var fecha = DateTime.ParseExact(MesSeleccionado, "MMM-yy", CultureInfo.CurrentCulture);
                if (pago.FechaPago.Month != fecha.Month || pago.FechaPago.Year != fecha.Year)
                    return false;
            }

            return true;
        }

        private void VerFactura(object obj)
        {
            if (obj is not Pago pago) return;

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SistemaInstituto", "Recibos");

            var archivos = Directory.GetFiles(carpeta, $"*{pago.Id}*.pdf");

            if (archivos.Length == 0)
            {
                MessageBox.Show("La factura no existe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string rutaFactura = archivos[0];

            Process.Start(new ProcessStartInfo
            {
                FileName = rutaFactura,
                UseShellExecute = true
            });
        }

        public IEnumerable GetExportableData()
        {
            return PagosView.Cast<Pago>();
        }
    }
}

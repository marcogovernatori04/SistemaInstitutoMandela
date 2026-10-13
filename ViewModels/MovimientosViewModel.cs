using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Data;
using SistemaInstituto.Interfaces;
using SistemaInstituto.Models;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;

namespace SistemaInstituto.ViewModels
{
    public class MovimientosViewModel : BaseViewModel, IExportable
    {
        private readonly InstitutoDbContext _context = new();

        public ObservableCollection<Movimiento> Movimientos { get; set; } = new();
        public ObservableCollection<Alumno> Alumnos { get; set; } = new();
        public ObservableCollection<string> Cursos { get; set; } = new();
        public ObservableCollection<string> Conceptos { get; set; } = new();
        public ObservableCollection<string> Meses { get; } = new();

        private ICollectionView _movimientosView = null!;
        public ICollectionView MovimientosView
        {
            get => _movimientosView;
            set
            {
                _movimientosView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CantRegistros));
                OnPropertyChanged(nameof(TotalImportes));
                OnPropertyChanged(nameof(BalanceActual));
                OnPropertyChanged(nameof(EstadoBalance));
                OnPropertyChanged(nameof(BalanceForeground));
                OnPropertyChanged(nameof(BalanceVisibility));
            }
        }

        public int CantRegistros => MovimientosView?.Cast<object>().Count() ?? 0;
        public decimal TotalImportes => MovimientosView?.Cast<Movimiento>().Sum(m => m.Importe) ?? 0;
        public decimal BalanceActual => AlumnoSeleccionado == null
            ? 0
            : Movimientos.Where(m => m.IdAlumno == AlumnoSeleccionado.Id).Sum(m => m.Importe);
        public string EstadoBalance => BalanceActual switch
        {
            > 0 => "Deuda",
            < 0 => "Saldo a favor",
            _ => "Sin deuda"
        };
        public Brush BalanceForeground => BalanceActual switch
        {
            > 0 => Brushes.Red,
            < 0 => Brushes.Green,
            _ => Brushes.Black
        };
        public Visibility BalanceVisibility => AlumnoSeleccionado == null ? Visibility.Collapsed : Visibility.Visible;

        private Alumno? _alumnoSeleccionado;
        public Alumno? AlumnoSeleccionado
        {
            get => _alumnoSeleccionado;
            set
            {
                _alumnoSeleccionado = value;
                OnPropertyChanged();
                MovimientosView?.Refresh();
                ActualizarTotales();
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
                MovimientosView?.Refresh();
                ActualizarTotales();
            }
        }

        private string? _conceptoSeleccionado;
        public string? ConceptoSeleccionado
        {
            get => _conceptoSeleccionado;
            set
            {
                _conceptoSeleccionado = value;
                OnPropertyChanged();
                MovimientosView?.Refresh();
                ActualizarTotales();
            }
        }

        private string? _mesSeleccionado;
        public string? MesSeleccionado
        {
            get => _mesSeleccionado;
            set
            {
                _mesSeleccionado = value;
                OnPropertyChanged();
                MovimientosView?.Refresh();
                ActualizarTotales();
            }
        }

        public MovimientosViewModel()
        {
            CargarDatos();
        }

        private void CargarDatos()
        {
            var movimientosDb = _context.Movimientos
                .Include(m => m.IdAlumnoNavigation)
                    .ThenInclude(a => a.IdCursoNavigation)
                .Include(m => m.IdPagoNavigation)
                .OrderByDescending(m => m.Fecha)
                .ThenBy(m => m.IdAlumnoNavigation.Nombre)
                .AsNoTracking()
                .ToList();

            Movimientos = new ObservableCollection<Movimiento>(movimientosDb);

            Alumnos = new ObservableCollection<Alumno>(
                movimientosDb
                    .Select(m => m.IdAlumnoNavigation)
                    .DistinctBy(a => a.Id)
                    .OrderBy(a => a.Nombre));

            Cursos = new ObservableCollection<string>(
                movimientosDb
                    .Select(m => m.IdAlumnoNavigation.IdCursoNavigation)
                    .DistinctBy(c => c.Id)
                    .OrderBy(c => c.Orden)
                    .Select(c => c.Nombre));
            Cursos.Insert(0, "Todos");

            Conceptos = new ObservableCollection<string>(
                movimientosDb
                    .Select(m => m.Concepto)
                    .Distinct()
                    .OrderBy(c => c));
            Conceptos.Insert(0, "Todos");

            CargarMeses(movimientosDb);

            MovimientosView = CollectionViewSource.GetDefaultView(Movimientos);
            MovimientosView.Filter = FiltrarMovimientos;
            MovimientosView.CollectionChanged += (s, e) => ActualizarTotales();
            MovimientosView.CurrentChanged += (s, e) => ActualizarTotales();

            AlumnoSeleccionado = null;
            CursoSeleccionado = Cursos.FirstOrDefault();
            ConceptoSeleccionado = Conceptos.FirstOrDefault();
            MesSeleccionado = Meses.FirstOrDefault();

            MovimientosView.Refresh();
            ActualizarTotales();
        }

        private void CargarMeses(IEnumerable<Movimiento> movimientos)
        {
            Meses.Clear();
            Meses.Add("Todos");

            foreach (var mes in movimientos
                .Select(m => new DateTime(m.Fecha.Year, m.Fecha.Month, 1))
                .Distinct()
                .OrderByDescending(f => f))
            {
                var nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(mes.Month);
                Meses.Add($"{nombreMes}-{mes:yy}".ToLower());
            }
        }

        private bool FiltrarMovimientos(object obj)
        {
            if (obj is not Movimiento movimiento) return false;

            if (AlumnoSeleccionado != null && movimiento.IdAlumno != AlumnoSeleccionado.Id)
                return false;

            if (!string.IsNullOrWhiteSpace(CursoSeleccionado) && CursoSeleccionado != "Todos" &&
                !movimiento.IdAlumnoNavigation.IdCursoNavigation.Nombre.Equals(CursoSeleccionado, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(ConceptoSeleccionado) && ConceptoSeleccionado != "Todos" &&
                !movimiento.Concepto.Equals(ConceptoSeleccionado, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(MesSeleccionado) && MesSeleccionado != "Todos")
            {
                var fecha = DateTime.ParseExact(MesSeleccionado, "MMM-yy", CultureInfo.CurrentCulture);
                if (movimiento.Fecha.Month != fecha.Month || movimiento.Fecha.Year != fecha.Year)
                    return false;
            }

            return true;
        }

        private void ActualizarTotales()
        {
            OnPropertyChanged(nameof(CantRegistros));
            OnPropertyChanged(nameof(TotalImportes));
            OnPropertyChanged(nameof(BalanceActual));
            OnPropertyChanged(nameof(EstadoBalance));
            OnPropertyChanged(nameof(BalanceForeground));
            OnPropertyChanged(nameof(BalanceVisibility));
        }

        public IEnumerable GetExportableData()
        {
            return MovimientosView.Cast<Movimiento>();
        }
    }
}

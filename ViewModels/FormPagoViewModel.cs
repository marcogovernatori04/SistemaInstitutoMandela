using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Generators;
using SistemaInstituto.Models;
using SistemaInstituto.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class FormPagoViewModel : BaseViewModel
    {
        private readonly InstitutoDbContext _context = new();

        public Action<bool?> CloseAction { get; set; }

        public ObservableCollection<Alumno> Alumnos { get; set; } = new();
        public ObservableCollection<Arancel> Aranceles { get; set; } = new();
        public ObservableCollection<MetodoPago> MetodosPago { get; set; } = new();

        public Pago? PagoGuardado { get; private set; } = null!;

        private Alumno? _alumnoSeleccionado;
        public Alumno? AlumnoSeleccionado
        {
            get => _alumnoSeleccionado;
            set
            {
                _alumnoSeleccionado = value;
                OnPropertyChanged(nameof(AlumnoSeleccionado));
                OnAlumnoSeleccionado();
            }
        }

        private Arancel? _arancelSeleccionado;
        public Arancel? ArancelSeleccionado
        {
            get => _arancelSeleccionado;
            set
            {
                _arancelSeleccionado = value;
                OnPropertyChanged(nameof(ArancelSeleccionado));
                CalcularMonto();
            }
        }

        private DateTime _fechaPago { get; set; } = DateTime.Today;
        public DateTime FechaPago
        {
            get => _fechaPago;
            set
            {
                if (_fechaPago == value) return;

                _fechaPago = value;
                OnPropertyChanged(nameof(FechaPago));

                if (AlumnoSeleccionado != null)
                {
                    CargarArancelesVigentes();
                    CalcularMonto();
                }
            }
        }

        public string Concepto { get; set; } = string.Empty;

        private decimal? _montoPagado;
        public decimal? MontoPagado
        {
            get => _montoPagado;
            set
            {
                _montoPagado = value;
                OnPropertyChanged(nameof(MontoPagado));
            }
        }

        private decimal _montoSugerido;
        public decimal MontoSugerido
        {
            get => _montoSugerido;
            set
            {
                _montoSugerido = value;
                OnPropertyChanged(nameof(MontoSugerido));
            }
        }

        private decimal _montoOriginal;
        public decimal MontoOriginal
        {
            get => _montoOriginal;
            set
            {
                _montoOriginal = value;
                OnPropertyChanged(nameof(MontoOriginal));
                OnPropertyChanged(nameof(EsDescontada));
            }
        }

        private decimal _saldoActual;
        public decimal SaldoActual
        {
            get => _saldoActual;
            set
            {
                _saldoActual = value;
                OnPropertyChanged(nameof(SaldoActual));
                OnPropertyChanged(nameof(SaldoVisual));
            }
        }

        public decimal SaldoVisual => -SaldoActual;

        private decimal _ultimoMontoConDescuento;

        public MetodoPago? MetodoPagoSeleccionado { get; set; } = null!;
        public bool EsDescontada => _montoOriginal != 0 && _montoOriginal != _montoSugerido;
        public string Notas { get; set; } = string.Empty;

        public string ExistenciaBeneficio
        {
            get
            {
                if (!PoseeBeneficio(AlumnoSeleccionado?.Id ?? 0))
                    return "No";
                else if (EsDescontada)
                    return "Sí (aplica)";
                else
                    return "Sí (no aplica)";
            }
        }

        public ICommand GuardarCommand { get; }

        public FormPagoViewModel()
        {
            var alumnosDb = _context.Alumnos
                .Include(a => a.IdCursoNavigation)
                .AsNoTracking()
                .OrderBy(a => a.Nombre)
                .ToList();

            Alumnos = new ObservableCollection<Alumno>(alumnosDb);

            MetodosPago = new ObservableCollection<MetodoPago>(_context.MetodosPagos.ToList());

            GuardarCommand = new RelayCommand(o => GuardarPago());
        }

        private void CargarArancelesVigentes()
        {
            Aranceles.Clear();
            ArancelSeleccionado = null;
            MontoSugerido = 0;

            if (AlumnoSeleccionado == null) return;

            var fechaPago = DateOnly.FromDateTime(FechaPago);

            var arancelesVigentes = _context.Aranceles
                .Where(ar =>
                    (ar.IdCurso == AlumnoSeleccionado.IdCurso || ar.IdCurso == null) &&
                    ar.VigenciaDesde <= fechaPago &&
                    (ar.VigenciaHasta == null || ar.VigenciaHasta >= fechaPago))
                .OrderBy(ar => ar.Monto)
                .AsNoTracking()
                .ToList();

            foreach (var ar in arancelesVigentes)
            {
                Aranceles.Add(ar);
            }

            if (Aranceles.Count > 1)
                ArancelSeleccionado = Aranceles[2];
            else
                ArancelSeleccionado = Aranceles.FirstOrDefault();
        }

        private void OnAlumnoSeleccionado()
        {
            if (AlumnoSeleccionado != null)
            {
                CargarArancelesVigentes();
                CalcularMonto();

                SaldoActual = _context.Movimientos
                    .Where(m => m.IdAlumno == AlumnoSeleccionado.Id)
                    .Sum(m => m.Importe);
            }
            else
            {
                SaldoActual = 0;
            }

            OnPropertyChanged(nameof(ExistenciaBeneficio));
        }

        private decimal ObtenerMontoFinalArancel()
        {
            if (AlumnoSeleccionado == null || ArancelSeleccionado == null)
                return 0m;

            decimal montoArancel = ArancelSeleccionado.Monto ?? 0m;
            decimal montoFinal;

            bool esBeneficiario = PoseeBeneficio(AlumnoSeleccionado.Id);
            bool cursoAdmite = AlumnoSeleccionado.IdCursoNavigation?.Beneficio ?? false;
            bool cuotaMasBarata = AplicaDescuentoBeneficio(AlumnoSeleccionado.Id);

            if (esBeneficiario && cursoAdmite && cuotaMasBarata)
            {
                var beneficioId = _context.AlumnosBeneficios
                    .Where(ab => ab.IdAlumno == AlumnoSeleccionado.Id)
                    .Select(ab => (int?)ab.IdBeneficio)
                    .FirstOrDefault();

                var descuento = _context.Beneficios
                    .Where(b => b.Id == beneficioId)
                    .Select(b => b.Descuento)
                    .FirstOrDefault();

                montoFinal = Math.Round(montoArancel * (1 - (descuento / 100m)), 2);
            }
            else
            {
                montoFinal = montoArancel;
            }

            _ultimoMontoConDescuento = montoFinal;
            return montoFinal;
        }

        private void CalcularMonto()
        {
            if (AlumnoSeleccionado == null || ArancelSeleccionado == null)
            {
                MontoSugerido = 0;
                MontoOriginal = 0;
                return;
            }

            bool esPagoManual = ArancelSeleccionado.Monto == null;
            decimal montoFinal = esPagoManual ? (MontoPagado ?? 0m) : ObtenerMontoFinalArancel();

            MontoOriginal = (!esPagoManual && montoFinal != ArancelSeleccionado.Monto) ? ArancelSeleccionado.Monto ?? 0 : 0;

            decimal saldoAcumulado = _context.Movimientos
                .Where(m => m.IdAlumno == AlumnoSeleccionado.Id)
                .Sum(m => m.Importe);

            MontoSugerido = esPagoManual ? (MontoPagado ?? 0m) : montoFinal + Math.Max(0, saldoAcumulado);


            OnPropertyChanged(nameof(EsDescontada));
            OnPropertyChanged(nameof(ExistenciaBeneficio));
        }

        private bool PoseeBeneficio(int idAlumno)
        {
            return _context.AlumnosBeneficios.Any(ab => ab.IdAlumno == idAlumno);
        }

        private bool AplicaDescuentoBeneficio(int idAlumno)
        {
            return _context.AlumnosBeneficios.Any(ab => ab.IdAlumno == idAlumno && ab.AplicaDescuento);
        }

        private bool PuedeGuardar()
        {
            if (AlumnoSeleccionado == null
                || ArancelSeleccionado == null
                || MetodoPagoSeleccionado == null
                || FechaPago == default
                || MontoPagado <= 0
                || string.IsNullOrWhiteSpace(Concepto))
                return false;

            return true;
        }

        private void GuardarPago()
        {
            bool esPagoManual = ArancelSeleccionado?.Monto == null;

            if (!PuedeGuardar())
            {
                MessageBox.Show("Los datos del pago no son válidos.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nuevoPago = new Pago
            {
                IdAlumno = AlumnoSeleccionado!.Id,
                IdArancel = ArancelSeleccionado!.Id,
                FechaPago = DateOnly.FromDateTime(FechaPago),
                Concepto = TextNormalizer.ToTitleCase(Concepto),
                MontoPagado = MontoPagado ?? 0,
                IdMetodoPago = MetodoPagoSeleccionado!.Id,
                Notas = TextNormalizer.ToTitleCaseOrNull(Notas)
            };

            _context.Pagos.Add(nuevoPago);
            _context.SaveChanges();

            decimal importeMovimiento = esPagoManual ? MontoPagado ?? 0m : _ultimoMontoConDescuento;

            var movimientoCargo = new Movimiento
            {
                IdAlumno = AlumnoSeleccionado.Id,
                Fecha = DateTime.Now,
                Importe = importeMovimiento,
                Concepto = TextNormalizer.ToSentenceCase(esPagoManual ? "Cargo manual" : $"Cargo arancel {ArancelSeleccionado.Concepto}"),
                IdPago = nuevoPago.Id,
            };

            var movimientoAbono = new Movimiento
            {
                IdAlumno = AlumnoSeleccionado.Id,
                Fecha = DateTime.Now,
                Importe = -(MontoPagado ?? 0),
                Concepto = TextNormalizer.ToSentenceCase("Pago recibido"),
                IdPago = nuevoPago.Id,
            };

            _context.Movimientos.Add(movimientoCargo);
            _context.Movimientos.Add(movimientoAbono);
            _context.SaveChanges();

            SaldoActual = _context.Movimientos
                .Where(m => m.IdAlumno == AlumnoSeleccionado.Id)
                .Sum(m => m.Importe);

            PagoGuardado = _context.Pagos
                .Include(p => p.IdAlumnoNavigation)
                    .ThenInclude(a => a.IdCursoNavigation)
                .Include(p => p.IdMetodoPagoNavigation)
                .Include(p => p.IdArancelNavigation)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == nuevoPago.Id);

            MessageBox.Show("Pago guardado correctamente.");
            FacturaGenerator.GenerarFactura(PagoGuardado);
            CloseAction?.Invoke(true);
        }
    }
}

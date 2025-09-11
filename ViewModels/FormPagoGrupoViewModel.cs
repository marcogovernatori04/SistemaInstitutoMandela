using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Generators;
using SistemaInstituto.Models;
using SistemaInstituto.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class FormPagoGrupoViewModel : BaseViewModel
    {
        private readonly InstitutoDbContext _context = new();
        public Action<bool?> CloseAction { get; set; }

        public ObservableCollection<Beneficio> Beneficios { get; set; } = new();

        private Beneficio? _beneficioSeleccionado;
        public Beneficio? BeneficioSeleccionado
        {
            get => _beneficioSeleccionado;
            set
            {
                _beneficioSeleccionado = value;
                OnPropertyChanged(nameof(BeneficioSeleccionado));
                CargarAlumnos();
                SaldoAFavorTotal = Alumnos.Sum(a => a.SaldoAFavorInicial);
            }
        }

        private ObservableCollection<string> _conceptosArancel { get; set; } = new();
        public ObservableCollection<string> ConceptosArancel
        {
            get => _conceptosArancel;
            set { _conceptosArancel = value; OnPropertyChanged(nameof(ConceptosArancel)); }
        }

        private string? _conceptoArancelSeleccionado;
        public string? ConceptoArancelSeleccionado
        {
            get => _conceptoArancelSeleccionado;
            set
            {
                _conceptoArancelSeleccionado = value;
                OnPropertyChanged(nameof(ConceptoArancelSeleccionado));
                CalcularMontosSegunFecha();
            }
        }

        public ObservableCollection<AlumnoPagoInfo> Alumnos { get; set; } = new();

        private decimal _totalCalculado;
        public decimal TotalCalculado
        {
            get => _totalCalculado;
            set
            { 
                _totalCalculado = value; 
                OnPropertyChanged(nameof(TotalCalculado));
                OnPropertyChanged(nameof(EsDescontada));
            }
        }

        private decimal _totalSugerido;
        public decimal TotalSugerido
        {
            get => _totalSugerido;
            set {
                _totalSugerido = value; 
                OnPropertyChanged(nameof(TotalSugerido));
                OnPropertyChanged(nameof(EsDescontada));
            }
        }

        private decimal? _montoPagado;
        public decimal? MontoPagado
        {
            get => _montoPagado;
            set { _montoPagado = value; OnPropertyChanged(nameof(MontoPagado)); }
        }

        private DateTime? _fechaPagoADateTime;
        public DateTime? FechaPagoADateTime
        {
            get => _fechaPagoADateTime;
            set
            {
                _fechaPagoADateTime = value;
                OnPropertyChanged(nameof(FechaPagoADateTime));
                FechaPago = value.HasValue ? DateOnly.FromDateTime(value.Value) : DateOnly.FromDateTime(DateTime.Today);
            }
        }

        private DateOnly _fechaPago = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly FechaPago
        {
            get => _fechaPago;
            set
            {
                _fechaPago = value;
                OnPropertyChanged(nameof(FechaPago));
                if (BeneficioSeleccionado != null && ConceptoArancelSeleccionado != null)
                    CalcularMontosSegunFecha();
            }
        }

        public ObservableCollection<MetodoPago> MetodosPago { get; set; } = new();

        private MetodoPago? _metodoPagoSeleccionado;
        public MetodoPago? MetodoPagoSeleccionado
        {
            get => _metodoPagoSeleccionado;
            set { _metodoPagoSeleccionado = value; OnPropertyChanged(nameof(MetodoPagoSeleccionado)); }
        }

        private decimal _saldoAFavorTotal;
        public decimal SaldoAFavorTotal
        {
            get => _saldoAFavorTotal;
            set 
            { 
                _saldoAFavorTotal = value; 
                OnPropertyChanged(nameof(SaldoAFavorTotal));
                OnPropertyChanged(nameof(SaldoVisualTotal));
            }
        }

        public decimal SaldoVisualTotal => -SaldoAFavorTotal;
        public bool EsDescontada => TotalSugerido < TotalCalculado;

        public string Notas { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;

       

        public Pago? PagoGuardado { get; private set; } = null!;
        public List<Pago> PagosGuardados { get; private set; } = new();
        public ICommand GuardarCommand { get; }

        public FormPagoGrupoViewModel()
        {

            var beneficiosDb = _context.Beneficios
                .Include(b => b.AlumnosBeneficios)
                    .ThenInclude(ab => ab.IdAlumnoNavigation)
                        .ThenInclude(a => a.IdCursoNavigation)
                .OrderBy(b => b.Nombre)
                .ToList();

            Beneficios = new ObservableCollection<Beneficio>(beneficiosDb);
            MetodosPago = new ObservableCollection<MetodoPago>(_context.MetodosPagos.ToList());

            FechaPago = DateOnly.FromDateTime(DateTime.Today);
            FechaPagoADateTime = DateTime.Today;

            GuardarCommand = new RelayCommand(o => GuardarPagos());
        }

        private void CargarAlumnos()
        {
            Alumnos.Clear();
            ConceptosArancel.Clear();
            TotalCalculado = 0m;
            TotalSugerido = 0m;

            if (BeneficioSeleccionado == null) return;

            foreach (var ab in BeneficioSeleccionado.AlumnosBeneficios)
            {
                var alumno = ab.IdAlumnoNavigation;
                if (alumno == null) continue;

                Alumnos.Add(new AlumnoPagoInfo
                {
                    IdAlumno = alumno.Id,
                    IdCurso = alumno.IdCurso,
                    Nombre = alumno.Nombre,
                    Curso = alumno.IdCursoNavigation.Nombre,
                    Aplica = "No",
                    MontoFinal = 0
                });
            }

            var conceptosArancelDb = _context.Aranceles
                    .Where(a => Alumnos.Select(x => x.IdCurso).Contains(a.IdCurso))
                    .GroupBy(a => a.Concepto)
                    .Select(g => new { Concepto = g.Key, MontoMinimo = g.Min(a => a.Monto) })
                    .OrderBy(x => x.MontoMinimo)
                    .Select(x => x.Concepto)
                    .ToList();

            foreach (var c in conceptosArancelDb)
                ConceptosArancel.Add(c);

            ConceptoArancelSeleccionado = ConceptosArancel.Count > 1 ? ConceptosArancel[1] : ConceptosArancel.FirstOrDefault();
        }

        private void CalcularMontosSegunFecha()
        {
            if (ConceptoArancelSeleccionado == null || BeneficioSeleccionado == null) return;

            var alumnosAranceles = BeneficioSeleccionado.AlumnosBeneficios
                .Select(ab => new
                {
                    Alumno = ab.IdAlumnoNavigation!,
                    Arancel = _context.Aranceles.FirstOrDefault(a =>
                        a.IdCurso == ab.IdAlumnoNavigation!.IdCurso &&
                        a.Concepto == ConceptoArancelSeleccionado &&
                        a.VigenciaDesde <= FechaPago &&
                        (a.VigenciaHasta == null || a.VigenciaHasta >= FechaPago))
                })
                .Where(x => x.Arancel != null)
                .ToList();

            var ordenados = alumnosAranceles.OrderBy(x => x.Arancel!.Monto ?? decimal.MaxValue).ToList();

            // Determinar cuántos alumnos aplican descuento según cantidad de alumnos
            int descuentos = ordenados.Count == 2 ? 1 : ordenados.Count == 3 ? 2 : 0;

            Alumnos.Clear();
            decimal totalBruto = 0m;
            decimal totalNeto = 0m;
            SaldoAFavorTotal = 0m;

            foreach (var (entry, index) in ordenados.Select((e, i) => (e, i)))
            {
                var alumno = entry.Alumno;
                var arancel = entry.Arancel!;
                decimal montoBase = arancel.Monto ?? 0m;

                bool esPagoManual = arancel?.Monto == null;
                bool aplicaDescuento = false;

                decimal montoConDesc;

                if (esPagoManual)
                {
                    montoConDesc = MontoPagado ?? 0m;
                }
                else
                {
                    bool cursoPermite = alumno.IdCursoNavigation!.Beneficio;
                    aplicaDescuento = (index < descuentos) && cursoPermite;

                    montoConDesc = aplicaDescuento
                        ? decimal.Round(montoBase * (1 - (BeneficioSeleccionado.Descuento / 100m)), 2)
                        : montoBase;
                }

                totalBruto += montoConDesc;

                decimal saldoActual = _context.Movimientos
                    .Where(m => m.IdAlumno == alumno.Id)
                    .Sum(m => m.Importe);

                // Determinar saldo usado y neto a pagar
                decimal saldoUsado, netoAPagar;

                if (esPagoManual)
                {
                    saldoUsado = 0;
                    netoAPagar = montoConDesc;
                }
                else if (saldoActual < 0) // saldo a favor
                {
                    saldoUsado = Math.Min(-saldoActual, montoConDesc);
                    netoAPagar = montoConDesc - saldoUsado;
                }
                else if (saldoActual > 0) // saldo deudor
                {
                    saldoUsado = 0;
                    netoAPagar = montoConDesc + saldoActual;
                }
                else
                {
                    saldoUsado = 0;
                    netoAPagar = montoConDesc;
                }

                var info = new AlumnoPagoInfo
                {
                    IdAlumno = alumno.Id,
                    IdCurso = alumno.IdCurso,
                    Nombre = alumno.Nombre,
                    Curso = alumno.IdCursoNavigation.Nombre,
                    Aplica = aplicaDescuento ? "Sí" : "No",
                    MontoArancel = montoConDesc,
                    SaldoAFavorInicial = saldoActual,
                    SaldoUsado = saldoUsado,
                    SaldoAFavorNuevo = saldoActual - saldoUsado,
                    MontoAPagar = netoAPagar
                };

                Alumnos.Add(info);
                totalNeto += netoAPagar;
                SaldoAFavorTotal += saldoActual;
            }

            TotalCalculado = decimal.Round(totalBruto, 2);
            TotalSugerido = decimal.Round(totalNeto, 2);
        }

        private void GuardarPagos()
        {
            if (ConceptoArancelSeleccionado == null || MetodoPagoSeleccionado == null)
            {
                MessageBox.Show("Debe seleccionar un arancel, un método de pago y una fecha de pago.");
                return;
            }

            if (MontoPagado == null || MontoPagado <= 0)
            {
                MessageBox.Show("Debe ingresar un monto pagado válido.");
                return;
            }

            decimal montoIngresado = MontoPagado ?? 0m;
            decimal totalNeto = TotalSugerido;

            if (totalNeto == 0m)
            {
                foreach (var alumno in Alumnos)
                {
                    alumno.MontoAPagar = MontoPagado / 2 ?? 0m; 
                    alumno.SaldoAFavorNuevo = -(alumno.MontoAPagar);
                }
            }
            else
            {
                decimal sumaAsignada = 0m;

                foreach (var alumno in Alumnos)
                {
                    decimal proporcion = alumno.MontoAPagar / totalNeto;
                    decimal montoProporcional = proporcion * (MontoPagado ?? 0m);
                    decimal montoRedondeado = Math.Round(montoProporcional, 0, MidpointRounding.AwayFromZero);

                    alumno.MontoAPagar = montoRedondeado;
                    alumno.SaldoAFavorNuevo = alumno.SaldoAFavorInicial - (alumno.MontoArancel - montoRedondeado);

                    sumaAsignada += montoRedondeado;
                }

                decimal diferencia = (MontoPagado ?? 0m) - sumaAsignada;
                if (diferencia != 0 && Alumnos.Count > 0)
                {
                    Alumnos.Last().MontoAPagar += diferencia;
                    Alumnos.Last().SaldoAFavorNuevo += diferencia;
                }
            }

            PagosGuardados.Clear();
            foreach (var alumno in Alumnos)
            {
                var arancel = _context.Aranceles.FirstOrDefault(a =>
                    a.IdCurso == alumno.IdCurso &&
                    a.Concepto == ConceptoArancelSeleccionado &&
                    a.VigenciaDesde <= FechaPago &&
                    (a.VigenciaHasta == null || a.VigenciaHasta >= FechaPago));

                if (arancel == null) continue;

                var nuevoPago = new Pago
                {
                    IdAlumno = alumno.IdAlumno,
                    IdArancel = arancel.Id,
                    FechaPago = FechaPago,
                    Concepto = Concepto,
                    MontoPagado = alumno.MontoAPagar,
                    IdMetodoPago = MetodoPagoSeleccionado.Id,
                    Notas = Notas
                };

                _context.Pagos.Add(nuevoPago);
                _context.SaveChanges();

                if (arancel != null)
                {
                    var movimientoCargo = new Movimiento
                    {
                        IdAlumno = alumno.IdAlumno,
                        Fecha = DateTime.Now,
                        Importe = alumno.MontoArancel,
                        Concepto = $"Cargo arancel {arancel.Concepto}",
                        IdPago = nuevoPago.Id
                    };

                    _context.Movimientos.Add(movimientoCargo);

                }

                var movimientoAbono = new Movimiento
                {
                    IdAlumno = alumno.IdAlumno,
                    Fecha = DateTime.Now,
                    Importe = -alumno.MontoAPagar,
                    Concepto = arancel == null ? "Pago año completo" : "Pago recibido",
                    IdPago = nuevoPago.Id
                };


                _context.Movimientos.Add(movimientoAbono);
                _context.SaveChanges();

                PagoGuardado = _context.Pagos
                    .Include(p => p.IdAlumnoNavigation)
                        .ThenInclude(a => a.IdCursoNavigation)
                    .Include(p => p.IdAlumnoNavigation)
                        .ThenInclude(a => a.AlumnosBeneficio)
                            .ThenInclude(ab => ab.IdBeneficioNavigation)
                    .Include(p => p.IdMetodoPagoNavigation)
                    .Include(p => p.IdArancelNavigation)
                    .AsNoTracking()
                    .FirstOrDefault(p => p.Id == nuevoPago.Id);

                if (PagoGuardado != null)
                    PagosGuardados.Add(PagoGuardado);
            }

            MessageBox.Show($"{PagosGuardados.Count} pagos guardados correctamente.");
            FacturaGenerator.GenerarFacturaGrupal(PagosGuardados);
            CloseAction?.Invoke(true);
        }

        public class AlumnoPagoInfo : BaseViewModel
        {
            public int IdAlumno { get; set; }
            public int IdCurso { get; set; }
            public string Nombre { get; set; } = null!;
            public string Curso { get; set; } = null!;
            public string Aplica { get; set; } = "No";

            public decimal MontoArancel { get; set; }
            public decimal SaldoAFavorInicial { get; set; }
            public decimal SaldoUsado { get; set; }

            private decimal _saldoAFavorNuevo;
            public decimal SaldoAFavorNuevo
            {
                get => _saldoAFavorNuevo;
                set { _saldoAFavorNuevo = value; OnPropertyChanged(nameof(SaldoAFavorNuevo)); OnPropertyChanged(nameof(SaldoAFavor)); }
            }

            private decimal _montoAPagar;
            public decimal MontoAPagar
            {
                get => _montoAPagar;
                set { _montoAPagar = value; OnPropertyChanged(nameof(MontoAPagar)); OnPropertyChanged(nameof(MontoFinal)); }
            }

            public decimal MontoFinal
            {
                get => MontoAPagar;
                set { MontoAPagar = value; }
            }

            public decimal SaldoAFavor
            {
                get => SaldoAFavorNuevo;
                set { SaldoAFavorNuevo = value; }
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class BeneficioDetalleViewModel : BaseViewModel
    {

        private Beneficio _beneficio = null!;

        public string BeneficioNombre { get; set; } = string.Empty;
        public ObservableCollection<AlumnoBeneficioItem> Alumnos { get; } = new();

        private decimal _cuotaDescontada;
        public decimal CuotaDescontada
        {
            get => _cuotaDescontada;
            set { _cuotaDescontada = value; OnPropertyChanged(); }
        }

        public int BeneficioId { get; private set; }
        public ICommand EditarBeneficioCommand { get; }

        public BeneficioDetalleViewModel(int beneficioId)
        {

            BeneficioId = beneficioId;
            EditarBeneficioCommand = new RelayCommand(_ => EditarBeneficio());
            CargarBeneficio();
        }

        private void CargarBeneficio()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            using var context = new InstitutoDbContext();
            _beneficio = context.Beneficios
                .Include(b => b.AlumnosBeneficios)
                    .ThenInclude(ab => ab.IdAlumnoNavigation)
                        .ThenInclude(a => a.IdCursoNavigation)
                .FirstOrDefault(b => b.Id == BeneficioId)!;

            if (_beneficio == null) return;

            var alumnos = _beneficio.AlumnosBeneficios
                .Select(ab => ab.IdAlumnoNavigation)
                .Where(a => a != null)
                .ToList();

            if (!alumnos.Any()) return;

            var cursosIds = alumnos.Select(a => a.IdCurso).Distinct().ToList();

            var hoyAranceles = context.Aranceles
                .Where(ar => ar.IdCurso == null || cursosIds.Contains(ar.IdCurso.Value))
                .Where(ar => ar.VigenciaDesde <= hoy && (ar.VigenciaHasta == null || ar.VigenciaHasta >= hoy))
                .GroupBy(ar => ar.IdCurso)
                .ToDictionary(g => g.Key ?? 0, g => g.Min(ar => ar.Monto) ?? 0m);

            BeneficioNombre = _beneficio.Nombre ?? $"Beneficio #{_beneficio.Id}";
            OnPropertyChanged(nameof(BeneficioNombre));

            Alumnos.Clear();
            foreach (var a in alumnos)
            {
                var curso = a.IdCursoNavigation;
                var cuotaBase = hoyAranceles.TryGetValue(a.IdCurso, out var m) ? m : 0m; // obtengo la cuota base para el curso del alumno desde el diccionario de aranceles, si no existe el curso en el diccionario asigno 0 como cuota base

                Alumnos.Add(new AlumnoBeneficioItem
                {
                    IdCurso = a.IdCurso,
                    CursoAdmiteBeneficio = curso?.Beneficio ?? false,
                    Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? "-" : a.Nombre,
                    Curso = curso?.Nombre ?? "—",
                    CuotaBase = cuotaBase,
                    CuotaFinal = cuotaBase
                });
            }

            AplicarDescuento(_beneficio.Descuento, _beneficio.AlumnosBeneficios);

        }

        private void RecargarBeneficio()
        {
            Alumnos.Clear();
            CargarBeneficio();
        }

        private void AplicarDescuento(decimal descuento, IEnumerable<AlumnosBeneficio> alumnosBeneficios)
        {
            var factor = 1m - (descuento / 100m);

            foreach (var a in Alumnos)
            {
                var aplica = alumnosBeneficios.FirstOrDefault(ab => ab.IdAlumnoNavigation.IdCurso == a.IdCurso)?.AplicaDescuento ?? false;

                a.EsConDescuento = aplica;
                a.CuotaFinal = aplica ? Math.Round(a.CuotaBase * factor, 2, MidpointRounding.AwayFromZero) : a.CuotaBase;
            }

            CuotaDescontada = Alumnos.Where(a => a.EsConDescuento).Select(a => a.CuotaFinal).DefaultIfEmpty(0).Min();
        }

        private void EditarBeneficio()
        {
            using var context = new InstitutoDbContext();
            var beneficio = context.Beneficios
                .Include(b => b.AlumnosBeneficios)
                    .FirstOrDefault(b => b.Id == BeneficioId)!;
            var viewModel = new FormBeneficioViewModel(beneficio);
            var ventana = new Views.FormBeneficioView
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
            {
                RecargarBeneficio();
            }
        }
    }

    public class AlumnoBeneficioItem : BaseViewModel
    {
        public int IdCurso { get; set; }
        public bool CursoAdmiteBeneficio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Curso { get; set; } = string.Empty;

        private decimal _cuotaBase;
        public decimal CuotaBase
        {
            get => _cuotaBase;
            set { _cuotaBase = value; OnPropertyChanged(); }
        }

        private decimal _cuotaFinal;
        public decimal CuotaFinal
        {
            get => _cuotaFinal;
            set { _cuotaFinal = value; OnPropertyChanged(); }
        }

        private bool _esConDescuento;
        public bool EsConDescuento
        {
            get => _esConDescuento;
            set { _esConDescuento = value; OnPropertyChanged(); }
        }
    }
}

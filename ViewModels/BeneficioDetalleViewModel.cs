using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using System.Collections.ObjectModel;

namespace SistemaInstituto.ViewModels
{
    public class BeneficioDetalleViewModel : BaseViewModel
    {
        public string BeneficioNombre { get; set; } = string.Empty;
        public ObservableCollection<AlumnoBeneficioItem> Alumnos { get; } = new();

        private decimal _cuotaDescontada;
        public decimal CuotaDescontada
        {
            get => _cuotaDescontada;
            set { _cuotaDescontada = value; OnPropertyChanged(); }
        }

        public BeneficioDetalleViewModel(int beneficioId)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            using var context = new InstitutoDbContext();

            var beneficio = context.Beneficios
                .Include(b => b.AlumnosBeneficios)
                    .ThenInclude(ab => ab.IdAlumnoNavigation)
                        .ThenInclude(a => a.IdCursoNavigation)
                .FirstOrDefault(b => b.Id == beneficioId);

            if (beneficio == null) return;

            BeneficioNombre = beneficio.Nombre ?? $"Beneficio #{beneficio.Id}";

            var alumnos = beneficio.AlumnosBeneficios
                .Select(ab => ab.IdAlumnoNavigation)
                .Where(a => a != null)
                .ToList();

            if (!alumnos.Any()) return;

            var hoyAranceles = context.Aranceles
                .Where(ar => alumnos.Select(a => a.IdCurso).Contains(ar.IdCurso))
                .Where(ar => ar.VigenciaDesde <= hoy && (ar.VigenciaHasta == null || ar.VigenciaHasta >= hoy))
                .GroupBy(ar => ar.IdCurso)
                .ToDictionary(g => g.Key, g => g.Min(ar => ar.Monto) ?? 0m);

            Alumnos.Clear();
            foreach (var a in alumnos)
            {
                var curso = a.IdCursoNavigation;
                var cuotaBase = hoyAranceles.TryGetValue(a.IdCurso, out var m) ? m : 0m;

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

            AplicarDescuento(beneficio.Descuento);
        }

        private void AplicarDescuento(decimal descuento)
        {
            var elegibles = Alumnos
                .Where(a => a.CursoAdmiteBeneficio && a.CuotaBase > 0)
                .OrderBy(a => a.CuotaBase)
                .ToList();

            if (!elegibles.Any())
            {
                CuotaDescontada = 0;
                return;
            }

            var cantidadBeneficiados = Alumnos.Count >= 3 ? 2 : 1;
            var ganadores = elegibles.Take(cantidadBeneficiados).ToList();

            var factor = 1m - (descuento / 100m);

            foreach (var a in Alumnos)
            {
                if (ganadores.Contains(a))
                {
                    a.EsConDescuento = true;
                    a.CuotaFinal = Math.Round(a.CuotaBase * factor, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    a.EsConDescuento = false;
                    a.CuotaFinal = a.CuotaBase;
                }
            }

            CuotaDescontada = ganadores.Min(g => g.CuotaFinal);
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

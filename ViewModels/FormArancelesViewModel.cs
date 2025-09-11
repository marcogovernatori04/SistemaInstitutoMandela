using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class FormArancelesViewModel : BaseViewModel
    {
        private InstitutoDbContext _context = new();

        public ObservableCollection<CursoArancel> CursosAranceles { get; set; }

        public List<Arancel>? ArancelesGuardados { get; private set; } = null!;

        public ICommand GuardarCommand { get; }

        public FormArancelesViewModel()
        {
            CargarCursosAranceles();
            GuardarCommand = new RelayCommand(o => ActualizarAranceles());

        }

        private void CargarCursosAranceles()
        {
            var arancelesActivos = _context.Aranceles
                .Where(a => a.VigenciaHasta == null)
                .ToList();

            var cursos = _context.Cursos.ToList();

            CursosAranceles = new ObservableCollection<CursoArancel>(
                cursos.Select(c =>
                {
                    var arancelesCurso = arancelesActivos.Where(a => a.IdCurso == c.Id).ToList();

                    return new CursoArancel
                    {
                        CursoId = c.Id,
                        CursoNombre = c.Nombre,

                        HastaEl10 = arancelesCurso.FirstOrDefault(a => a.Concepto == "Hasta el 10")?.Monto,
                        Del10Al30 = arancelesCurso.FirstOrDefault(a => a.Concepto == "Del 10 al 30")?.Monto,
                        MesVencido = arancelesCurso.FirstOrDefault(a => a.Concepto == "Mes vencido")?.Monto,

                        OriginalHastaEl10 = arancelesCurso.FirstOrDefault(a => a.Concepto == "Hasta el 10")?.Monto,
                        OriginalDel10Al30 = arancelesCurso.FirstOrDefault(a => a.Concepto == "Del 10 al 30")?.Monto,
                        OriginalMesVencido = arancelesCurso.FirstOrDefault(a => a.Concepto == "Mes vencido")?.Monto
                    };
                })
            );
        }

        private void ActualizarAranceles()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            foreach (var ca in CursosAranceles)
            {
                if (ca.HastaEl10.HasValue && ca.HastaEl10 != ca.OriginalHastaEl10)
                    Actualizar(ca.CursoId, "Hasta el 10", ca.HastaEl10.Value, hoy);
                if (ca.Del10Al30.HasValue && ca.Del10Al30 != ca.OriginalDel10Al30)
                    Actualizar(ca.CursoId, "Del 10 al 30", ca.Del10Al30.Value, hoy);
                if (ca.MesVencido.HasValue && ca.MesVencido != ca.OriginalMesVencido)
                    Actualizar(ca.CursoId, "Mes vencido", ca.MesVencido.Value, hoy);

            }

            _context.SaveChanges();

            if (ArancelesGuardados != null)
            {
                ArancelesGuardados.Clear();
            }
            else
            {
                ArancelesGuardados = new List<Arancel>();
            }

            ArancelesGuardados = _context.Aranceles
                .Include(a => a.IdCursoNavigation)
                .Where(a => a.Monto != null)
                .OrderByDescending(a => a.Activo)
                .ThenBy(a => a.IdCurso)
                .ThenBy(a => a.Monto)
                .AsNoTracking()
                .ToList();


            var ventana = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (ventana != null)
            {
                ventana.DialogResult = true;
                ventana.Close();
            }
            else
            {
                MessageBox.Show("No se pudo cerrar la ventana correctamente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Actualizar(int cursoId, string concepto, decimal nuevoMonto, DateOnly hoy)
        {

            var arancelActual = _context.Aranceles
                .FirstOrDefault(a => a.IdCurso == cursoId && a.Concepto == concepto && a.VigenciaHasta == null);

            if (arancelActual == null || arancelActual.Monto == nuevoMonto)
                return;


            arancelActual.VigenciaHasta = hoy.AddDays(-1);
            arancelActual.Activo = false;

            var nuevoArancel = new Arancel
            {
                IdCurso = cursoId,
                Concepto = concepto,
                Monto = nuevoMonto,
                VigenciaDesde = hoy,
                VigenciaHasta = null,
                Activo = true
            };

            _context.Aranceles.Add(nuevoArancel);

        }
    }

    public class CursoArancel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = null!;
        public decimal? HastaEl10 { get; set; }
        public decimal? Del10Al30 { get; set; }
        public decimal? MesVencido { get; set; }

        public decimal? OriginalHastaEl10 { get; set; }
        public decimal? OriginalDel10Al30 { get; set; }
        public decimal? OriginalMesVencido { get; set; }
    }
}

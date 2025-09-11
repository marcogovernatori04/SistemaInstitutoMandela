using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class PerfilAlumnoViewModel : BaseViewModel
    {
        private readonly InstitutoDbContext _context = new();
        public ICommand EditarAlumnoCommand { get; }

        private Alumno _alumno = null!;
        public Alumno Alumno
        {
            get => _alumno;
            set
            {
                _alumno = value;
                OnPropertyChanged(nameof(Alumno));
            }
        }

        private string _nombre;
        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged(nameof(Nombre));
            }
        }

        private string _curso;
        public string Curso 
        { 
            get => _curso; 
            set
            {
                _curso = value;
                OnPropertyChanged(nameof(Curso));
            } 
        }

        private string _estado;
        public string Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged(nameof(Estado));
            }
        }

        private string _notas;
        public string Notas
        {
            get => _notas;
            set
            {
                _notas = value;
                OnPropertyChanged(nameof(Notas));
            }
        }

        private string _telefono;
        public string Telefono
        {
            get => _telefono;
            set
            {
                _telefono = value;
                OnPropertyChanged(nameof(Telefono));    
            }
        }

        public string ExistenciaBeneficio => CalcularExistenciaBeneficio();

        public ObservableCollection<Pago> Pagos { get; set; }

        public PerfilAlumnoViewModel(Alumno alumno)
        {
            _alumno = _context.Alumnos
                .Include(a => a.IdCursoNavigation)
                .Include(a => a.IdEstadoNavigation)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.IdArancelNavigation)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.IdMetodoPagoNavigation)
                .First(a => a.Id == alumno.Id);

            RecargarAlumno();

            Pagos = new ObservableCollection<Pago>(
                _alumno.Pagos.OrderByDescending(p => p.FechaPago));

            EditarAlumnoCommand = new RelayCommand(o => EditarAlumno());
        }

        private void EditarAlumno()
        {
            var viewModel = new FormAlumnoViewModel(_alumno);
            var ventana = new FormAlumnoView
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
            {
                Alumno = viewModel.AlumnoGuardado;
                RecargarAlumno();
                AlumnoActualizado?.Invoke(this, Alumno);


            }
        }

        private void RecargarAlumno()
        {
            Nombre = _alumno.Nombre;
            Curso = _alumno.IdCursoNavigation?.Nombre ?? "Desconocido";
            Estado = _alumno.IdEstadoNavigation?.Nombre ?? "Desconocido";
            Notas = _alumno.Notas ?? string.Empty;
            Telefono = _alumno.Telefono ?? string.Empty;
        }
      

        private string CalcularExistenciaBeneficio()
        {
            bool esBeneficiario = PoseeBeneficio(_alumno.Id);
            bool cursoAdmite = _alumno.IdCursoNavigation?.Beneficio ?? false;
            bool cuotaMasBarata = esBeneficiario && PoseeCuotaMasBarata(_alumno.Id);

            if (!esBeneficiario)
                return "No";
            else if (cursoAdmite && cuotaMasBarata)
                return "Sí (aplica)";
            else
                return "Sí (no aplica)";
                
        }

        private bool PoseeBeneficio(int idAlumno)
        {
            return _context.AlumnosBeneficios.Any(ab => ab.IdAlumno == idAlumno);
        }

        private bool PoseeCuotaMasBarata(int idAlumno)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var beneficioId = _context.AlumnosBeneficios
                .Where(ab => ab.IdAlumno == idAlumno)
                .Select(ab => (int?)ab.IdBeneficio)
                .FirstOrDefault();

            if (beneficioId.HasValue)
            {
                var cursosIdGrupo = _context.AlumnosBeneficios
                    .Where(ab => ab.IdBeneficio == beneficioId.Value)
                    .Select(ab => ab.IdAlumnoNavigation.IdCurso)
                    .Distinct().ToList();

                if (cursosIdGrupo.Count != 0)
                {
                    var minimoCurso = _context.Aranceles
                        .Where(ar =>
                            ar.VigenciaDesde <= hoy &&
                            (ar.VigenciaHasta == null || ar.VigenciaHasta >= hoy) &&
                            cursosIdGrupo.Contains(ar.IdCurso))
                        .GroupBy(ar => ar.IdCurso)
                        .Select(g => new { IdCurso = g.Key, MontoMinimo = g.Min(ar => ar.Monto) })
                        .AsNoTracking().ToList();

                    if (minimoCurso.Count != 0)
                    {
                        var minimoGrupo = minimoCurso.Min(x => x.MontoMinimo);
                        var minimoAlumno = minimoCurso
                            .Where(x => x.IdCurso == (_alumno?.IdCurso ?? 0))
                            .Select(x => (decimal?)x.MontoMinimo)
                            .FirstOrDefault() ?? decimal.MaxValue;

                        return minimoAlumno == minimoGrupo;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public static event EventHandler<Alumno>? AlumnoActualizado;
    }
}

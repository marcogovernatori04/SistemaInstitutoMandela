using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace SistemaInstituto.ViewModels
{
    public class AlumnosViewModel : BaseViewModel
    {
        public RelayCommand AgregarAlumnoCommand { get; }
        public RelayCommand ToggleInactivosCommand { get; }
        public ObservableCollection<Alumno> Alumnos { get; set; } = null!;
        public ObservableCollection<Curso> Cursos { get; set; } = null!;

        private InstitutoDbContext _context = new();

        public int CantRegistros => AlumnosView?.Cast<object>().Count() ?? 0;


        private ICollectionView _alumnosView { get; set; } = null!;
        public ICollectionView AlumnosView
        {
            get => _alumnosView;
            set
            {
                _alumnosView = value;
                OnPropertyChanged(nameof(AlumnosView));
                OnPropertyChanged(nameof(CantRegistros));
            }
        }


        private Curso _cursoFiltrado = null!;
        public Curso CursoFiltrado
        {
            get => _cursoFiltrado;
            set
            {
                _cursoFiltrado = value;
                OnPropertyChanged(nameof(CursoFiltrado));
                AlumnosView.Refresh();
            }
        }

        private string _busqNombre = null!;
        public string BusqNombre
        {
            get => _busqNombre;
            set
            {
                _busqNombre = value;
                OnPropertyChanged(nameof(BusqNombre));
                AlumnosView.Refresh();
            }
        }

        private bool _mostrarInactivos = false;
        public bool MostrarInactivos
        {
            get => _mostrarInactivos;
            set
            {
                _mostrarInactivos = value;
                OnPropertyChanged(nameof(MostrarInactivos));
                AlumnosView.Refresh();
            }
        }

        public AlumnosViewModel()
        {
            CargarDatos();

            PerfilAlumnoViewModel.AlumnoActualizado += (s, alumnoActualizado) =>
            {
                var alumno = Alumnos.FirstOrDefault(a => a.Id == alumnoActualizado.Id);
                if (alumno != null)
                {
                    alumno.Nombre = alumnoActualizado.Nombre;
                    alumno.IdCursoNavigation = alumnoActualizado.IdCursoNavigation;
                    alumno.IdEstadoNavigation = alumnoActualizado.IdEstadoNavigation;
                    alumno.Notas = alumnoActualizado.Notas;

                    OnPropertyChanged(nameof(Alumnos));
                }
            };

            AgregarAlumnoCommand = new RelayCommand(o => AgregarAlumno());
            ToggleInactivosCommand = new RelayCommand(o => MostrarInactivos = !MostrarInactivos);

            PerfilAlumnoViewModel.AlumnoActualizado += OnAlumnoActualizado;
        }

        private void AgregarAlumno()
        {
            var viewModel = new FormAlumnoViewModel();
            var ventana = new FormAlumnoView()
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                if (viewModel.AlumnoGuardado != null)
                {
                    Alumnos.Add(viewModel.AlumnoGuardado);
                    AlumnosView.Refresh();
                }
            }
        }

        private void CargarDatos()
        {
            var alumnosDb = _context.Alumnos.Include(a => a.IdCursoNavigation).Include(a => a.IdEstadoNavigation).OrderBy(a => a.IdCursoNavigation.Id).ToList();
            Alumnos = new ObservableCollection<Alumno>(alumnosDb);

            var listaCursos = alumnosDb.Select(a => a.IdCursoNavigation).Distinct().ToList();
            Cursos = new ObservableCollection<Curso>(listaCursos);

            Cursos.Insert(0, new Curso { Id = 0, Nombre = "Todos los cursos" });

            AlumnosView = CollectionViewSource.GetDefaultView(Alumnos);
            AlumnosView.Filter = FiltrarAlumnos;

            CursoFiltrado = Cursos.FirstOrDefault() ?? new Curso { Id = 0, Nombre = "Todos los cursos" };

            AlumnosView.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CantRegistros));
            };
        }

        private bool FiltrarAlumnos(object obj)
        {
            if (obj is Alumno alumno)
            {
                bool nombreCoincide = string.IsNullOrWhiteSpace(BusqNombre) || alumno.Nombre.ToLower().Contains(BusqNombre.ToLower());

                bool cursoCoincide = CursoFiltrado == null || CursoFiltrado.Id == 0 || alumno.IdCurso == CursoFiltrado.Id;

                bool estadoCoincide = MostrarInactivos || alumno.IdEstadoNavigation.Nombre == "Activo";

                return cursoCoincide && nombreCoincide && estadoCoincide;
            }
            return false;
        }

        private void OnAlumnoActualizado(object? sender, Alumno alumno)
        {
            var existente = Alumnos.FirstOrDefault(a => a.Id == alumno.Id);
            if (existente != null)
            {
                var idx = Alumnos.IndexOf(existente);
                Alumnos[idx] = alumno;
            }
        }
    }
}

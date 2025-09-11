using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class FormAlumnoViewModel : BaseViewModel
    {
        private InstitutoDbContext _context = new();

        public ICommand GuardarCommand { get; }

        public string Nombre { get; set; } = null!;
        public string? Telefono { get; set; }

        public ObservableCollection<Curso> Cursos { get; set; } 
        public ObservableCollection<Estado> Estados { get; set; } 
        public Curso? CursoSeleccionado { get; set; } = null!;
        public string? Notas { get; set; }

        public string Titulo => _alumnoEditando == null ? "Nuevo alumno" : "Editar alumno";




        private Estado? _estadoSeleccionado;
        public Estado? EstadoSeleccionado
        {
            get => _estadoSeleccionado;
            set
            {
                _estadoSeleccionado = value;
                OnPropertyChanged(nameof(EstadoSeleccionado));
            }
        }


        private Alumno? _alumnoEditando;
        public Alumno? AlumnoGuardado { get; private set; } = null!;

        public bool EsCreacion => _alumnoEditando == null;

        public FormAlumnoViewModel()
        {
            Cursos = new ObservableCollection<Curso>(_context.Cursos.ToList());
            Estados = new ObservableCollection<Estado>(_context.Estados.ToList());
            GuardarCommand = new RelayCommand(GuardarAlumno);
        }

        public FormAlumnoViewModel(Alumno alumnoExistente)
        {
            Cursos = new ObservableCollection<Curso>(_context.Cursos.ToList());
            GuardarCommand = new RelayCommand(GuardarAlumno);

            _alumnoEditando = alumnoExistente;

            Nombre = alumnoExistente.Nombre;
            CursoSeleccionado = Cursos.FirstOrDefault(c => c.Id == alumnoExistente.IdCurso);
            Notas = alumnoExistente.Notas ?? string.Empty;
            Telefono = alumnoExistente.Telefono ?? string.Empty;
            Estados = new ObservableCollection<Estado>(_context.Estados.ToList());
            EstadoSeleccionado = Estados.FirstOrDefault(e => e.Id == alumnoExistente.IdEstado);


        }


        private void GuardarAlumno(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Nombre) || CursoSeleccionado == null)
            {
                MessageBox.Show("Complete todos los campos antes de guardar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_alumnoEditando == null)
            {
                var nuevoAlumno = new Alumno
                {
                    Nombre = this.Nombre,
                    IdCurso = CursoSeleccionado.Id,
                    IdEstado = 1, 
                    Telefono = this.Telefono,
                    Notas = this.Notas
                };

                _context.Alumnos.Add(nuevoAlumno);
                _context.SaveChanges();

                AlumnoGuardado = _context.Alumnos
                    .Include(a => a.IdCursoNavigation)
                    .Include(a => a.IdEstadoNavigation)
                    .FirstOrDefault(a => a.Id == nuevoAlumno.Id);
            }
            else
            {
                var alumno = _context.Alumnos.Find(_alumnoEditando.Id);
                if (alumno == null)
                {
                    MessageBox.Show("El alumno no existe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                alumno.Nombre = this.Nombre;
                alumno.IdCurso = CursoSeleccionado.Id;
                alumno.IdEstado = EstadoSeleccionado!.Id;  
                alumno.Telefono = this.Telefono;
                alumno.Notas = this.Notas;

                _context.SaveChanges();

                AlumnoGuardado = _context.Alumnos
                    .Include(a => a.IdCursoNavigation)
                    .Include(a => a.IdEstadoNavigation)
                    .FirstOrDefault(a => a.Id == alumno.Id);
            }



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
    }
}


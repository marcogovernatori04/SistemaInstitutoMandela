using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace SistemaInstituto.ViewModels
{
    class FormBeneficioViewModel : BaseViewModel
    {
        private readonly InstitutoDbContext _context = new();
        public ObservableCollection<Alumno> Alumnos { get; set; } = new();
        public Beneficio? BeneficioGuardado { get; private set; } = null!;

        private Alumno? _alumno1Seleccionado;
        public Alumno? Alumno1Seleccionado
        {
            get => _alumno1Seleccionado;
            set
            {
                _alumno1Seleccionado = value;
                OnPropertyChanged(nameof(Alumno1Seleccionado));
                CalcularCuotaMasBarata();
            }
        }

        private Alumno? _alumno2Seleccionado;
        public Alumno? Alumno2Seleccionado
        {
            get => _alumno2Seleccionado;
            set
            {
                _alumno2Seleccionado = value;
                OnPropertyChanged(nameof(Alumno2Seleccionado));
                CalcularCuotaMasBarata();
            }
        }

        private Alumno? _alumno3Seleccionado;
        public Alumno? Alumno3Seleccionado
        {
            get => _alumno3Seleccionado;
            set
            {
                _alumno3Seleccionado = value;
                OnPropertyChanged(nameof(Alumno3Seleccionado));
                CalcularCuotaMasBarata();
            }
        }

        private string _cuotaMasBarata = string.Empty;
        public string CuotaMasBarata
        {
            get => _cuotaMasBarata;
            set
            {
                _cuotaMasBarata = value;
                OnPropertyChanged(nameof(CuotaMasBarata));
            }
        }

        private decimal _valorFinalCuota;
        public decimal ValorFinalCuota
        {
            get => _valorFinalCuota;
            set
            {
                _valorFinalCuota = value;
                OnPropertyChanged(nameof(ValorFinalCuota));
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand ToggleAlumno3Command { get; }

        public string Nombre { get; set; } = null!;
        private int _descuento { get; set; } = 20;
        public int Descuento
        {
            get => _descuento;
            set
            {
                _descuento = value;
                OnPropertyChanged(nameof(Descuento));
                CalcularCuotaMasBarata();
            }
        }

        private bool _mostrarAlumno3;
        public bool MostrarAlumno3
        {
            get => _mostrarAlumno3;
            set
            {
                _mostrarAlumno3 = value;
                OnPropertyChanged(nameof(MostrarAlumno3));
                OnPropertyChanged(nameof(TextoBotonAlumno));
            }
        }
        public string TextoBotonAlumno => MostrarAlumno3 ? "➖ Quitar tercer alumno" : "➕ Agregar tercer alumno";


        public FormBeneficioViewModel()
        {
            var alumnosDb = _context.Alumnos.
                Include(a => a.IdCursoNavigation).AsNoTracking().OrderBy(a => a.Nombre).ToList();

            Alumnos = new ObservableCollection<Alumno>(alumnosDb);

            ToggleAlumno3Command = new RelayCommand(_ => MostrarAlumno3 = !MostrarAlumno3);
            GuardarCommand = new RelayCommand(GuardarBeneficio);
        }

        private void CalcularCuotaMasBarata()
        {
            var alumnosSeleccionados = new List<(Alumno alumno, decimal cuota)>();

            if (Alumno1Seleccionado != null)
            {
                alumnosSeleccionados.Add((Alumno1Seleccionado, ObtenerArancelMasBarato(Alumno1Seleccionado.IdCurso)));
            }
            if (Alumno2Seleccionado != null)
            {
                alumnosSeleccionados.Add((Alumno2Seleccionado, ObtenerArancelMasBarato(Alumno2Seleccionado.IdCurso)));
            }
            if (Alumno3Seleccionado != null)
            {
                alumnosSeleccionados.Add((Alumno3Seleccionado, ObtenerArancelMasBarato(Alumno3Seleccionado.IdCurso)));
            }

            if (alumnosSeleccionados.Any())
            {
                var menor = alumnosSeleccionados.OrderBy(c => c.cuota).First();
                CuotaMasBarata = $"{menor.alumno.Nombre} - {menor.cuota:C}";

                if (menor.alumno.IdCursoNavigation.Beneficio)
                {
                    var factor = 1m - (Math.Clamp(Descuento, 0, 100) / 100m);
                    ValorFinalCuota = Math.Round(menor.cuota * factor, 2);
                }
                else
                {
                    ValorFinalCuota = menor.cuota;
                }

            }
            else
            {
                CuotaMasBarata = "N/A";
                ValorFinalCuota = 0;
            }
        }

        private decimal ObtenerArancelMasBarato(int idCurso)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            decimal? monto = _context.Aranceles
                .Where(ar => ar.IdCurso == idCurso)
                .Where(ar => ar.VigenciaDesde <= hoy &&
                             (ar.VigenciaHasta == null || ar.VigenciaHasta >= hoy))
                .Min(ar => ar.Monto);

            return monto ?? 0m;
        }

        private bool PuedeGuardar()
        {
            var cantAlumnos = new[] { Alumno1Seleccionado, Alumno2Seleccionado, Alumno3Seleccionado }
                .Count(a => a != null);
            return !string.IsNullOrWhiteSpace(Nombre)
                && Descuento >= 0
                && Descuento <= 100
                && cantAlumnos >= 2;
        }

        private void GuardarBeneficio(object? obj)
        {
            if (!PuedeGuardar())
                return;

            var nuevoBeneficio = new Beneficio
            {
                Nombre = Nombre,
                FechaCreacion = DateOnly.FromDateTime(DateTime.Today),
                Activo = true,
                Descuento = Descuento
            };
            _context.Beneficios.Add(nuevoBeneficio);
            _context.SaveChanges();

            var alumnosIds = new List<int>();
            if (Alumno1Seleccionado != null)
                alumnosIds.Add(Alumno1Seleccionado.Id);
            if (Alumno2Seleccionado != null)
                alumnosIds.Add(Alumno2Seleccionado.Id);
            if (MostrarAlumno3 && Alumno3Seleccionado != null)
                alumnosIds.Add(Alumno3Seleccionado.Id);

            foreach (var id in alumnosIds.Distinct())
            {
                _context.AlumnosBeneficios.Add(new AlumnosBeneficio
                {
                    IdAlumno = id,
                    IdBeneficio = nuevoBeneficio.Id
                });
            }
            _context.SaveChanges();

            BeneficioGuardado = nuevoBeneficio;

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

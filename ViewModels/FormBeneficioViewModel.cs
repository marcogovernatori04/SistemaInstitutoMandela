using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Application = System.Windows.Application;

namespace SistemaInstituto.ViewModels
{
    class FormBeneficioViewModel : BaseViewModel
    {
        private const int MIN_ALUMNOS = 2;
        private const int MAX_ALUMNOS = 5;


        private readonly InstitutoDbContext _context = new(); // base
        public ObservableCollection<Alumno> Alumnos { get; set; } = new(); // colección de alumnos para los ComboBox

        public ObservableCollection<AlumnoSeleccionItem> AlumnosSeleccionados { get; set; } = new();

        public ObservableCollection<BeneficiarioInfo> AlumnosConDescuento { get; } = new();

        public Beneficio? BeneficioGuardado { get; private set; } = null!; // propiedad para almacenar el beneficio creado y mostrarlo en la ventana de detalles

        private string _cuotaMasBarata = string.Empty; // propiedad para mostrar el alumno con la cuota más barata y su monto formateado, se actualiza cada vez que se selecciona un alumno o cambia el descuento
        public string CuotaMasBarata
        {
            get => _cuotaMasBarata;
            set
            {
                _cuotaMasBarata = value;
                OnPropertyChanged(nameof(CuotaMasBarata));
            }
        }

        private decimal _valorFinalCuota; // propiedad para mostrar el valor final de la cuota más barata después de aplicar el descuento, se actualiza cada vez que se selecciona un alumno o cambia el descuento
        public decimal ValorFinalCuota
        {
            get => _valorFinalCuota;
            set
            {
                _valorFinalCuota = value;
                OnPropertyChanged(nameof(ValorFinalCuota));
            }
        }

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged(nameof(Nombre));
                ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
            }
        }
        private int _descuento { get; set; } = 20;
        public int Descuento
        {
            get => _descuento;
            set
            {
                _descuento = value;
                OnPropertyChanged(nameof(Descuento));
                CalcularCuotaMasBarata();
                ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
            }
        }

        public string Titulo => _beneficioEditandoId.HasValue ? "Editar un beneficio" : "Agregar un beneficio";

        public bool PuedeAgregarAlumno => AlumnosSeleccionados.Count < MAX_ALUMNOS;

        private readonly int? _beneficioEditandoId;

        public ICommand GuardarCommand { get; }
        public ICommand AgregarAlumnoCommand { get; }
        public ICommand QuitarAlumnoCommand { get; }


        public FormBeneficioViewModel()
        {
            var alumnosDb = _context.Alumnos.
                Include(a => a.IdCursoNavigation).AsNoTracking().OrderBy(a => a.Nombre).ToList(); // traigo los alumnos con su curso para mostrar el nombre del curso en el ComboBox y calcular la cuota más barata, uso AsNoTracking porque no voy a modificar los alumnos

            Alumnos = new ObservableCollection<Alumno>(alumnosDb);

            for (int i = 0; i < MIN_ALUMNOS; i++)
                AgregarSlot();

            AgregarAlumnoCommand = new RelayCommand(_ => AgregarSlot(), _ => PuedeAgregarAlumno);
            QuitarAlumnoCommand = new RelayCommand(
                param => QuitarSlot(param as AlumnoSeleccionItem),
                param => PuedeQuitarSlot(param as AlumnoSeleccionItem)
                );

            GuardarCommand = new RelayCommand(GuardarBeneficio, _ => PuedeGuardar());
        }

        public FormBeneficioViewModel(Beneficio beneficioExistente) : this()
        {
            _beneficioEditandoId = beneficioExistente.Id;
            Nombre = beneficioExistente.Nombre;
            Descuento = beneficioExistente.Descuento;

            var alumnosBeneficio = _context.AlumnosBeneficios
                .Where(ab => ab.IdBeneficio == beneficioExistente.Id)
                .Include(ab => ab.IdAlumnoNavigation)
                    .ThenInclude(a => a.IdCursoNavigation)
                .AsNoTracking()
                .ToList();

            AlumnosSeleccionados.Clear();

            foreach (var ab in alumnosBeneficio)
            {
                var item = new AlumnoSeleccionItem
                {
                    AlumnoSeleccionado = ab.IdAlumnoNavigation,
                    EsEliminable = alumnosBeneficio.Count > MIN_ALUMNOS,
                    AplicaDescuento = ab.AplicaDescuento,
                    Numero = AlumnosSeleccionados.Count + 1,
                    AplicaDescuentoManual = true
                };
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AlumnoSeleccionItem.AlumnoSeleccionado) || e.PropertyName == nameof(AlumnoSeleccionItem.AplicaDescuento))
                    {
                        CalcularCuotaMasBarata();
                        ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
                    }
                };
                AlumnosSeleccionados.Add(item);
            }

            CalcularCuotaMasBarata();
            OnPropertyChanged(nameof(PuedeAgregarAlumno));

        }

        private void AgregarSlot()
        {
            if (AlumnosSeleccionados.Count >= MAX_ALUMNOS)
                return;

            var item = new AlumnoSeleccionItem { EsEliminable = AlumnosSeleccionados.Count >= MIN_ALUMNOS, Numero = AlumnosSeleccionados.Count + 1 };

            item.PropertyChanged += (s, e) => // me suscribo al evento PropertyChanged de cada item de selección de alumno para recalcular la cuota más barata cada vez que se selecciona un alumno diferente en el ComboBox, verificando que la propiedad que cambió sea AlumnoSeleccionado antes de llamar a CalcularCuotaMasBarata, ya que solo me interesa recalcular la cuota cuando cambia el alumno seleccionado y no por otros cambios en la interfaz
            {
                if (e.PropertyName == nameof(AlumnoSeleccionItem.AlumnoSeleccionado) || e.PropertyName == nameof(AlumnoSeleccionItem.AplicaDescuento))
                {
                    CalcularCuotaMasBarata();
                    ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
                }
            };

            AlumnosSeleccionados.Add(item);
            OnPropertyChanged(nameof(PuedeAgregarAlumno));

        }

        private bool PuedeQuitarSlot(AlumnoSeleccionItem? item)
        {
            return item != null && item.EsEliminable;
        }

        private void QuitarSlot(AlumnoSeleccionItem? item)
        {
            if (item == null || !item.EsEliminable)
                return;
            AlumnosSeleccionados.Remove(item);
            CalcularCuotaMasBarata();
            OnPropertyChanged(nameof(PuedeAgregarAlumno));
            ((RelayCommand)GuardarCommand).RaiseCanExecuteChanged();
        }


        private bool _calculando = false;

        private void CalcularCuotaMasBarata()
        {
            if (_calculando) return;
            _calculando = true;

            try
            {
                var alumnosConCuota = AlumnosSeleccionados
                    .Where(i => i.AlumnoSeleccionado != null)
                    .Select(i => (item: i, alumno: i.AlumnoSeleccionado!, cuota: ObtenerArancelMasBarato(i.AlumnoSeleccionado!.IdCurso)))
                    .ToList();

                if (alumnosConCuota.Any())
                {
                    var menor = alumnosConCuota.OrderBy(c => c.cuota).First();
                    var factor = 1m - (Math.Clamp(Descuento, 0, 100) / 100m); // variable local, no propiedad

                    var montoMinimo = alumnosConCuota.Min(c => c.cuota);
                    foreach (var entry in alumnosConCuota)
                    {
                        if (!entry.item.AplicaDescuentoManual)
                            entry.item.SetAplicaDescuentoAutomatico(entry.cuota == montoMinimo && entry.alumno.IdCursoNavigation.Beneficio); // si el alumno no tiene el descuento aplicado manualmente, se lo aplico automáticamente solo si su cuota es igual a la cuota más barata y su curso admite beneficio, de lo contrario se lo quito automáticamente
                    }
                }

                AlumnosConDescuento.Clear();
                foreach (var entry in alumnosConCuota.Where(e => e.item.AplicaDescuento && e.alumno.IdCursoNavigation.Beneficio))
                {
                    var factor = 1m - (Math.Clamp(Descuento, 0, 100) / 100m);
                    var valorFinal = Math.Round(entry.cuota * factor, 2);
                    AlumnosConDescuento.Add(new BeneficiarioInfo
                    {
                        NombreYCuota = $"{entry.alumno.Nombre} - {entry.cuota:C}",
                        ValorFinal = valorFinal
                    });
                }
            }
            finally
            {
                _calculando = false;
            }

        }

        private decimal ObtenerArancelMasBarato(int idCurso)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            decimal? monto = _context.Aranceles // obtengo el monto del arancel más barato para el curso dado, filtrando por vigencia y ordenando por monto, luego tomo el primer resultado o null si no hay resultados
                .Where(ar => ar.IdCurso == idCurso)
                .Where(ar => ar.VigenciaDesde <= hoy &&
                             (ar.VigenciaHasta == null || ar.VigenciaHasta >= hoy))
                .Min(ar => ar.Monto);

            return monto ?? 0m;
        }

        private bool PuedeGuardar()
        {
            var cantAlumnos = AlumnosSeleccionados.Count(i => i.AlumnoSeleccionado != null);
            return !string.IsNullOrWhiteSpace(Nombre)
                && Descuento >= 0
                && Descuento <= 100
                && cantAlumnos >= MIN_ALUMNOS;
        }

        private void GuardarBeneficio(object? obj)
        {
            if (!PuedeGuardar()) return;

            Beneficio beneficio;

            if (_beneficioEditandoId.HasValue)
            {
                // Modo edición
                beneficio = _context.Beneficios.Find(_beneficioEditandoId.Value)!;
                beneficio.Nombre = TextNormalizer.ToTitleCase(Nombre);
                beneficio.Descuento = Descuento;

                // Eliminar relaciones anteriores y recrearlas
                var anteriores = _context.AlumnosBeneficios
                    .Where(ab => ab.IdBeneficio == _beneficioEditandoId.Value)
                    .ToList();
                _context.AlumnosBeneficios.RemoveRange(anteriores);
            }
            else
            {
                // Modo creación
                beneficio = new Beneficio
                {
                    Nombre = TextNormalizer.ToTitleCase(Nombre),
                    FechaCreacion = DateOnly.FromDateTime(DateTime.Today),
                    Activo = true,
                    Descuento = Descuento
                };
                _context.Beneficios.Add(beneficio);
            }

            _context.SaveChanges();

            var itemsValidos = AlumnosSeleccionados.Where(i => i.AlumnoSeleccionado != null).ToList();
            foreach (var item in itemsValidos)
            {
                _context.AlumnosBeneficios.Add(new AlumnosBeneficio
                {
                    IdAlumno = item.AlumnoSeleccionado!.Id,
                    IdBeneficio = beneficio.Id,
                    AplicaDescuento = item.AplicaDescuento
                });
            }

            var confirm = MessageBox.Show("¿Guardar cambios del beneficio?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _context.SaveChanges();
            BeneficioGuardado = beneficio;

            var ventana = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (ventana != null) { ventana.DialogResult = true; ventana.Close(); }
            else MessageBox.Show("No se pudo cerrar la ventana correctamente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class AlumnoSeleccionItem : BaseViewModel
    {
        private Alumno? _alumnoSeleccionado;
        public Alumno? AlumnoSeleccionado
        {
            get => _alumnoSeleccionado;
            set
            {
                _alumnoSeleccionado = value;
                OnPropertyChanged(nameof(AlumnoSeleccionado));
                OnPropertyChanged(nameof(NombreCurso));
                OnPropertyChanged(nameof(TextoComboBox));
                OnPropertyChanged(nameof(TieneAlumno));
                OnPropertyChanged(nameof(CursoAdmiteDescuento));
            }
        }
            
        public string TextoComboBox
        {
            get => AlumnoSeleccionado?.Nombre ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    AlumnoSeleccionado = null;
            }
        }

        private bool _aplicaDescuento = true;
        public bool AplicaDescuento
        {
            get => _aplicaDescuento;
            set
            {
                _aplicaDescuento = value;
                AplicaDescuentoManual = true;
                OnPropertyChanged(nameof(AplicaDescuento));
            }
        }

        public void SetAplicaDescuentoAutomatico(bool value) // método para establecer el valor de AplicaDescuento sin marcarlo como aplicado manualmente, se usa para aplicar o quitar el descuento automáticamente al alumno con la cuota más barata, sin afectar a los alumnos que ya tienen el descuento aplicado manualmente
        {
            if (_aplicaDescuento == value) return;
            _aplicaDescuento = value;
            OnPropertyChanged(nameof(AplicaDescuento));

        }

        public bool AplicaDescuentoManual { get; set; } = false;
        public bool CursoAdmiteDescuento => AlumnoSeleccionado?.IdCursoNavigation?.Beneficio ?? false;

        public bool TieneAlumno => AlumnoSeleccionado != null;  
        public string NombreCurso => AlumnoSeleccionado?.IdCursoNavigation.Nombre ?? string.Empty; // propiedad calculada para mostrar el nombre del curso del alumno seleccionado en el ComboBox, o un texto por defecto si no hay un alumno seleccionado

        public bool EsEliminable { get; set; }
        public int Numero { get; set; } 
    }

    public class BeneficiarioInfo
    {
        public string NombreYCuota { get; set; } = string.Empty;
        public decimal ValorFinal { get; set; }
    }

}

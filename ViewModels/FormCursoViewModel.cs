using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    class FormCursoViewModel : BaseViewModel
    {
        private readonly InstitutoDbContext _context = new();

        public ICommand GuardarCommand { get; }

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
        public bool AdmiteBeneficio { get; set; }

        public string Titulo => _cursoEditando == null ? "Nuevo curso" : "Editar curso";

        private Curso? _cursoEditando;
        public Curso? CursoGuardado { get; private set; } = null!;

        public FormCursoViewModel()
        {
            GuardarCommand = new RelayCommand(GuardarCurso, _ => !string.IsNullOrWhiteSpace(Nombre));
        }

        public FormCursoViewModel(Curso cursoExistente)
        {
            GuardarCommand = new RelayCommand(GuardarCurso, _ => !string.IsNullOrWhiteSpace(Nombre));
            _cursoEditando = cursoExistente;
            Nombre = cursoExistente.Nombre ?? string.Empty;
            AdmiteBeneficio = cursoExistente.Beneficio;
        }

        private void GuardarCurso(object _)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                // Aquí podrías mostrar un mensaje de error al usuario indicando que el nombre es obligatorio
                return;
            }
            if (_cursoEditando == null)
            {
                var maxOrden = _context.Cursos.Max(c => (int?)c.Orden) ?? 0;

                var nuevoCurso = new Curso
                {
                    Nombre = TextNormalizer.ToTitleCase(Nombre),
                    Beneficio = AdmiteBeneficio,
                    Orden = maxOrden + 1

                };
                _context.Cursos.Add(nuevoCurso);
                _context.SaveChanges();
                CursoGuardado = nuevoCurso;
            }
            else
            {
                var cursoDb = _context.Cursos.Find(_cursoEditando.Id)!;
                cursoDb.Nombre = TextNormalizer.ToTitleCase(Nombre);
                cursoDb.Beneficio = AdmiteBeneficio;
                _context.SaveChanges();
                CursoGuardado = cursoDb;
            }

            var ventana = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
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

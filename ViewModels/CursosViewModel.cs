using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaInstituto.Commands;
using SistemaInstituto.Data;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SistemaInstituto.ViewModels
{
    public class CursosViewModel : BaseViewModel
    {
        public RelayCommand AgregarCursoCommand { get; }
        public RelayCommand EditarCursoCommand { get; }
        public RelayCommand SubirCursoCommand { get; }
        public RelayCommand BajarCursoCommand { get; }

        public ObservableCollection<Curso> Cursos { get; set; } = null!;

        private InstitutoDbContext _context = new InstitutoDbContext();

        private ICollectionView _cursosView { get; set; } = null!;
        public ICollectionView CursosView
        {
            get => _cursosView;
            set
            {
                _cursosView = value;
                OnPropertyChanged();
            }
        }

        private Curso? _cursoSeleccionado = null!;
        public Curso? CursoSeleccionado
        {
            get => _cursoSeleccionado;
            set
            {
                _cursoSeleccionado = value;
                OnPropertyChanged();
                SubirCursoCommand.RaiseCanExecuteChanged();
                BajarCursoCommand.RaiseCanExecuteChanged();
            }
        }

        public CursosViewModel()
        {
            AgregarCursoCommand = new RelayCommand(o => AgregarCurso());
            EditarCursoCommand = new RelayCommand(o => EditarCurso(o as Curso), o => o is Curso);
            SubirCursoCommand = new RelayCommand(o => MoverCurso(-1), o => PuedeMover(-1));
            BajarCursoCommand = new RelayCommand(o => MoverCurso(1), o => PuedeMover(1));


            CargarDatos();
        }

        private void AgregarCurso()
        {
            var viewModel = new FormCursoViewModel();
            var ventana = new FormCursoView()
            {
                DataContext = viewModel

            };
            if (ventana.ShowDialog() == true)
            {
                if (viewModel.CursoGuardado != null)
                {
                    Cursos.Add(viewModel.CursoGuardado);
                    CursosView.Refresh();
                    CursoSeleccionado = viewModel.CursoGuardado;
                }
            }
        }

        private void EditarCurso(Curso? curso)
        {
            if (curso == null) return;
            var viewModel = new FormCursoViewModel(curso);
            var ventana = new FormCursoView()
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                CursosView.Refresh();
            }
        }

        private void CargarDatos()
        {
            var cursosDb = _context.Cursos.OrderBy(c => c.Orden).ToList();
            Cursos = new ObservableCollection<Curso>(cursosDb);

            CursosView = CollectionViewSource.GetDefaultView(Cursos);
        }

        private bool PuedeMover(int direccion)
        {
            if (CursoSeleccionado == null) return false;
            var lista = Cursos.OrderBy(c => c.Orden).ToList();
            var index = lista.IndexOf(CursoSeleccionado);
            if (index == -1) return false;
            return direccion == -1 ? index > 0 : index < lista.Count - 1;
        }

        private void MoverCurso(int direccion)
        {
            if (CursoSeleccionado == null) return;

            var lista = Cursos.OrderBy(c => c.Orden).ToList();
            var index = lista.IndexOf(CursoSeleccionado);
            if (index == -1) return;
            var vecino = lista[index + direccion];

            (CursoSeleccionado.Orden, vecino.Orden) = (vecino.Orden, CursoSeleccionado.Orden);

            _context.Cursos.Update(CursoSeleccionado);
            _context.Cursos.Update(vecino);
            _context.SaveChanges();

            var cursoSeleccionadoId = CursoSeleccionado.Id;
            var cursosDb = _context.Cursos.OrderBy(c => c.Orden).ToList();
            Cursos.Clear();
            foreach (var c in cursosDb)
                Cursos.Add(c);

            CursoSeleccionado = Cursos.FirstOrDefault(c => c.Id == cursoSeleccionadoId);


            SubirCursoCommand.RaiseCanExecuteChanged();
            BajarCursoCommand.RaiseCanExecuteChanged();
        }
    }
}

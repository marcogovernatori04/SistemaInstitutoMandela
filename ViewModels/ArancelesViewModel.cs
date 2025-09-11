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
    public class ArancelesViewModel : BaseViewModel
    {
        public RelayCommand AgregarArancelCommand { get; }
        public RelayCommand ToggleInactivosCommand { get; }

        public ObservableCollection<Arancel> Aranceles { get; set; } = null!;
        public ObservableCollection<Curso> Cursos { get; set; } = null!;

        private InstitutoDbContext _context = new InstitutoDbContext();

        private ICollectionView _arancelesView { get; set; } = null!;
        public ICollectionView ArancelesView
        {
            get => _arancelesView;
            set
            {
                _arancelesView = value;
                OnPropertyChanged();
            }
        }

        private Curso _cursoFiltrado = null!;
        public Curso CursoFiltrado
        {
            get => _cursoFiltrado;
            set
            {
                _cursoFiltrado = value;
                OnPropertyChanged();
                ArancelesView.Refresh();
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
                ArancelesView.Refresh();
            }
        }

        public ArancelesViewModel()
        {
            CargarDatos();
            AgregarArancelCommand = new RelayCommand(o => AgregarArancel());
            ToggleInactivosCommand = new RelayCommand(o => MostrarInactivos = !MostrarInactivos);

        }

        private void AgregarArancel()
        {
            var viewModel = new FormArancelesViewModel();
            var ventana = new FormArancelesView()
            {
                DataContext = viewModel,
                Owner = App.Current.MainWindow
            };
            if (ventana.ShowDialog() == true)
            {
                if (viewModel.ArancelesGuardados != null)
                {
                    Aranceles.Clear();

                    foreach (var arancel in viewModel.ArancelesGuardados)
                    {
                        Aranceles.Add(arancel);
                    }

                    ArancelesView.Refresh();
                    
                }
            }
        }

        private void CargarDatos()
        {
            var arancelesDb = _context.Aranceles
                .Include(a => a.IdCursoNavigation)
                .Where(a => a.Monto != null)
                .OrderByDescending(a => a.Activo)
                    .ThenBy(a => a.IdCurso)
                    .ThenBy(a => a.Monto)
                .AsNoTracking()
                .ToList();

            Aranceles = new ObservableCollection<Arancel>(arancelesDb);

            var listaCursos = arancelesDb.Select(ar => ar.IdCursoNavigation).DistinctBy(c => c.Id).ToList();
            Cursos = new ObservableCollection<Curso>(listaCursos);

            Cursos.Insert(0, new Curso { Id = 0, Nombre = "Todos los cursos" });

            ArancelesView = CollectionViewSource.GetDefaultView(Aranceles);
            ArancelesView.Filter = FiltrarAranceles;

            ArancelesView.SortDescriptions.Clear();

        }

        private bool FiltrarAranceles(object obj)
        {
            if (obj is Arancel arancel)
            {
                bool cursoCoincide = CursoFiltrado == null || CursoFiltrado.Id == 0 || arancel.IdCurso == CursoFiltrado.Id;
                bool estadoCoincide = MostrarInactivos || arancel.Activo == true;

                return cursoCoincide && estadoCoincide;
            }
            return false;
        }
    }
}

using SistemaInstituto.Commands;
using SistemaInstituto.Models;
using SistemaInstituto.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SistemaInstituto.ViewModels
{
    public class SeleccionPagoViewModel : BaseViewModel
    {
        public ICommand PagoNormalCommand { get; }
        public ICommand PagoBeneficioCommand { get; }

        public Action<bool?> CloseAction { get; set; }


        public Pago PagoIndividual { get; private set; }
        public List<Pago> PagosGrupales { get; private set; }


        public SeleccionPagoViewModel()
        {
            PagoNormalCommand = new RelayCommand(o => AbrirPagoNormal());
            PagoBeneficioCommand = new RelayCommand(o => AbrirPagoBeneficio());
        }

        private void AbrirPagoNormal()
        {
            var vm = new FormPagoViewModel();
            var win = new FormPagoView(vm)
            {
                DataContext = vm,
            };
            if (win.ShowDialog() == true)
            {
                PagoIndividual = vm.PagoGuardado;
                CloseAction?.Invoke(true);
            }
            else
            {
                CloseAction?.Invoke(false);
            }
        }

        private void AbrirPagoBeneficio()
        {
            var vm = new FormPagoGrupoViewModel();
            var win = new FormPagoGrupoView(vm)
            {
                DataContext = vm,
            };
            if (win.ShowDialog() == true)
            {
                PagosGrupales = vm.PagosGuardados;
                CloseAction?.Invoke(true);
            }
            else
            {
                CloseAction?.Invoke(false);
            }


        }
    }
}

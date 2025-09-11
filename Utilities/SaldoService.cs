using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInstituto.Utilities
{
    public class SaldoService
    {
        private readonly InstitutoDbContext _context;

        public SaldoService(InstitutoDbContext context)
        {
            _context = context;
        }

        public decimal CalcularSaldoAlumno(int idAlumno)
        {
            var pagos = _context.Pagos
                .Where(p => p.IdAlumno == idAlumno)
                .ToList();

            var idCurso = _context.Alumnos
                .Where(a => a.Id == idAlumno)
                .Select(a => a.IdCurso)
                .FirstOrDefault();

            var aranceles = _context.Aranceles
                .Where(a => a.IdCurso == idCurso)
                .ToList();

            decimal saldo = 0m;

            foreach (var arancel in aranceles)
            {
                var montoDebido = arancel.Monto ?? 0m;

                var pagosArancel = pagos.Where(p => p.IdArancel == arancel.Id).Sum(p => p.MontoPagado);

                saldo += pagosArancel - montoDebido;
            }

            return saldo;
        }
    }
}

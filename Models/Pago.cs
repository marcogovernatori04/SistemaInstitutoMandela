using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class Pago
{
    public int Id { get; set; }

    public int IdAlumno { get; set; }

    public int IdArancel { get; set; }

    public DateOnly FechaPago { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal MontoPagado { get; set; }

    public int IdMetodoPago { get; set; }

    public string? Notas { get; set; }

    public virtual Alumno IdAlumnoNavigation { get; set; } = null!;

    public virtual Arancel IdArancelNavigation { get; set; } = null!;

    public virtual MetodoPago IdMetodoPagoNavigation { get; set; } = null!;
    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

}

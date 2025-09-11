using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class Movimiento
{
    public int Id { get; set; }

    public int IdAlumno { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Importe { get; set; }

    public string Concepto { get; set; } = null!;

    public int? IdPago { get; set; }

    public virtual Alumno IdAlumnoNavigation { get; set; } = null!;

    public virtual Pago? IdPagoNavigation { get; set; }
}

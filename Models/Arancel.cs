using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class Arancel
{
    public int Id { get; set; }

    public int? IdCurso { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal? Monto { get; set; }

    public DateOnly VigenciaDesde { get; set; }

    public DateOnly? VigenciaHasta { get; set; }

    public bool Activo { get; set; }

    public virtual Curso? IdCursoNavigation { get; set; } = null!;

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

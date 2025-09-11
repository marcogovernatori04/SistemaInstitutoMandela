using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class Beneficio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateOnly FechaCreacion { get; set; }

    public bool Activo { get; set; }

    public int Descuento { get; set; }

    public virtual ICollection<AlumnosBeneficio> AlumnosBeneficios { get; set; } = new List<AlumnosBeneficio>();
}

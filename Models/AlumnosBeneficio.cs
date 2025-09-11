using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class AlumnosBeneficio
{
    public int IdAlumno { get; set; }

    public int IdBeneficio { get; set; }

    public virtual Alumno IdAlumnoNavigation { get; set; } = null!;

    public virtual Beneficio IdBeneficioNavigation { get; set; } = null!;
}

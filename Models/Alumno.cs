using SistemaInstituto.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaInstituto.Models;

public partial class Alumno
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int IdEstado { get; set; }
    public int IdCurso { get; set; }
    public string? Notas { get; set; }
    public string? Telefono { get; set; }


    public virtual AlumnosBeneficio? AlumnosBeneficio { get; set; }
    public virtual Curso IdCursoNavigation { get; set; } = null!;
    public virtual Estado IdEstadoNavigation { get; set; } = null!;
    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

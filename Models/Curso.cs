using System;
using System.Collections.Generic;

namespace SistemaInstituto.Models;

public partial class Curso
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Beneficio { get; set; }

    public virtual ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();

    public virtual ICollection<Arancel> Aranceles { get; set; } = new List<Arancel>();
}

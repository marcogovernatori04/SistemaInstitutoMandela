using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInstituto.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAplicaDescuento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AplicaDescuento",
                table: "AlumnosBeneficios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Para cada beneficio, marca true solo al alumno con la cuota más baja vigente hoy
            migrationBuilder.Sql(@"
        UPDATE AlumnosBeneficios
        SET AplicaDescuento = 1
        WHERE IdAlumno IN (
            SELECT ab.IdAlumno
            FROM AlumnosBeneficios ab
            JOIN Alumnos a ON ab.IdAlumno = a.Id
            JOIN (
                SELECT ab2.IdBeneficio, MIN(ar.Monto) AS MontoMinimo
                FROM AlumnosBeneficios ab2
                JOIN Alumnos a2 ON ab2.IdAlumno = a2.Id
                JOIN Aranceles ar ON ar.IdCurso = a2.IdCurso
                JOIN Cursos c ON c.Id = a2.IdCurso
                WHERE ar.VigenciaHasta IS NULL OR ar.VigenciaHasta >= DATE('now')
                AND c.Beneficio = 1
                GROUP BY ab2.IdBeneficio
            ) minimos ON ab.IdBeneficio = minimos.IdBeneficio
            JOIN Aranceles ar ON ar.IdCurso = a.IdCurso
            WHERE (ar.VigenciaHasta IS NULL OR ar.VigenciaHasta >= DATE('now'))
            AND ar.Monto = minimos.MontoMinimo
        )
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AplicaDescuento",
                table: "AlumnosBeneficios");
        }
    }
}

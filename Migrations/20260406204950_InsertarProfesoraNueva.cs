using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInstituto.Migrations
{
    /// <inheritdoc />
    public partial class InsertarProfesoraNueva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Profesores (Nombre) VALUES ('Florencia Speranza');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Profesores WHERE Nombre = 'Florencia Speranza';");
        }
    }
}

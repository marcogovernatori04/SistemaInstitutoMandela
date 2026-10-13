using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInstituto.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarCursoAdults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Cursos SET Nombre = '2nd Adults B' WHERE Nombre = '2nd Adults Online'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Cursos SET Nombre = '2nd Adults B' WHERE Nombre = '2nd Adults Online'");
        }
    }
}

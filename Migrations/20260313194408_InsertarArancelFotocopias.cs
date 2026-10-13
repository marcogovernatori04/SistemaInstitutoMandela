using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaInstituto.Migrations
{
    /// <inheritdoc />
    public partial class InsertarArancelFotocopias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Aranceles",
                columns: new[] { "IdCurso", "Concepto", "Monto", "VigenciaDesde", "VigenciaHasta", "Activo" },
                values: new object[] { null, "Fotocopias", null, "2026-01-01", null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Aranceles",
                keyColumn: "Concepto",
                keyValue: "Fotocopias");
        }
    }
}

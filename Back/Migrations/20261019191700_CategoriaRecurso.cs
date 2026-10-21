using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reservas.Migrations
{
    /// <inheritdoc />
    public partial class CategoriaRecurso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipoDeporte",
                table: "RecursosReservables",
                newName: "Categoria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Categoria",
                table: "RecursosReservables",
                newName: "TipoDeporte");
        }
    }
}

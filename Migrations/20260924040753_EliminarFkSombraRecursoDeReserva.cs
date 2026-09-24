using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reservas.Migrations
{
    /// <inheritdoc />
    public partial class EliminarFkSombraRecursoDeReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_RecursosReservables_RecursoReservableId",
                table: "Reservas");

            migrationBuilder.DropIndex(
                name: "IX_Reservas_RecursoReservableId",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "RecursoReservableId",
                table: "Reservas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecursoReservableId",
                table: "Reservas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_RecursoReservableId",
                table: "Reservas",
                column: "RecursoReservableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_RecursosReservables_RecursoReservableId",
                table: "Reservas",
                column: "RecursoReservableId",
                principalTable: "RecursosReservables",
                principalColumn: "Id");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Reservas.Migrations
{
    /// <inheritdoc />
    public partial class ReservasPublicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificacionesCorreo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodigoHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CodigoExpiraEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoUsadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TokenExpiraEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenUsadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacionesCorreo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesCorreo_Email",
                table: "VerificacionesCorreo",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesCorreo_Email_CreadoEn",
                table: "VerificacionesCorreo",
                columns: new[] { "Email", "CreadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesCorreo_TokenHash",
                table: "VerificacionesCorreo",
                column: "TokenHash",
                unique: true,
                filter: "\"TokenHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificacionesCorreo");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpedienteSeguroYDeriveSegurosTeso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "seguros_teso",
                table: "DevengadosExtra");

            migrationBuilder.CreateTable(
                name: "ExpedientesSeguro",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    expediente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    expediente_financiera = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    op = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    beneficiario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    importe_neto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    seguro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesSeguro", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesSeguro_expediente_financiera",
                table: "ExpedientesSeguro",
                column: "expediente_financiera");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpedientesSeguro");

            migrationBuilder.AddColumn<string>(
                name: "seguros_teso",
                table: "DevengadosExtra",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}

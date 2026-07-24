using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpedienteUnicoYLimpiezaColumnasMuertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: donde exista la clave financiera ya normalizada, pasa a ser
            // LA columna de expediente (columna única). El resto conserva su valor
            // (se normalizará al próximo guardado, que ahora valida el formato).
            migrationBuilder.Sql("""
                UPDATE ExpedientesCaf SET expediente = expediente_financiera
                WHERE expediente_financiera IS NOT NULL AND expediente_financiera <> '';

                UPDATE ExpedientesSeguro SET expediente = expediente_financiera
                WHERE expediente_financiera IS NOT NULL AND expediente_financiera <> '';
                """);

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesSeguro_expediente_financiera",
                table: "ExpedientesSeguro");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesCaf_expediente_financiera",
                table: "ExpedientesCaf");

            migrationBuilder.DropColumn(
                name: "expediente_financiera",
                table: "ExpedientesSeguro");

            migrationBuilder.DropColumn(
                name: "expediente_financiera",
                table: "ExpedientesCaf");

            migrationBuilder.DropColumn(
                name: "buzon_sade",
                table: "DevengadosExtra");

            migrationBuilder.DropColumn(
                name: "fecha_sade",
                table: "DevengadosExtra");

            migrationBuilder.AlterColumn<string>(
                name: "expediente",
                table: "ExpedientesSeguro",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "expediente",
                table: "ExpedientesCaf",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesSeguro_expediente",
                table: "ExpedientesSeguro",
                column: "expediente");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesCaf_expediente",
                table: "ExpedientesCaf",
                column: "expediente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpedientesSeguro_expediente",
                table: "ExpedientesSeguro");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesCaf_expediente",
                table: "ExpedientesCaf");

            migrationBuilder.AlterColumn<string>(
                name: "expediente",
                table: "ExpedientesSeguro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "expediente_financiera",
                table: "ExpedientesSeguro",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "expediente",
                table: "ExpedientesCaf",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "expediente_financiera",
                table: "ExpedientesCaf",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buzon_sade",
                table: "DevengadosExtra",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_sade",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesSeguro_expediente_financiera",
                table: "ExpedientesSeguro",
                column: "expediente_financiera");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesCaf_expediente_financiera",
                table: "ExpedientesCaf",
                column: "expediente_financiera");
        }
    }
}

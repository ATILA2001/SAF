using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaRechazoYQuitarFaltaPoliza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "falta_poliza",
                table: "StatusContabilidadExtras");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_rechazo",
                table: "StatusContabilidadExtras",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_rechazo",
                table: "StatusContabilidadExtras");

            migrationBuilder.AddColumn<bool>(
                name: "falta_poliza",
                table: "StatusContabilidadExtras",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

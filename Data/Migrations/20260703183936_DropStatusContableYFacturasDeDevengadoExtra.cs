using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropStatusContableYFacturasDeDevengadoExtra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_factura_correcta",
                table: "DevengadosExtra");

            migrationBuilder.DropColumn(
                name: "pedido_factura2",
                table: "DevengadosExtra");

            migrationBuilder.DropColumn(
                name: "pedido_factura3",
                table: "DevengadosExtra");

            migrationBuilder.DropColumn(
                name: "status_contable",
                table: "DevengadosExtra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_factura_correcta",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pedido_factura2",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pedido_factura3",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_contable",
                table: "DevengadosExtra",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}

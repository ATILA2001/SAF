using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropFechaPagoTotal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_pago_total",
                table: "DevengadosExtra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_pago_total",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);
        }
    }
}

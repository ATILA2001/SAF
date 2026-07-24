using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropFechaDePagoNoCaf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_de_pago_no_caf",
                table: "DevengadosExtra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_de_pago_no_caf",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);
        }
    }
}

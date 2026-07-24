using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpedienteCafYDeriveFechaPagoCaf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_de_pago_caf",
                table: "DevengadosExtra");

            migrationBuilder.CreateTable(
                name: "ExpedientesCaf",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    anio = table.Column<int>(type: "int", nullable: false),
                    expediente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    expediente_financiera = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    op = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    beneficiario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    importe_neto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    iibb = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    cuenta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fecha_pago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cargado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cc_pagadora = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    pase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    revisado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesCaf", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesCaf_anio",
                table: "ExpedientesCaf",
                column: "anio");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesCaf_expediente_financiera",
                table: "ExpedientesCaf",
                column: "expediente_financiera");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpedientesCaf");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_de_pago_caf",
                table: "DevengadosExtra",
                type: "datetime2",
                nullable: true);
        }
    }
}

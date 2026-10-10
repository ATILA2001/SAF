using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCorreccionesDeOp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SigafOpCorrecciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nro_doc_resp = table.Column<long>(type: "bigint", nullable: false),
                    op = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    expediente_original = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    expediente_corregido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    beneficiario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    importe_op = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    fecha_pago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigafOpCorrecciones", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SigafOpCorrecciones_expediente_corregido",
                table: "SigafOpCorrecciones",
                column: "expediente_corregido");

            migrationBuilder.CreateIndex(
                name: "IX_SigafOpCorrecciones_nro_doc_resp_op",
                table: "SigafOpCorrecciones",
                columns: new[] { "nro_doc_resp", "op" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SigafOpCorrecciones");
        }
    }
}

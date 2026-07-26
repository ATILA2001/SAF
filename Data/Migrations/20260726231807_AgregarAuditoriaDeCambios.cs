using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAuditoriaDeCambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CambiosAuditoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    lote = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    usuario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    vista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entidad_id = table.Column<int>(type: "int", nullable: true),
                    clave_negocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    accion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    campo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    valor_anterior = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    valor_nuevo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosAuditoria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosAuditoria_vista_clave_negocio_fecha",
                table: "CambiosAuditoria",
                columns: new[] { "vista", "clave_negocio", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosAuditoria_vista_entidad_id_fecha",
                table: "CambiosAuditoria",
                columns: new[] { "vista", "entidad_id", "fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosAuditoria");
        }
    }
}

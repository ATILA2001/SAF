using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devengados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_dev = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nro_dev = table.Column<int>(type: "int", nullable: false),
                    fecha_imputacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    expediente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    empresa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    importe_pp = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    fecha_importacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devengados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StatusContableOpciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusContableOpciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StatusDgayfOpciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusDgayfOpciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StatusOpOpciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusOpOpciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TramitadoresCuentasPagar",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TramitadoresCuentasPagar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TramitadoresLiquidaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TramitadoresLiquidaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DevengadosExtra",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_dev = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nro_dev = table.Column<int>(type: "int", nullable: false),
                    status_dgayf_opcion_id = table.Column<int>(type: "int", nullable: true),
                    status_op_opcion_id = table.Column<int>(type: "int", nullable: true),
                    fecha_firma_op = table.Column<DateTime>(type: "datetime2", nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ccoo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fecha_ccoo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_notificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status_contable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    seguros_teso = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fecha_de_pago_no_caf = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_de_pago_caf = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_pago_total = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_sade = table.Column<DateTime>(type: "datetime2", nullable: true),
                    buzon_sade = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    pedido_factura2 = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pedido_factura3 = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_factura_correcta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    caf_si_no = table.Column<bool>(type: "bit", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevengadosExtra", x => x.id);
                    table.ForeignKey(
                        name: "FK_DevengadosExtra_StatusDgayfOpciones_status_dgayf_opcion_id",
                        column: x => x.status_dgayf_opcion_id,
                        principalTable: "StatusDgayfOpciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_DevengadosExtra_StatusOpOpciones_status_op_opcion_id",
                        column: x => x.status_op_opcion_id,
                        principalTable: "StatusOpOpciones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StatusContabilidadExtras",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_dev = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nro_dev = table.Column<int>(type: "int", nullable: false),
                    fecha_pedido_factura2 = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reiterar_pedido_factura3 = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_ingreso_factura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status_contable_opcion_id = table.Column<int>(type: "int", nullable: true),
                    observaciones_cuentas_pagar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tramitador_cuentas_pagar_opcion_id = table.Column<int>(type: "int", nullable: true),
                    tramitador_liquidaciones_opcion_id = table.Column<int>(type: "int", nullable: true),
                    observaciones_liquidaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    falta_poliza = table.Column<bool>(type: "bit", nullable: false),
                    ultimo_movimiento_sade = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusContabilidadExtras", x => x.id);
                    table.ForeignKey(
                        name: "FK_StatusContabilidadExtras_StatusContableOpciones_status_contable_opcion_id",
                        column: x => x.status_contable_opcion_id,
                        principalTable: "StatusContableOpciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StatusContabilidadExtras_TramitadoresCuentasPagar_tramitador_cuentas_pagar_opcion_id",
                        column: x => x.tramitador_cuentas_pagar_opcion_id,
                        principalTable: "TramitadoresCuentasPagar",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StatusContabilidadExtras_TramitadoresLiquidaciones_tramitador_liquidaciones_opcion_id",
                        column: x => x.tramitador_liquidaciones_opcion_id,
                        principalTable: "TramitadoresLiquidaciones",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "StatusContableOpciones",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, "Factura Pedida", 1 },
                    { 2, true, "Frenar", 2 },
                    { 3, true, "Liquidaciones", 3 },
                    { 4, true, "OP Lista", 4 },
                    { 5, true, "Presupuesto", 5 },
                    { 6, true, "Problema SIGAF", 6 },
                    { 7, true, "Seguros", 7 }
                });

            migrationBuilder.InsertData(
                table: "StatusDgayfOpciones",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, "avanzar", 1 },
                    { 2, true, "avanzar CAF", 2 },
                    { 3, true, "no avanzar", 3 },
                    { 4, true, "no avanzar CAF", 4 },
                    { 5, true, "anulado", 5 },
                    { 6, true, "fuera financiera", 6 },
                    { 7, true, "en proceso baja", 7 },
                    { 8, true, "CCOO inf al area", 8 },
                    { 9, true, "para desafectar", 9 },
                    { 10, true, "GUARDA TEMPORAL", 10 },
                    { 11, true, "Proceso Reclamo", 11 }
                });

            migrationBuilder.InsertData(
                table: "StatusOpOpciones",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, "anulado", 1 },
                    { 2, true, "OP Firmada", 2 },
                    { 3, true, "Pasado al pago BONO", 3 },
                    { 4, true, "pagado 2023", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devengados_fecha_imputacion",
                table: "Devengados",
                column: "fecha_imputacion");

            migrationBuilder.CreateIndex(
                name: "IX_Devengados_tipo_dev_nro_dev",
                table: "Devengados",
                columns: new[] { "tipo_dev", "nro_dev" });

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_status_dgayf_opcion_id",
                table: "DevengadosExtra",
                column: "status_dgayf_opcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_status_op_opcion_id",
                table: "DevengadosExtra",
                column: "status_op_opcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_tipo_dev_nro_dev",
                table: "DevengadosExtra",
                columns: new[] { "tipo_dev", "nro_dev" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusContabilidadExtras_status_contable_opcion_id",
                table: "StatusContabilidadExtras",
                column: "status_contable_opcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_StatusContabilidadExtras_tipo_dev_nro_dev",
                table: "StatusContabilidadExtras",
                columns: new[] { "tipo_dev", "nro_dev" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusContabilidadExtras_tramitador_cuentas_pagar_opcion_id",
                table: "StatusContabilidadExtras",
                column: "tramitador_cuentas_pagar_opcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_StatusContabilidadExtras_tramitador_liquidaciones_opcion_id",
                table: "StatusContabilidadExtras",
                column: "tramitador_liquidaciones_opcion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devengados");

            migrationBuilder.DropTable(
                name: "DevengadosExtra");

            migrationBuilder.DropTable(
                name: "StatusContabilidadExtras");

            migrationBuilder.DropTable(
                name: "StatusDgayfOpciones");

            migrationBuilder.DropTable(
                name: "StatusOpOpciones");

            migrationBuilder.DropTable(
                name: "StatusContableOpciones");

            migrationBuilder.DropTable(
                name: "TramitadoresCuentasPagar");

            migrationBuilder.DropTable(
                name: "TramitadoresLiquidaciones");
        }
    }
}

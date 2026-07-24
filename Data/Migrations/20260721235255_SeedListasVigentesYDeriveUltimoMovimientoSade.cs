using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedListasVigentesYDeriveUltimoMovimientoSade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ultimo_movimiento_sade",
                table: "StatusContabilidadExtras");

            migrationBuilder.UpdateData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "activo", "orden" },
                values: new object[] { false, 23 });

            migrationBuilder.InsertData(
                table: "StatusContableOpciones",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 8, true, "Falta Poliza", 2 },
                    { 9, true, "Error en la Factura", 8 },
                    { 10, true, "Error en Documentación", 9 },
                    { 11, true, "No presenta factura", 10 },
                    { 12, true, "Activo Fijo", 11 },
                    { 13, true, "Para pedir FC", 12 },
                    { 14, true, "Fuera de financiera", 13 },
                    { 15, true, "No presenta factura - Envío de CCOO", 14 },
                    { 16, true, "Notificación cédula proveedor", 15 },
                    { 17, true, "Guarda temporal- No presenta Factura", 16 },
                    { 18, true, "DG Financiera", 17 },
                    { 19, true, "Archivado", 18 },
                    { 20, true, "Pendiente", 19 },
                    { 21, true, "Anulado", 20 },
                    { 22, true, "Falta Anexo I Banco", 21 },
                    { 23, true, "No presenta anexo I", 22 }
                });

            migrationBuilder.InsertData(
                table: "TramitadoresCuentasPagar",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, "Daniela", 1 },
                    { 2, true, "Verónica", 2 },
                    { 3, true, "Mariela", 3 },
                    { 4, true, "Ignacio", 4 },
                    { 5, true, "Brian", 5 }
                });

            migrationBuilder.InsertData(
                table: "TramitadoresLiquidaciones",
                columns: new[] { "id", "activo", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, "Mayra", 1 },
                    { 2, true, "Stephanie", 2 },
                    { 3, true, "Camila", 3 },
                    { 4, true, "Gabriel", 4 },
                    { 5, true, "Mauro", 5 },
                    { 6, true, "Tadeo", 6 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "TramitadoresCuentasPagar",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TramitadoresCuentasPagar",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TramitadoresCuentasPagar",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TramitadoresCuentasPagar",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TramitadoresCuentasPagar",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TramitadoresLiquidaciones",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.AddColumn<string>(
                name: "ultimo_movimiento_sade",
                table: "StatusContabilidadExtras",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "StatusContableOpciones",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "activo", "orden" },
                values: new object[] { true, 2 });
        }
    }
}

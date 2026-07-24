using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeguroNormalizadoAListaPeorCasoGana : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "seguro_opcion_id",
                table: "ExpedientesSeguro",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SeguroOpciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false),
                    es_ok = table.Column<bool>(type: "bit", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguroOpciones", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "SeguroOpciones",
                columns: new[] { "id", "activo", "es_ok", "nombre", "orden" },
                values: new object[,]
                {
                    { 1, true, true, "ok", 1 },
                    { 2, true, true, "CAF/ok", 2 },
                    { 3, true, false, "Pendiente", 3 }
                });

            // Backfill: mapear el texto libre viejo a la lista (case-insensitive: cubre "oK").
            // Valores no reconocidos quedan sin opción (null) — se recargan a mano.
            migrationBuilder.Sql("""
                UPDATE es SET seguro_opcion_id = so.id
                FROM ExpedientesSeguro es
                JOIN SeguroOpciones so
                  ON LOWER(LTRIM(RTRIM(es.seguro))) = LOWER(so.nombre)
                WHERE es.seguro IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "seguro",
                table: "ExpedientesSeguro");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesSeguro_seguro_opcion_id",
                table: "ExpedientesSeguro",
                column: "seguro_opcion_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpedientesSeguro_SeguroOpciones_seguro_opcion_id",
                table: "ExpedientesSeguro",
                column: "seguro_opcion_id",
                principalTable: "SeguroOpciones",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpedientesSeguro_SeguroOpciones_seguro_opcion_id",
                table: "ExpedientesSeguro");

            migrationBuilder.DropTable(
                name: "SeguroOpciones");

            migrationBuilder.DropIndex(
                name: "IX_ExpedientesSeguro_seguro_opcion_id",
                table: "ExpedientesSeguro");

            migrationBuilder.DropColumn(
                name: "seguro_opcion_id",
                table: "ExpedientesSeguro");

            migrationBuilder.AddColumn<string>(
                name: "seguro",
                table: "ExpedientesSeguro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}

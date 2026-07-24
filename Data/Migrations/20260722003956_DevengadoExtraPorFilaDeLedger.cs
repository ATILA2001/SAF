using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class DevengadoExtraPorFilaDeLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevengadosExtra_tipo_dev_nro_dev",
                table: "DevengadosExtra");

            migrationBuilder.AddColumn<int>(
                name: "devengado_id",
                table: "DevengadosExtra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: los extras existentes (clave vieja tipo_dev+nro_dev) se asignan a la
            // PRIMERA fila del ledger de su devengado. Huérfanos sin fila se eliminan.
            migrationBuilder.Sql("""
                UPDATE de SET devengado_id = m.min_id
                FROM DevengadosExtra de
                CROSS APPLY (SELECT MIN(d.id) AS min_id
                             FROM Devengados d
                             WHERE d.tipo_dev = de.tipo_dev AND d.nro_dev = de.nro_dev) m
                WHERE m.min_id IS NOT NULL;

                DELETE FROM DevengadosExtra WHERE devengado_id = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_devengado_id",
                table: "DevengadosExtra",
                column: "devengado_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_tipo_dev_nro_dev",
                table: "DevengadosExtra",
                columns: new[] { "tipo_dev", "nro_dev" });

            migrationBuilder.AddForeignKey(
                name: "FK_DevengadosExtra_Devengados_devengado_id",
                table: "DevengadosExtra",
                column: "devengado_id",
                principalTable: "Devengados",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevengadosExtra_Devengados_devengado_id",
                table: "DevengadosExtra");

            migrationBuilder.DropIndex(
                name: "IX_DevengadosExtra_devengado_id",
                table: "DevengadosExtra");

            migrationBuilder.DropIndex(
                name: "IX_DevengadosExtra_tipo_dev_nro_dev",
                table: "DevengadosExtra");

            migrationBuilder.DropColumn(
                name: "devengado_id",
                table: "DevengadosExtra");

            migrationBuilder.CreateIndex(
                name: "IX_DevengadosExtra_tipo_dev_nro_dev",
                table: "DevengadosExtra",
                columns: new[] { "tipo_dev", "nro_dev" },
                unique: true);
        }
    }
}

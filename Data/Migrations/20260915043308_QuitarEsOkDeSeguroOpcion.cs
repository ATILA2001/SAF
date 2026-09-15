using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAF.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuitarEsOkDeSeguroOpcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_ok",
                table: "SeguroOpciones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_ok",
                table: "SeguroOpciones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SeguroOpciones",
                keyColumn: "id",
                keyValue: 1,
                column: "es_ok",
                value: true);

            migrationBuilder.UpdateData(
                table: "SeguroOpciones",
                keyColumn: "id",
                keyValue: 2,
                column: "es_ok",
                value: true);

            migrationBuilder.UpdateData(
                table: "SeguroOpciones",
                keyColumn: "id",
                keyValue: 3,
                column: "es_ok",
                value: false);
        }
    }
}

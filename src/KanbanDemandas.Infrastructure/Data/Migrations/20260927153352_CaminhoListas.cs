using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CaminhoListas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrdemFluxo",
                table: "Listas",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 1,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 2,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 3,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 4,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 5,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 6,
                column: "OrdemFluxo",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 7,
                column: "OrdemFluxo",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrdemFluxo",
                table: "Listas");
        }
    }
}

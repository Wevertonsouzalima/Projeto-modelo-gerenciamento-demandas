using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CamposBloqueadosEtapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CamposFixosBloqueados",
                table: "Listas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 4,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 5,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 6,
                column: "CamposFixosBloqueados",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 7,
                column: "CamposFixosBloqueados",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CamposFixosBloqueados",
                table: "Listas");
        }
    }
}

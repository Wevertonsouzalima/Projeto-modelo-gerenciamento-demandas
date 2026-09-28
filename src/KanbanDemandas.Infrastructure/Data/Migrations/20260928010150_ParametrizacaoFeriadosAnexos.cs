using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParametrizacaoFeriadosAnexos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Comprimido",
                table: "Anexos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "TamanhoArmazenadoBytes",
                table: "Anexos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE `Anexos` SET `TamanhoArmazenadoBytes` = `TamanhoBytes`;");

            migrationBuilder.CreateTable(
                name: "EventosAutomacaoPendentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Gatilho = table.Column<int>(type: "int", nullable: false),
                    ListaId = table.Column<int>(type: "int", nullable: true),
                    Campo = table.Column<int>(type: "int", nullable: false),
                    UsuarioAlvoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExecutarEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAutomacaoPendentes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    RecorrenteAnual = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CriadoPorId = table.Column<int>(type: "int", nullable: false),
                    AlteradoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AlteradoPorId = table.Column<int>(type: "int", nullable: true),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParametrosSistema",
                columns: table => new
                {
                    Chave = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Valor = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlteradoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlteradoPorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosSistema", x => x.Chave);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAutomacaoPendentes_CartaoId",
                table: "EventosAutomacaoPendentes",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAutomacaoPendentes_ExecutarEm",
                table: "EventosAutomacaoPendentes",
                column: "ExecutarEm");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Data",
                table: "Feriados",
                column: "Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosAutomacaoPendentes");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "Comprimido",
                table: "Anexos");

            migrationBuilder.DropColumn(
                name: "TamanhoArmazenadoBytes",
                table: "Anexos");
        }
    }
}

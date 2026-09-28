using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PortalGitTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusPortal",
                table: "Listas",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MensagensTeams",
                table: "ExecucoesAutomacao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Publico",
                table: "Comentarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OrigemPortal",
                table: "Cartoes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoSolicitacao",
                table: "Cartoes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VinculosGit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Repositorio = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Identificador = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titulo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Autor = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosGit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosGit_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 1,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 2,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 3,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 4,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 5,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 6,
                column: "StatusPortal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 7,
                column: "StatusPortal",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_VinculosGit_CartaoId_Tipo_Repositorio_Identificador",
                table: "VinculosGit",
                columns: new[] { "CartaoId", "Tipo", "Repositorio", "Identificador" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VinculosGit");

            migrationBuilder.DropColumn(
                name: "StatusPortal",
                table: "Listas");

            migrationBuilder.DropColumn(
                name: "MensagensTeams",
                table: "ExecucoesAutomacao");

            migrationBuilder.DropColumn(
                name: "Publico",
                table: "Comentarios");

            migrationBuilder.DropColumn(
                name: "OrigemPortal",
                table: "Cartoes");

            migrationBuilder.DropColumn(
                name: "TipoSolicitacao",
                table: "Cartoes");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegrasEtapaEEstorno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CriadoAutomaticamente",
                table: "ValoresCampoCartao",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HistoricoOrigemId",
                table: "ValoresCampoCartao",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorAnteriorAutomatico",
                table: "ValoresCampoCartao",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "BloquearEntradaComPendencias",
                table: "Listas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CamposFixosExigidos",
                table: "Listas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AlterouDataInicio",
                table: "HistoricoAtividades",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicioAnterior",
                table: "HistoricoAtividades",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Estornado",
                table: "HistoricoAtividades",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DefineDataInicioCartao",
                table: "DefinicoesCampo",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Preenchimento",
                table: "DefinicoesCampo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RegraReentrada",
                table: "DefinicoesCampo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "DefinicoesCampo",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DefineDataInicioCartao", "Preenchimento", "RegraReentrada" },
                values: new object[] { false, 0, 0 });

            migrationBuilder.UpdateData(
                table: "DefinicoesCampo",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DefineDataInicioCartao", "Preenchimento", "RegraReentrada" },
                values: new object[] { false, 0, 0 });

            migrationBuilder.UpdateData(
                table: "DefinicoesCampo",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DefineDataInicioCartao", "Preenchimento", "RegraReentrada" },
                values: new object[] { false, 0, 0 });

            migrationBuilder.UpdateData(
                table: "DefinicoesCampo",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DefineDataInicioCartao", "Preenchimento", "RegraReentrada" },
                values: new object[] { false, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.UpdateData(
                table: "Listas",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "BloquearEntradaComPendencias", "CamposFixosExigidos" },
                values: new object[] { false, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_ValoresCampoCartao_HistoricoOrigemId",
                table: "ValoresCampoCartao",
                column: "HistoricoOrigemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ValoresCampoCartao_HistoricoAtividades_HistoricoOrigemId",
                table: "ValoresCampoCartao",
                column: "HistoricoOrigemId",
                principalTable: "HistoricoAtividades",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValoresCampoCartao_HistoricoAtividades_HistoricoOrigemId",
                table: "ValoresCampoCartao");

            migrationBuilder.DropIndex(
                name: "IX_ValoresCampoCartao_HistoricoOrigemId",
                table: "ValoresCampoCartao");

            migrationBuilder.DropColumn(
                name: "CriadoAutomaticamente",
                table: "ValoresCampoCartao");

            migrationBuilder.DropColumn(
                name: "HistoricoOrigemId",
                table: "ValoresCampoCartao");

            migrationBuilder.DropColumn(
                name: "ValorAnteriorAutomatico",
                table: "ValoresCampoCartao");

            migrationBuilder.DropColumn(
                name: "BloquearEntradaComPendencias",
                table: "Listas");

            migrationBuilder.DropColumn(
                name: "CamposFixosExigidos",
                table: "Listas");

            migrationBuilder.DropColumn(
                name: "AlterouDataInicio",
                table: "HistoricoAtividades");

            migrationBuilder.DropColumn(
                name: "DataInicioAnterior",
                table: "HistoricoAtividades");

            migrationBuilder.DropColumn(
                name: "Estornado",
                table: "HistoricoAtividades");

            migrationBuilder.DropColumn(
                name: "DefineDataInicioCartao",
                table: "DefinicoesCampo");

            migrationBuilder.DropColumn(
                name: "Preenchimento",
                table: "DefinicoesCampo");

            migrationBuilder.DropColumn(
                name: "RegraReentrada",
                table: "DefinicoesCampo");
        }
    }
}

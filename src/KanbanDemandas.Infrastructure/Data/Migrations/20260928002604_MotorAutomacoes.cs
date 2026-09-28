using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MotorAutomacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropForeignKey(
                name: "FK_RegrasAutomacao_Listas_ListaId",
                table: "RegrasAutomacao");

            
            migrationBuilder.AddColumn<bool>(
                name: "ReceberEmailsAutomacao",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ListaId",
                table: "RegrasAutomacao",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CondicoesJson",
                table: "RegrasAutomacao",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "RegrasAutomacao",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ExigirTodasCondicoes",
                table: "RegrasAutomacao",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Gatilho",
                table: "RegrasAutomacao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ParametrosGatilhoJson",
                table: "RegrasAutomacao",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "QuadroId",
                table: "RegrasAutomacao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SomenteHorarioComercial",
                table: "RegrasAutomacao",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaExecucaoEm",
                table: "RegrasAutomacao",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "AcoesAutomacao",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ParametrosJson",
                table: "AcoesAutomacao",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Converte as regras antigas (gatilho implícito "entrou na lista" e ações com colunas fixas) para o novo modelo.
            migrationBuilder.Sql(@"
UPDATE `RegrasAutomacao` r JOIN `Listas` l ON l.`Id` = r.`ListaId`
SET r.`QuadroId` = l.`QuadroId`, r.`Gatilho` = 2, r.`ExigirTodasCondicoes` = TRUE;");
            migrationBuilder.Sql(@"
UPDATE `AcoesAutomacao` a
LEFT JOIN `DefinicoesCampo` d ON d.`Id` = a.`DefinicaoCampoId`
SET a.`Ordem` = a.`Id`,
    a.`ParametrosJson` = CASE a.`Tipo`
        WHEN 1 THEN JSON_OBJECT('ListaId', a.`ListaDestinoId`)
        WHEN 2 THEN JSON_OBJECT('Destinatarios', 'Desenvolvedores')
        WHEN 3 THEN JSON_OBJECT('CampoNome', d.`Nome`, 'Valor', a.`ValorCampo`,
                                'SomenteSeVazio', IF(a.`SomenteSeVazio`, CAST('true' AS JSON), CAST('false' AS JSON)))
        ELSE NULL END;");
            migrationBuilder.Sql(@"
DELETE a FROM `AcoesAutomacao` a JOIN `RegrasAutomacao` r ON r.`Id` = a.`RegraAutomacaoId` WHERE r.`QuadroId` = 0;");
            migrationBuilder.Sql(@"DELETE FROM `RegrasAutomacao` WHERE `QuadroId` = 0;");
            migrationBuilder.Sql(@"UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE;");

            migrationBuilder.DropForeignKey(
                name: "FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropForeignKey(
                name: "FK_AcoesAutomacao_Listas_ListaDestinoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropIndex(
                name: "IX_AcoesAutomacao_DefinicaoCampoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropIndex(
                name: "IX_AcoesAutomacao_ListaDestinoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropColumn(
                name: "DefinicaoCampoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropColumn(
                name: "ListaDestinoId",
                table: "AcoesAutomacao");

            migrationBuilder.DropColumn(
                name: "SomenteSeVazio",
                table: "AcoesAutomacao");

            migrationBuilder.DropColumn(
                name: "ValorCampo",
                table: "AcoesAutomacao");


            migrationBuilder.CreateTable(
                name: "AlertasCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    RegraAutomacaoId = table.Column<int>(type: "int", nullable: true),
                    Mensagem = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severidade = table.Column<int>(type: "int", nullable: false),
                    Resolvido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvidoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasCartao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertasCartao_RegrasAutomacao_RegraAutomacaoId",
                        column: x => x.RegraAutomacaoId,
                        principalTable: "RegrasAutomacao",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExecucoesAutomacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegraAutomacaoId = table.Column<int>(type: "int", nullable: false),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: true),
                    Gatilho = table.Column<int>(type: "int", nullable: false),
                    ChaveDisparo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Resumo = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Erro = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesfazerJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailsEnviados = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DesfeitaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DesfeitaPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucoesAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecucoesAutomacao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecucoesAutomacao_RegrasAutomacao_RegraAutomacaoId",
                        column: x => x.RegraAutomacaoId,
                        principalTable: "RegrasAutomacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SugestoesAutomacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegraAutomacaoId = table.Column<int>(type: "int", nullable: false),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PropostaJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecididoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DecididoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SugestoesAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SugestoesAutomacao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SugestoesAutomacao_RegrasAutomacao_RegraAutomacaoId",
                        column: x => x.RegraAutomacaoId,
                        principalTable: "RegrasAutomacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReceberEmailsAutomacao",
                value: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReceberEmailsAutomacao",
                value: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "ReceberEmailsAutomacao",
                value: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 4,
                column: "ReceberEmailsAutomacao",
                value: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 5,
                column: "ReceberEmailsAutomacao",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegrasAutomacao_QuadroId_Gatilho",
                table: "RegrasAutomacao",
                columns: new[] { "QuadroId", "Gatilho" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCartao_CartaoId_Resolvido",
                table: "AlertasCartao",
                columns: new[] { "CartaoId", "Resolvido" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCartao_RegraAutomacaoId",
                table: "AlertasCartao",
                column: "RegraAutomacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesAutomacao_CartaoId",
                table: "ExecucoesAutomacao",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesAutomacao_QuadroId_OcorridoEm",
                table: "ExecucoesAutomacao",
                columns: new[] { "QuadroId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesAutomacao_RegraAutomacaoId_CartaoId_ChaveDisparo",
                table: "ExecucoesAutomacao",
                columns: new[] { "RegraAutomacaoId", "CartaoId", "ChaveDisparo" });

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesAutomacao_CartaoId_Status",
                table: "SugestoesAutomacao",
                columns: new[] { "CartaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesAutomacao_QuadroId_Status",
                table: "SugestoesAutomacao",
                columns: new[] { "QuadroId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesAutomacao_RegraAutomacaoId",
                table: "SugestoesAutomacao",
                column: "RegraAutomacaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegrasAutomacao_Listas_ListaId",
                table: "RegrasAutomacao",
                column: "ListaId",
                principalTable: "Listas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegrasAutomacao_Quadros_QuadroId",
                table: "RegrasAutomacao",
                column: "QuadroId",
                principalTable: "Quadros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegrasAutomacao_Listas_ListaId",
                table: "RegrasAutomacao");

            migrationBuilder.DropForeignKey(
                name: "FK_RegrasAutomacao_Quadros_QuadroId",
                table: "RegrasAutomacao");

            migrationBuilder.DropTable(
                name: "AlertasCartao");

            migrationBuilder.DropTable(
                name: "ExecucoesAutomacao");

            migrationBuilder.DropTable(
                name: "SugestoesAutomacao");

            migrationBuilder.DropIndex(
                name: "IX_RegrasAutomacao_QuadroId_Gatilho",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "ReceberEmailsAutomacao",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CondicoesJson",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "ExigirTodasCondicoes",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "Gatilho",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "ParametrosGatilhoJson",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "QuadroId",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "SomenteHorarioComercial",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "UltimaExecucaoEm",
                table: "RegrasAutomacao");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "AcoesAutomacao");

            migrationBuilder.DropColumn(
                name: "ParametrosJson",
                table: "AcoesAutomacao");

            migrationBuilder.AlterColumn<int>(
                name: "ListaId",
                table: "RegrasAutomacao",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefinicaoCampoId",
                table: "AcoesAutomacao",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ListaDestinoId",
                table: "AcoesAutomacao",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SomenteSeVazio",
                table: "AcoesAutomacao",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ValorCampo",
                table: "AcoesAutomacao",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesAutomacao_DefinicaoCampoId",
                table: "AcoesAutomacao",
                column: "DefinicaoCampoId");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesAutomacao_ListaDestinoId",
                table: "AcoesAutomacao",
                column: "ListaDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId",
                table: "AcoesAutomacao",
                column: "DefinicaoCampoId",
                principalTable: "DefinicoesCampo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AcoesAutomacao_Listas_ListaDestinoId",
                table: "AcoesAutomacao",
                column: "ListaDestinoId",
                principalTable: "Listas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegrasAutomacao_Listas_ListaId",
                table: "RegrasAutomacao",
                column: "ListaId",
                principalTable: "Listas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

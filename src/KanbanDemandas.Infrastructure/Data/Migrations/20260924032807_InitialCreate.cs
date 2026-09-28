using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KanbanDemandas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Quadros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "#1565C0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateQuadroId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Quadros", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sistemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_Sistemas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TemplatesQuadro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstruturaJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_TemplatesQuadro", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NotificarPorEmail = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Etiquetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "#607D8B")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Etiquetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etiquetas_Quadros_QuadroId",
                        column: x => x.QuadroId,
                        principalTable: "Quadros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Listas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
                    ListaPaiId = table.Column<int>(type: "int", nullable: true),
                    LimiteWip = table.Column<int>(type: "int", nullable: true),
                    EhBacklog = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_Listas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listas_Listas_ListaPaiId",
                        column: x => x.ListaPaiId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listas_Quadros_QuadroId",
                        column: x => x.QuadroId,
                        principalTable: "Quadros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Meta = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Fechada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Quadros_QuadroId",
                        column: x => x.QuadroId,
                        principalTable: "Quadros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TemplatesCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitulopadraO = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescricaoPadrao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuadroId = table.Column<int>(type: "int", nullable: false),
                    ItensTarefaPadrao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CamposPadrao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_TemplatesCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplatesCartao_Quadros_QuadroId",
                        column: x => x.QuadroId,
                        principalTable: "Quadros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cartoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ListaId = table.Column<int>(type: "int", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Prazo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Estimativa = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Prioridade = table.Column<int>(type: "int", nullable: true),
                    SistemaId = table.Column<int>(type: "int", nullable: true),
                    SolicitanteId = table.Column<int>(type: "int", nullable: true),
                    CartaoPaiId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Cartoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cartoes_Cartoes_CartaoPaiId",
                        column: x => x.CartaoPaiId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cartoes_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cartoes_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cartoes_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DefinicoesCampo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ListaId = table.Column<int>(type: "int", nullable: false),
                    Obrigatorio = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PedirNovamenteACadaEntrada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OpcoesSelecao = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DefinicoesCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefinicoesCampo_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegrasAutomacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ListaId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RegrasAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegrasAutomacao_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Anexos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    NomeOriginal = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeArmazenado = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Caminho = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    EnviadoPorId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Anexos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anexos_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Anexos_Usuarios_EnviadoPorId",
                        column: x => x.EnviadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CartaoDesenvolvedores",
                columns: table => new
                {
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Principal = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartaoDesenvolvedores", x => new { x.CartaoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_CartaoDesenvolvedores_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartaoDesenvolvedores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CartaoEtiquetas",
                columns: table => new
                {
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    EtiquetaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartaoEtiquetas", x => new { x.CartaoId, x.EtiquetaId });
                    table.ForeignKey(
                        name: "FK_CartaoEtiquetas_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartaoEtiquetas_Etiquetas_EtiquetaId",
                        column: x => x.EtiquetaId,
                        principalTable: "Etiquetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmailsCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Para = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cc = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Assunto = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorpoHtml = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnviadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EnviadoPorId = table.Column<int>(type: "int", nullable: false),
                    Enviado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErroEnvio = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_EmailsCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailsCartao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailsCartao_Usuarios_EnviadoPorId",
                        column: x => x.EnviadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoricoAtividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ListaOrigemId = table.Column<int>(type: "int", nullable: true),
                    ListaDestinoId = table.Column<int>(type: "int", nullable: true),
                    Descricao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorAnterior = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorNovo = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAtividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoAtividades_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricoAtividades_Listas_ListaDestinoId",
                        column: x => x.ListaDestinoId,
                        principalTable: "Listas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoAtividades_Listas_ListaOrigemId",
                        column: x => x.ListaOrigemId,
                        principalTable: "Listas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoAtividades_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItensTarefa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Concluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesenvolvedorId = table.Column<int>(type: "int", nullable: true),
                    CartaoPromovidoId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ItensTarefa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensTarefa_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensTarefa_Cartoes_CartaoPromovidoId",
                        column: x => x.CartaoPromovidoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItensTarefa_Usuarios_DesenvolvedorId",
                        column: x => x.DesenvolvedorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DestinatarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Mensagem = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Lida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LidaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CartaoOrigemId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Cartoes_CartaoOrigemId",
                        column: x => x.CartaoOrigemId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Usuarios_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RelacoesCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoOrigemId = table.Column<int>(type: "int", nullable: false),
                    CartaoDestinoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RelacoesCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelacoesCartao_Cartoes_CartaoDestinoId",
                        column: x => x.CartaoDestinoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelacoesCartao_Cartoes_CartaoOrigemId",
                        column: x => x.CartaoOrigemId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SprintCartoes",
                columns: table => new
                {
                    SprintId = table.Column<int>(type: "int", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    AdicionadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprintCartoes", x => new { x.SprintId, x.CartaoId });
                    table.ForeignKey(
                        name: "FK_SprintCartoes_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SprintCartoes_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValoresCampoCartao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CartaoId = table.Column<int>(type: "int", nullable: false),
                    DefinicaoCampoId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroEntradaNaLista = table.Column<int>(type: "int", nullable: false),
                    DataPreenchimento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PreenchidoPorId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ValoresCampoCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoresCampoCartao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValoresCampoCartao_DefinicoesCampo_DefinicaoCampoId",
                        column: x => x.DefinicaoCampoId,
                        principalTable: "DefinicoesCampo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValoresCampoCartao_Usuarios_PreenchidoPorId",
                        column: x => x.PreenchidoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AcoesAutomacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegraAutomacaoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ListaDestinoId = table.Column<int>(type: "int", nullable: true),
                    DefinicaoCampoId = table.Column<int>(type: "int", nullable: true),
                    ValorCampo = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SomenteSeVazio = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_AcoesAutomacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId",
                        column: x => x.DefinicaoCampoId,
                        principalTable: "DefinicoesCampo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcoesAutomacao_Listas_ListaDestinoId",
                        column: x => x.ListaDestinoId,
                        principalTable: "Listas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcoesAutomacao_RegrasAutomacao_RegraAutomacaoId",
                        column: x => x.RegraAutomacaoId,
                        principalTable: "RegrasAutomacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Texto = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Editado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: true),
                    ItemTarefaId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Comentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comentarios_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comentarios_ItensTarefa_ItemTarefaId",
                        column: x => x.ItemTarefaId,
                        principalTable: "ItensTarefa",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comentarios_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reunioes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Ata = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    CartaoId = table.Column<int>(type: "int", nullable: true),
                    ItemTarefaId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Reunioes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reunioes_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reunioes_ItensTarefa_ItemTarefaId",
                        column: x => x.ItemTarefaId,
                        principalTable: "ItensTarefa",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reunioes_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReuniaoParticipantes",
                columns: table => new
                {
                    ReuniaoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReuniaoParticipantes", x => new { x.ReuniaoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_ReuniaoParticipantes_Reunioes_ReuniaoId",
                        column: x => x.ReuniaoId,
                        principalTable: "Reunioes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReuniaoParticipantes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Quadros",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "Cor", "CriadoEm", "CriadoPorId", "Descricao", "Excluido", "ExcluidoEm", "ExcluidoPorId", "Nome", "TemplateQuadroId" },
                values: new object[] { 1, null, null, "#1565C0", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Quadro principal do time", false, null, null, "Time de Desenvolvimento", null });

            migrationBuilder.InsertData(
                table: "Sistemas",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "Ativo", "CriadoEm", "CriadoPorId", "Excluido", "ExcluidoEm", "ExcluidoPorId", "Nome" },
                values: new object[,]
                {
                    { 1, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Portal do Cliente" },
                    { 2, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "ERP Interno" },
                    { 3, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "App Mobile" },
                    { 4, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Integrações" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "Ativo", "CriadoEm", "CriadoPorId", "Email", "Excluido", "ExcluidoEm", "ExcluidoPorId", "Nome", "NotificarPorEmail" },
                values: new object[,]
                {
                    { 1, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "admin@empresa.com", false, null, null, "Admin", false },
                    { 2, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "ana.lima@empresa.com", false, null, null, "Ana Lima", true },
                    { 3, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "bruno.costa@empresa.com", false, null, null, "Bruno Costa", true },
                    { 4, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "carla.mendes@empresa.com", false, null, null, "Carla Mendes", false },
                    { 5, null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "diego.santos@empresa.com", false, null, null, "Diego Santos", true }
                });

            migrationBuilder.InsertData(
                table: "Etiquetas",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "Cor", "CriadoEm", "CriadoPorId", "Excluido", "ExcluidoEm", "ExcluidoPorId", "Nome", "QuadroId" },
                values: new object[,]
                {
                    { 1, null, null, "#E53935", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Bug", 1 },
                    { 2, null, null, "#43A047", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Feature", 1 },
                    { 3, null, null, "#1E88E5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Melhoria", 1 },
                    { 4, null, null, "#FB8C00", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Urgente", 1 },
                    { 5, null, null, "#8E24AA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, "Técnico", 1 }
                });

            migrationBuilder.InsertData(
                table: "Listas",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "CriadoEm", "CriadoPorId", "EhBacklog", "Excluido", "ExcluidoEm", "ExcluidoPorId", "LimiteWip", "ListaPaiId", "Nome", "Ordem", "QuadroId" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, null, null, null, null, "Backlog", 1, 1 },
                    { 2, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, 10, null, "A Fazer", 2, 1 },
                    { 3, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, 5, null, "Em Andamento", 3, 1 },
                    { 4, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, 3, null, "Code Review", 4, 1 },
                    { 5, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, null, null, "Homologação", 5, 1 },
                    { 6, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, null, null, "Pré-produção", 6, 1 },
                    { 7, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, false, null, null, null, null, "Concluído", 7, 1 }
                });

            migrationBuilder.InsertData(
                table: "DefinicoesCampo",
                columns: new[] { "Id", "AlteradoEm", "AlteradoPorId", "CriadoEm", "CriadoPorId", "Excluido", "ExcluidoEm", "ExcluidoPorId", "ListaId", "Nome", "Obrigatorio", "OpcoesSelecao", "Ordem", "PedirNovamenteACadaEntrada", "Tipo" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, 5, "Data Homologado", true, null, 1, true, 3 },
                    { 2, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, 5, "Homologado Por", false, null, 2, false, 1 },
                    { 3, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, 6, "Data Prevista de Change", true, null, 1, false, 3 },
                    { 4, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, null, null, 6, "Número do Change", false, null, 2, false, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcoesAutomacao_DefinicaoCampoId",
                table: "AcoesAutomacao",
                column: "DefinicaoCampoId");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesAutomacao_ListaDestinoId",
                table: "AcoesAutomacao",
                column: "ListaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_AcoesAutomacao_RegraAutomacaoId",
                table: "AcoesAutomacao",
                column: "RegraAutomacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_CartaoId",
                table: "Anexos",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_EnviadoPorId",
                table: "Anexos",
                column: "EnviadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_CartaoDesenvolvedores_UsuarioId",
                table: "CartaoDesenvolvedores",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CartaoEtiquetas_EtiquetaId",
                table: "CartaoEtiquetas",
                column: "EtiquetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_CartaoPaiId",
                table: "Cartoes",
                column: "CartaoPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_ListaId",
                table: "Cartoes",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_SistemaId",
                table: "Cartoes",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_SolicitanteId",
                table: "Cartoes",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_AutorId",
                table: "Comentarios",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_CartaoId",
                table: "Comentarios",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_ItemTarefaId",
                table: "Comentarios",
                column: "ItemTarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_DefinicoesCampo_ListaId",
                table: "DefinicoesCampo",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailsCartao_CartaoId",
                table: "EmailsCartao",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailsCartao_EnviadoPorId",
                table: "EmailsCartao",
                column: "EnviadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Etiquetas_QuadroId",
                table: "Etiquetas",
                column: "QuadroId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAtividades_CartaoId_OcorridoEm",
                table: "HistoricoAtividades",
                columns: new[] { "CartaoId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAtividades_ListaDestinoId_OcorridoEm",
                table: "HistoricoAtividades",
                columns: new[] { "ListaDestinoId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAtividades_ListaOrigemId",
                table: "HistoricoAtividades",
                column: "ListaOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAtividades_UsuarioId",
                table: "HistoricoAtividades",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensTarefa_CartaoId",
                table: "ItensTarefa",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensTarefa_CartaoPromovidoId",
                table: "ItensTarefa",
                column: "CartaoPromovidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensTarefa_DesenvolvedorId",
                table: "ItensTarefa",
                column: "DesenvolvedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Listas_ListaPaiId",
                table: "Listas",
                column: "ListaPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_Listas_QuadroId",
                table: "Listas",
                column: "QuadroId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_CartaoOrigemId",
                table: "Notificacoes",
                column: "CartaoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_DestinatarioId_Lida",
                table: "Notificacoes",
                columns: new[] { "DestinatarioId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_RegrasAutomacao_ListaId",
                table: "RegrasAutomacao",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacoesCartao_CartaoDestinoId",
                table: "RelacoesCartao",
                column: "CartaoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacoesCartao_CartaoOrigemId",
                table: "RelacoesCartao",
                column: "CartaoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReuniaoParticipantes_UsuarioId",
                table: "ReuniaoParticipantes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reunioes_AutorId",
                table: "Reunioes",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Reunioes_CartaoId",
                table: "Reunioes",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reunioes_ItemTarefaId",
                table: "Reunioes",
                column: "ItemTarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_SprintCartoes_CartaoId",
                table: "SprintCartoes",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_QuadroId",
                table: "Sprints",
                column: "QuadroId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatesCartao_QuadroId",
                table: "TemplatesCartao",
                column: "QuadroId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoresCampoCartao_CartaoId_DefinicaoCampoId_NumeroEntradaNa~",
                table: "ValoresCampoCartao",
                columns: new[] { "CartaoId", "DefinicaoCampoId", "NumeroEntradaNaLista" });

            migrationBuilder.CreateIndex(
                name: "IX_ValoresCampoCartao_DefinicaoCampoId",
                table: "ValoresCampoCartao",
                column: "DefinicaoCampoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresCampoCartao_PreenchidoPorId",
                table: "ValoresCampoCartao",
                column: "PreenchidoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcoesAutomacao");

            migrationBuilder.DropTable(
                name: "Anexos");

            migrationBuilder.DropTable(
                name: "CartaoDesenvolvedores");

            migrationBuilder.DropTable(
                name: "CartaoEtiquetas");

            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "EmailsCartao");

            migrationBuilder.DropTable(
                name: "HistoricoAtividades");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "RelacoesCartao");

            migrationBuilder.DropTable(
                name: "ReuniaoParticipantes");

            migrationBuilder.DropTable(
                name: "SprintCartoes");

            migrationBuilder.DropTable(
                name: "TemplatesCartao");

            migrationBuilder.DropTable(
                name: "TemplatesQuadro");

            migrationBuilder.DropTable(
                name: "ValoresCampoCartao");

            migrationBuilder.DropTable(
                name: "RegrasAutomacao");

            migrationBuilder.DropTable(
                name: "Etiquetas");

            migrationBuilder.DropTable(
                name: "Reunioes");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropTable(
                name: "DefinicoesCampo");

            migrationBuilder.DropTable(
                name: "ItensTarefa");

            migrationBuilder.DropTable(
                name: "Cartoes");

            migrationBuilder.DropTable(
                name: "Listas");

            migrationBuilder.DropTable(
                name: "Sistemas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Quadros");
        }
    }
}

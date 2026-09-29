/*
    Kanban de Demandas: criação completa do banco no SQL Server (2016 ou superior).

    Cria o banco, todas as tabelas, chaves, índices e os dados iniciais (usuários, sistemas,
    quadro de exemplo). Tudo roda numa transação: ou cria tudo, ou não cria nada.
    Se as tabelas já existirem, o script para sem alterar nada.

    Pelo SSMS: abra o arquivo e execute (F5).
    Pela linha de comando: sqlcmd -S <servidor> -E -b -i KanbanDemandas.SqlServer.sql

    Se o banco for criado pelo DBA, basta ele já existir vazio: o script pula o CREATE DATABASE.
    Nesse caso, peça também o collation Latin1_General_100_CI_AI e o READ_COMMITTED_SNAPSHOT ON.

    Para usar outro nome de banco, troque "KanbanDemandas" neste arquivo e na connection string.
*/

IF DB_ID(N'KanbanDemandas') IS NULL
BEGIN
    -- CI_AI: comparações e buscas ignoram maiúsculas e acentos (mesmo comportamento do MySQL anterior).
    CREATE DATABASE [KanbanDemandas] COLLATE Latin1_General_100_CI_AI;
    -- Leituras não esperam por gravações em andamento (evita bloqueios entre telas do Blazor).
    ALTER DATABASE [KanbanDemandas] SET READ_COMMITTED_SNAPSHOT ON;
END;
GO

USE [KanbanDemandas];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'KanbanDemandas'
BEGIN
    RAISERROR(N'O banco KanbanDemandas não está em uso (falhou a criação?). Nada foi alterado.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'[dbo].[Quadros]', N'U') IS NOT NULL
BEGIN
    RAISERROR(N'As tabelas do Kanban já existem neste banco. Nada foi alterado.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

    CREATE TABLE [dbo].[EventosAutomacaoPendentes] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [Gatilho] int NOT NULL,
        [ListaId] int NULL,
        [Campo] int NOT NULL,
        [UsuarioAlvoId] int NULL,
        [UsuarioId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [ExecutarEm] datetime2 NOT NULL,
        CONSTRAINT [PK_EventosAutomacaoPendentes] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[Feriados] (
        [Id] int NOT NULL IDENTITY,
        [Data] date NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Tipo] int NOT NULL,
        [RecorrenteAnual] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Feriados] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[ParametrosSistema] (
        [Chave] nvarchar(200) NOT NULL,
        [Valor] nvarchar(2000) NULL,
        [AlteradoEm] datetime2 NOT NULL,
        [AlteradoPorId] int NOT NULL,
        CONSTRAINT [PK_ParametrosSistema] PRIMARY KEY ([Chave])
    );

    CREATE TABLE [dbo].[Quadros] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(1000) NULL,
        [Cor] nvarchar(20) NOT NULL CONSTRAINT [DF_Quadros_Cor] DEFAULT N'#1565C0',
        [TemplateQuadroId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Quadros] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[Sistemas] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Ativo] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Sistemas] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[TemplatesQuadro] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(1000) NULL,
        [EstruturaJson] nvarchar(max) NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_TemplatesQuadro] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [Ativo] bit NOT NULL,
        [NotificarPorEmail] bit NOT NULL,
        [ReceberEmailsAutomacao] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );

    CREATE TABLE [dbo].[Etiquetas] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Cor] nvarchar(20) NOT NULL CONSTRAINT [DF_Etiquetas_Cor] DEFAULT N'#607D8B',
        [QuadroId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Etiquetas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Etiquetas_Quadros_QuadroId] FOREIGN KEY ([QuadroId]) REFERENCES [dbo].[Quadros] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[Listas] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Ordem] int NOT NULL,
        [QuadroId] int NOT NULL,
        [ListaPaiId] int NULL,
        [LimiteWip] int NULL,
        [EhBacklog] bit NOT NULL,
        [OrdemFluxo] int NULL,
        [CamposFixosExigidos] int NOT NULL,
        [CamposFixosBloqueados] int NOT NULL,
        [BloquearEntradaComPendencias] bit NOT NULL,
        [StatusPortal] nvarchar(100) NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Listas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Listas_Listas_ListaPaiId] FOREIGN KEY ([ListaPaiId]) REFERENCES [dbo].[Listas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Listas_Quadros_QuadroId] FOREIGN KEY ([QuadroId]) REFERENCES [dbo].[Quadros] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[Sprints] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Meta] nvarchar(1000) NULL,
        [DataInicio] datetime2 NOT NULL,
        [DataFim] datetime2 NOT NULL,
        [Ativa] bit NOT NULL,
        [Fechada] bit NOT NULL,
        [QuadroId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Sprints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sprints_Quadros_QuadroId] FOREIGN KEY ([QuadroId]) REFERENCES [dbo].[Quadros] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[TemplatesCartao] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [TituloPadrao] nvarchar(500) NULL,
        [DescricaoPadrao] nvarchar(max) NULL,
        [QuadroId] int NOT NULL,
        [ItensTarefaPadrao] nvarchar(max) NULL,
        [CamposPadrao] nvarchar(max) NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_TemplatesCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TemplatesCartao_Quadros_QuadroId] FOREIGN KEY ([QuadroId]) REFERENCES [dbo].[Quadros] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[Cartoes] (
        [Id] int NOT NULL IDENTITY,
        [Titulo] nvarchar(500) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [ListaId] int NOT NULL,
        [OrigemPortal] bit NOT NULL,
        [TipoSolicitacao] nvarchar(100) NULL,
        [Ordem] int NOT NULL,
        [DataInicio] datetime2 NULL,
        [Prazo] datetime2 NULL,
        [Estimativa] decimal(10,2) NULL,
        [Prioridade] int NULL,
        [SistemaId] int NULL,
        [SolicitanteId] int NULL,
        [CartaoPaiId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Cartoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cartoes_Cartoes_CartaoPaiId] FOREIGN KEY ([CartaoPaiId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cartoes_Listas_ListaId] FOREIGN KEY ([ListaId]) REFERENCES [dbo].[Listas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cartoes_Sistemas_SistemaId] FOREIGN KEY ([SistemaId]) REFERENCES [dbo].[Sistemas] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Cartoes_Usuarios_SolicitanteId] FOREIGN KEY ([SolicitanteId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL
    );

    CREATE TABLE [dbo].[DefinicoesCampo] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Tipo] int NOT NULL,
        [ListaId] int NOT NULL,
        [Obrigatorio] bit NOT NULL,
        [PedirNovamenteACadaEntrada] bit NOT NULL,
        [OpcoesSelecao] nvarchar(2000) NULL,
        [Ordem] int NOT NULL,
        [Preenchimento] int NOT NULL,
        [RegraReentrada] int NOT NULL,
        [DefineDataInicioCartao] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_DefinicoesCampo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DefinicoesCampo_Listas_ListaId] FOREIGN KEY ([ListaId]) REFERENCES [dbo].[Listas] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[RegrasAutomacao] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(1000) NULL,
        [Ativa] bit NOT NULL,
        [QuadroId] int NOT NULL,
        [Gatilho] int NOT NULL,
        [ListaId] int NULL,
        [ParametrosGatilhoJson] nvarchar(4000) NULL,
        [CondicoesJson] nvarchar(max) NULL,
        [ExigirTodasCondicoes] bit NOT NULL,
        [SomenteHorarioComercial] bit NOT NULL,
        [UltimaExecucaoEm] datetime2 NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_RegrasAutomacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegrasAutomacao_Listas_ListaId] FOREIGN KEY ([ListaId]) REFERENCES [dbo].[Listas] ([Id]),
        CONSTRAINT [FK_RegrasAutomacao_Quadros_QuadroId] FOREIGN KEY ([QuadroId]) REFERENCES [dbo].[Quadros] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[Anexos] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [NomeOriginal] nvarchar(500) NOT NULL,
        [NomeArmazenado] nvarchar(500) NOT NULL,
        [Caminho] nvarchar(1000) NOT NULL,
        [ContentType] nvarchar(200) NOT NULL,
        [TamanhoBytes] bigint NOT NULL,
        [Comprimido] bit NOT NULL,
        [TamanhoArmazenadoBytes] bigint NOT NULL,
        [EnviadoPorId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Anexos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Anexos_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Anexos_Usuarios_EnviadoPorId] FOREIGN KEY ([EnviadoPorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[CartaoDesenvolvedores] (
        [CartaoId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [Principal] bit NOT NULL,
        CONSTRAINT [PK_CartaoDesenvolvedores] PRIMARY KEY ([CartaoId], [UsuarioId]),
        CONSTRAINT [FK_CartaoDesenvolvedores_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartaoDesenvolvedores_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[CartaoEtiquetas] (
        [CartaoId] int NOT NULL,
        [EtiquetaId] int NOT NULL,
        CONSTRAINT [PK_CartaoEtiquetas] PRIMARY KEY ([CartaoId], [EtiquetaId]),
        CONSTRAINT [FK_CartaoEtiquetas_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartaoEtiquetas_Etiquetas_EtiquetaId] FOREIGN KEY ([EtiquetaId]) REFERENCES [dbo].[Etiquetas] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[EmailsCartao] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [Para] nvarchar(500) NOT NULL,
        [Cc] nvarchar(500) NULL,
        [Assunto] nvarchar(500) NOT NULL,
        [CorpoHtml] nvarchar(max) NOT NULL,
        [EnviadoEm] datetime2 NOT NULL,
        [EnviadoPorId] int NOT NULL,
        [Enviado] bit NOT NULL,
        [ErroEnvio] nvarchar(2000) NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_EmailsCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailsCartao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmailsCartao_Usuarios_EnviadoPorId] FOREIGN KEY ([EnviadoPorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[HistoricoAtividades] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [Tipo] int NOT NULL,
        [ListaOrigemId] int NULL,
        [ListaDestinoId] int NULL,
        [Descricao] nvarchar(1000) NOT NULL,
        [ValorAnterior] nvarchar(2000) NULL,
        [ValorNovo] nvarchar(2000) NULL,
        [UsuarioId] int NOT NULL,
        [OcorridoEm] datetime2 NOT NULL,
        [Estornado] bit NOT NULL,
        [AlterouDataInicio] bit NOT NULL,
        [DataInicioAnterior] datetime2 NULL,
        CONSTRAINT [PK_HistoricoAtividades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoricoAtividades_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HistoricoAtividades_Listas_ListaDestinoId] FOREIGN KEY ([ListaDestinoId]) REFERENCES [dbo].[Listas] ([Id]),
        CONSTRAINT [FK_HistoricoAtividades_Listas_ListaOrigemId] FOREIGN KEY ([ListaOrigemId]) REFERENCES [dbo].[Listas] ([Id]),
        CONSTRAINT [FK_HistoricoAtividades_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[ItensTarefa] (
        [Id] int NOT NULL IDENTITY,
        [Titulo] nvarchar(500) NOT NULL,
        [Concluido] bit NOT NULL,
        [CartaoId] int NOT NULL,
        [Ordem] int NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [DesenvolvedorId] int NULL,
        [CartaoPromovidoId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_ItensTarefa] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ItensTarefa_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ItensTarefa_Cartoes_CartaoPromovidoId] FOREIGN KEY ([CartaoPromovidoId]) REFERENCES [dbo].[Cartoes] ([Id]),
        CONSTRAINT [FK_ItensTarefa_Usuarios_DesenvolvedorId] FOREIGN KEY ([DesenvolvedorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL
    );

    CREATE TABLE [dbo].[Notificacoes] (
        [Id] int NOT NULL IDENTITY,
        [DestinatarioId] int NOT NULL,
        [Tipo] int NOT NULL,
        [Mensagem] nvarchar(1000) NOT NULL,
        [Lida] bit NOT NULL,
        [LidaEm] datetime2 NULL,
        [CartaoOrigemId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Notificacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notificacoes_Cartoes_CartaoOrigemId] FOREIGN KEY ([CartaoOrigemId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Notificacoes_Usuarios_DestinatarioId] FOREIGN KEY ([DestinatarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[RelacoesCartao] (
        [Id] int NOT NULL IDENTITY,
        [CartaoOrigemId] int NOT NULL,
        [CartaoDestinoId] int NOT NULL,
        [Tipo] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_RelacoesCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RelacoesCartao_Cartoes_CartaoDestinoId] FOREIGN KEY ([CartaoDestinoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RelacoesCartao_Cartoes_CartaoOrigemId] FOREIGN KEY ([CartaoOrigemId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[SprintCartoes] (
        [SprintId] int NOT NULL,
        [CartaoId] int NOT NULL,
        [AdicionadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_SprintCartoes] PRIMARY KEY ([SprintId], [CartaoId]),
        CONSTRAINT [FK_SprintCartoes_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SprintCartoes_Sprints_SprintId] FOREIGN KEY ([SprintId]) REFERENCES [dbo].[Sprints] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[VinculosGit] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [Tipo] int NOT NULL,
        [Repositorio] nvarchar(300) NOT NULL,
        [Identificador] nvarchar(300) NOT NULL,
        [Titulo] nvarchar(500) NOT NULL,
        [Url] nvarchar(1000) NOT NULL,
        [Autor] nvarchar(200) NULL,
        [Estado] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [AtualizadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_VinculosGit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VinculosGit_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[AcoesAutomacao] (
        [Id] int NOT NULL IDENTITY,
        [RegraAutomacaoId] int NOT NULL,
        [Tipo] int NOT NULL,
        [Ordem] int NOT NULL,
        [ParametrosJson] nvarchar(max) NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_AcoesAutomacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AcoesAutomacao_RegrasAutomacao_RegraAutomacaoId] FOREIGN KEY ([RegraAutomacaoId]) REFERENCES [dbo].[RegrasAutomacao] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[AlertasCartao] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [RegraAutomacaoId] int NULL,
        [Mensagem] nvarchar(500) NOT NULL,
        [Severidade] int NOT NULL,
        [Resolvido] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [ResolvidoEm] datetime2 NULL,
        [ResolvidoPorId] int NULL,
        CONSTRAINT [PK_AlertasCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AlertasCartao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AlertasCartao_RegrasAutomacao_RegraAutomacaoId] FOREIGN KEY ([RegraAutomacaoId]) REFERENCES [dbo].[RegrasAutomacao] ([Id])
    );

    CREATE TABLE [dbo].[ExecucoesAutomacao] (
        [Id] int NOT NULL IDENTITY,
        [RegraAutomacaoId] int NOT NULL,
        [QuadroId] int NOT NULL,
        [CartaoId] int NULL,
        [Gatilho] int NOT NULL,
        [ChaveDisparo] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [Resumo] nvarchar(2000) NOT NULL,
        [Erro] nvarchar(2000) NULL,
        [DesfazerJson] nvarchar(max) NULL,
        [EmailsEnviados] int NOT NULL,
        [MensagensTeams] int NOT NULL,
        [UsuarioId] int NOT NULL,
        [OcorridoEm] datetime2 NOT NULL,
        [DesfeitaEm] datetime2 NULL,
        [DesfeitaPorId] int NULL,
        CONSTRAINT [PK_ExecucoesAutomacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExecucoesAutomacao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]),
        CONSTRAINT [FK_ExecucoesAutomacao_RegrasAutomacao_RegraAutomacaoId] FOREIGN KEY ([RegraAutomacaoId]) REFERENCES [dbo].[RegrasAutomacao] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[SugestoesAutomacao] (
        [Id] int NOT NULL IDENTITY,
        [RegraAutomacaoId] int NOT NULL,
        [QuadroId] int NOT NULL,
        [CartaoId] int NOT NULL,
        [Descricao] nvarchar(1000) NOT NULL,
        [PropostaJson] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [DecididoEm] datetime2 NULL,
        [DecididoPorId] int NULL,
        CONSTRAINT [PK_SugestoesAutomacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SugestoesAutomacao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SugestoesAutomacao_RegrasAutomacao_RegraAutomacaoId] FOREIGN KEY ([RegraAutomacaoId]) REFERENCES [dbo].[RegrasAutomacao] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [dbo].[ValoresCampoCartao] (
        [Id] int NOT NULL IDENTITY,
        [CartaoId] int NOT NULL,
        [DefinicaoCampoId] int NOT NULL,
        [Valor] nvarchar(2000) NULL,
        [NumeroEntradaNaLista] int NOT NULL,
        [DataPreenchimento] datetime2 NOT NULL,
        [HistoricoOrigemId] int NULL,
        [CriadoAutomaticamente] bit NOT NULL,
        [ValorAnteriorAutomatico] nvarchar(2000) NULL,
        [PreenchidoPorId] int NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_ValoresCampoCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ValoresCampoCartao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ValoresCampoCartao_DefinicoesCampo_DefinicaoCampoId] FOREIGN KEY ([DefinicaoCampoId]) REFERENCES [dbo].[DefinicoesCampo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ValoresCampoCartao_HistoricoAtividades_HistoricoOrigemId] FOREIGN KEY ([HistoricoOrigemId]) REFERENCES [dbo].[HistoricoAtividades] ([Id]),
        CONSTRAINT [FK_ValoresCampoCartao_Usuarios_PreenchidoPorId] FOREIGN KEY ([PreenchidoPorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[Comentarios] (
        [Id] int NOT NULL IDENTITY,
        [Texto] nvarchar(max) NOT NULL,
        [AutorId] int NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [Editado] bit NOT NULL,
        [Publico] bit NOT NULL,
        [CartaoId] int NULL,
        [ItemTarefaId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Comentarios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comentarios_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Comentarios_ItensTarefa_ItemTarefaId] FOREIGN KEY ([ItemTarefaId]) REFERENCES [dbo].[ItensTarefa] ([Id]),
        CONSTRAINT [FK_Comentarios_Usuarios_AutorId] FOREIGN KEY ([AutorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[Reunioes] (
        [Id] int NOT NULL IDENTITY,
        [Data] datetime2 NOT NULL,
        [Ata] nvarchar(max) NOT NULL,
        [AutorId] int NOT NULL,
        [CartaoId] int NULL,
        [ItemTarefaId] int NULL,
        [CriadoEm] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [AlteradoEm] datetime2 NULL,
        [AlteradoPorId] int NULL,
        [Excluido] bit NOT NULL,
        [ExcluidoEm] datetime2 NULL,
        [ExcluidoPorId] int NULL,
        CONSTRAINT [PK_Reunioes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reunioes_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [dbo].[Cartoes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reunioes_ItensTarefa_ItemTarefaId] FOREIGN KEY ([ItemTarefaId]) REFERENCES [dbo].[ItensTarefa] ([Id]),
        CONSTRAINT [FK_Reunioes_Usuarios_AutorId] FOREIGN KEY ([AutorId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    CREATE TABLE [dbo].[ReuniaoParticipantes] (
        [ReuniaoId] int NOT NULL,
        [UsuarioId] int NOT NULL,
        CONSTRAINT [PK_ReuniaoParticipantes] PRIMARY KEY ([ReuniaoId], [UsuarioId]),
        CONSTRAINT [FK_ReuniaoParticipantes_Reunioes_ReuniaoId] FOREIGN KEY ([ReuniaoId]) REFERENCES [dbo].[Reunioes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReuniaoParticipantes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );

    SET IDENTITY_INSERT [dbo].[Quadros] ON;
    INSERT INTO [dbo].[Quadros] ([Id], [AlteradoEm], [AlteradoPorId], [Cor], [CriadoEm], [CriadoPorId], [Descricao], [Excluido], [ExcluidoEm], [ExcluidoPorId], [Nome], [TemplateQuadroId])
    VALUES (1, NULL, NULL, N'#1565C0', '2026-01-01T00:00:00.0000000Z', 1, N'Quadro principal do time', CAST(0 AS bit), NULL, NULL, N'Time de Desenvolvimento', NULL);
    SET IDENTITY_INSERT [dbo].[Quadros] OFF;

    SET IDENTITY_INSERT [dbo].[Sistemas] ON;
    INSERT INTO [dbo].[Sistemas] ([Id], [AlteradoEm], [AlteradoPorId], [Ativo], [CriadoEm], [CriadoPorId], [Excluido], [ExcluidoEm], [ExcluidoPorId], [Nome])
    VALUES (1, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Portal do Cliente'),
    (2, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'ERP Interno'),
    (3, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'App Mobile'),
    (4, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Integrações');
    SET IDENTITY_INSERT [dbo].[Sistemas] OFF;

    SET IDENTITY_INSERT [dbo].[Usuarios] ON;
    INSERT INTO [dbo].[Usuarios] ([Id], [AlteradoEm], [AlteradoPorId], [Ativo], [CriadoEm], [CriadoPorId], [Email], [Excluido], [ExcluidoEm], [ExcluidoPorId], [Nome], [NotificarPorEmail], [ReceberEmailsAutomacao])
    VALUES (1, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, N'admin@empresa.com', CAST(0 AS bit), NULL, NULL, N'Admin', CAST(0 AS bit), CAST(1 AS bit)),
    (2, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, N'ana.lima@empresa.com', CAST(0 AS bit), NULL, NULL, N'Ana Lima', CAST(1 AS bit), CAST(1 AS bit)),
    (3, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, N'bruno.costa@empresa.com', CAST(0 AS bit), NULL, NULL, N'Bruno Costa', CAST(1 AS bit), CAST(1 AS bit)),
    (4, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, N'carla.mendes@empresa.com', CAST(0 AS bit), NULL, NULL, N'Carla Mendes', CAST(0 AS bit), CAST(1 AS bit)),
    (5, NULL, NULL, CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 1, N'diego.santos@empresa.com', CAST(0 AS bit), NULL, NULL, N'Diego Santos', CAST(1 AS bit), CAST(1 AS bit));
    SET IDENTITY_INSERT [dbo].[Usuarios] OFF;

    SET IDENTITY_INSERT [dbo].[Etiquetas] ON;
    INSERT INTO [dbo].[Etiquetas] ([Id], [AlteradoEm], [AlteradoPorId], [Cor], [CriadoEm], [CriadoPorId], [Excluido], [ExcluidoEm], [ExcluidoPorId], [Nome], [QuadroId])
    VALUES (1, NULL, NULL, N'#E53935', '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Bug', 1),
    (2, NULL, NULL, N'#43A047', '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Feature', 1),
    (3, NULL, NULL, N'#1E88E5', '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Melhoria', 1),
    (4, NULL, NULL, N'#FB8C00', '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Urgente', 1),
    (5, NULL, NULL, N'#8E24AA', '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), NULL, NULL, N'Técnico', 1);
    SET IDENTITY_INSERT [dbo].[Etiquetas] OFF;

    SET IDENTITY_INSERT [dbo].[Listas] ON;
    INSERT INTO [dbo].[Listas] ([Id], [AlteradoEm], [AlteradoPorId], [BloquearEntradaComPendencias], [CamposFixosBloqueados], [CamposFixosExigidos], [CriadoEm], [CriadoPorId], [EhBacklog], [Excluido], [ExcluidoEm], [ExcluidoPorId], [LimiteWip], [ListaPaiId], [Nome], [Ordem], [OrdemFluxo], [QuadroId], [StatusPortal])
    VALUES (1, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, NULL, NULL, N'Backlog', 1, NULL, 1, NULL),
    (2, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 10, NULL, N'A Fazer', 2, NULL, 1, NULL),
    (3, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 5, NULL, N'Em Andamento', 3, NULL, 1, NULL),
    (4, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 3, NULL, N'Code Review', 4, NULL, 1, NULL),
    (5, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, NULL, NULL, N'Homologação', 5, NULL, 1, NULL),
    (6, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, NULL, NULL, N'Pré-produção', 6, NULL, 1, NULL),
    (7, NULL, NULL, CAST(0 AS bit), 0, 0, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, NULL, NULL, N'Concluído', 7, NULL, 1, NULL);
    SET IDENTITY_INSERT [dbo].[Listas] OFF;

    SET IDENTITY_INSERT [dbo].[DefinicoesCampo] ON;
    INSERT INTO [dbo].[DefinicoesCampo] ([Id], [AlteradoEm], [AlteradoPorId], [CriadoEm], [CriadoPorId], [DefineDataInicioCartao], [Excluido], [ExcluidoEm], [ExcluidoPorId], [ListaId], [Nome], [Obrigatorio], [OpcoesSelecao], [Ordem], [PedirNovamenteACadaEntrada], [Preenchimento], [RegraReentrada], [Tipo])
    VALUES (1, NULL, NULL, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 5, N'Data Homologado', CAST(1 AS bit), NULL, 1, CAST(1 AS bit), 0, 0, 3),
    (2, NULL, NULL, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 5, N'Homologado Por', CAST(0 AS bit), NULL, 2, CAST(0 AS bit), 0, 0, 1),
    (3, NULL, NULL, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 6, N'Data Prevista de Change', CAST(1 AS bit), NULL, 1, CAST(0 AS bit), 0, 0, 3),
    (4, NULL, NULL, '2026-01-01T00:00:00.0000000Z', 1, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 6, N'Número do Change', CAST(0 AS bit), NULL, 2, CAST(0 AS bit), 0, 0, 1);
    SET IDENTITY_INSERT [dbo].[DefinicoesCampo] OFF;

    CREATE INDEX [IX_AcoesAutomacao_RegraAutomacaoId] ON [dbo].[AcoesAutomacao] ([RegraAutomacaoId]);

    CREATE INDEX [IX_AlertasCartao_CartaoId_Resolvido] ON [dbo].[AlertasCartao] ([CartaoId], [Resolvido]);

    CREATE INDEX [IX_AlertasCartao_RegraAutomacaoId] ON [dbo].[AlertasCartao] ([RegraAutomacaoId]);

    CREATE INDEX [IX_Anexos_CartaoId] ON [dbo].[Anexos] ([CartaoId]);

    CREATE INDEX [IX_Anexos_EnviadoPorId] ON [dbo].[Anexos] ([EnviadoPorId]);

    CREATE INDEX [IX_CartaoDesenvolvedores_UsuarioId] ON [dbo].[CartaoDesenvolvedores] ([UsuarioId]);

    CREATE INDEX [IX_CartaoEtiquetas_EtiquetaId] ON [dbo].[CartaoEtiquetas] ([EtiquetaId]);

    CREATE INDEX [IX_Cartoes_CartaoPaiId] ON [dbo].[Cartoes] ([CartaoPaiId]);

    CREATE INDEX [IX_Cartoes_ListaId] ON [dbo].[Cartoes] ([ListaId]);

    CREATE INDEX [IX_Cartoes_SistemaId] ON [dbo].[Cartoes] ([SistemaId]);

    CREATE INDEX [IX_Cartoes_SolicitanteId] ON [dbo].[Cartoes] ([SolicitanteId]);

    CREATE INDEX [IX_Comentarios_AutorId] ON [dbo].[Comentarios] ([AutorId]);

    CREATE INDEX [IX_Comentarios_CartaoId] ON [dbo].[Comentarios] ([CartaoId]);

    CREATE INDEX [IX_Comentarios_ItemTarefaId] ON [dbo].[Comentarios] ([ItemTarefaId]);

    CREATE INDEX [IX_DefinicoesCampo_ListaId] ON [dbo].[DefinicoesCampo] ([ListaId]);

    CREATE INDEX [IX_EmailsCartao_CartaoId] ON [dbo].[EmailsCartao] ([CartaoId]);

    CREATE INDEX [IX_EmailsCartao_EnviadoPorId] ON [dbo].[EmailsCartao] ([EnviadoPorId]);

    CREATE INDEX [IX_Etiquetas_QuadroId] ON [dbo].[Etiquetas] ([QuadroId]);

    CREATE INDEX [IX_EventosAutomacaoPendentes_CartaoId] ON [dbo].[EventosAutomacaoPendentes] ([CartaoId]);

    CREATE INDEX [IX_EventosAutomacaoPendentes_ExecutarEm] ON [dbo].[EventosAutomacaoPendentes] ([ExecutarEm]);

    CREATE INDEX [IX_ExecucoesAutomacao_CartaoId] ON [dbo].[ExecucoesAutomacao] ([CartaoId]);

    CREATE INDEX [IX_ExecucoesAutomacao_QuadroId_OcorridoEm] ON [dbo].[ExecucoesAutomacao] ([QuadroId], [OcorridoEm]);

    CREATE INDEX [IX_ExecucoesAutomacao_RegraAutomacaoId_CartaoId_ChaveDisparo] ON [dbo].[ExecucoesAutomacao] ([RegraAutomacaoId], [CartaoId], [ChaveDisparo]);

    CREATE INDEX [IX_Feriados_Data] ON [dbo].[Feriados] ([Data]);

    CREATE INDEX [IX_HistoricoAtividades_CartaoId_OcorridoEm] ON [dbo].[HistoricoAtividades] ([CartaoId], [OcorridoEm]);

    CREATE INDEX [IX_HistoricoAtividades_ListaDestinoId_OcorridoEm] ON [dbo].[HistoricoAtividades] ([ListaDestinoId], [OcorridoEm]);

    CREATE INDEX [IX_HistoricoAtividades_ListaOrigemId] ON [dbo].[HistoricoAtividades] ([ListaOrigemId]);

    CREATE INDEX [IX_HistoricoAtividades_UsuarioId] ON [dbo].[HistoricoAtividades] ([UsuarioId]);

    CREATE INDEX [IX_ItensTarefa_CartaoId] ON [dbo].[ItensTarefa] ([CartaoId]);

    CREATE INDEX [IX_ItensTarefa_CartaoPromovidoId] ON [dbo].[ItensTarefa] ([CartaoPromovidoId]);

    CREATE INDEX [IX_ItensTarefa_DesenvolvedorId] ON [dbo].[ItensTarefa] ([DesenvolvedorId]);

    CREATE INDEX [IX_Listas_ListaPaiId] ON [dbo].[Listas] ([ListaPaiId]);

    CREATE INDEX [IX_Listas_QuadroId] ON [dbo].[Listas] ([QuadroId]);

    CREATE INDEX [IX_Notificacoes_CartaoOrigemId] ON [dbo].[Notificacoes] ([CartaoOrigemId]);

    CREATE INDEX [IX_Notificacoes_DestinatarioId_Lida] ON [dbo].[Notificacoes] ([DestinatarioId], [Lida]);

    CREATE INDEX [IX_RegrasAutomacao_ListaId] ON [dbo].[RegrasAutomacao] ([ListaId]);

    CREATE INDEX [IX_RegrasAutomacao_QuadroId_Gatilho] ON [dbo].[RegrasAutomacao] ([QuadroId], [Gatilho]);

    CREATE INDEX [IX_RelacoesCartao_CartaoDestinoId] ON [dbo].[RelacoesCartao] ([CartaoDestinoId]);

    CREATE INDEX [IX_RelacoesCartao_CartaoOrigemId] ON [dbo].[RelacoesCartao] ([CartaoOrigemId]);

    CREATE INDEX [IX_ReuniaoParticipantes_UsuarioId] ON [dbo].[ReuniaoParticipantes] ([UsuarioId]);

    CREATE INDEX [IX_Reunioes_AutorId] ON [dbo].[Reunioes] ([AutorId]);

    CREATE INDEX [IX_Reunioes_CartaoId] ON [dbo].[Reunioes] ([CartaoId]);

    CREATE INDEX [IX_Reunioes_ItemTarefaId] ON [dbo].[Reunioes] ([ItemTarefaId]);

    CREATE INDEX [IX_SprintCartoes_CartaoId] ON [dbo].[SprintCartoes] ([CartaoId]);

    CREATE INDEX [IX_Sprints_QuadroId] ON [dbo].[Sprints] ([QuadroId]);

    CREATE INDEX [IX_SugestoesAutomacao_CartaoId_Status] ON [dbo].[SugestoesAutomacao] ([CartaoId], [Status]);

    CREATE INDEX [IX_SugestoesAutomacao_QuadroId_Status] ON [dbo].[SugestoesAutomacao] ([QuadroId], [Status]);

    CREATE INDEX [IX_SugestoesAutomacao_RegraAutomacaoId] ON [dbo].[SugestoesAutomacao] ([RegraAutomacaoId]);

    CREATE INDEX [IX_TemplatesCartao_QuadroId] ON [dbo].[TemplatesCartao] ([QuadroId]);

    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [dbo].[Usuarios] ([Email]);

    CREATE INDEX [IX_ValoresCampoCartao_CartaoId_DefinicaoCampoId_NumeroEntradaNaLista] ON [dbo].[ValoresCampoCartao] ([CartaoId], [DefinicaoCampoId], [NumeroEntradaNaLista]);

    CREATE INDEX [IX_ValoresCampoCartao_DefinicaoCampoId] ON [dbo].[ValoresCampoCartao] ([DefinicaoCampoId]);

    CREATE INDEX [IX_ValoresCampoCartao_HistoricoOrigemId] ON [dbo].[ValoresCampoCartao] ([HistoricoOrigemId]);

    CREATE INDEX [IX_ValoresCampoCartao_PreenchidoPorId] ON [dbo].[ValoresCampoCartao] ([PreenchidoPorId]);

    CREATE UNIQUE INDEX [IX_VinculosGit_CartaoId_Tipo_Repositorio_Identificador] ON [dbo].[VinculosGit] ([CartaoId], [Tipo], [Repositorio], [Identificador]);

COMMIT TRANSACTION;
PRINT N'Banco KanbanDemandas criado.';
GO

-- Opcional: login e usuário da aplicação (o mesmo da connection string em appsettings.json).
-- Descomente, troque a senha e execute. Com autenticação do Windows ou login já existente,
-- use só o CREATE USER (ex.: CREATE USER [DOMINIO\usuario] FOR LOGIN [DOMINIO\usuario]).
--
-- CREATE LOGIN [kanban] WITH PASSWORD = N'troque-esta-senha', CHECK_POLICY = ON;
-- CREATE USER [kanban] FOR LOGIN [kanban] WITH DEFAULT_SCHEMA = [dbo];
-- ALTER ROLE [db_datareader] ADD MEMBER [kanban];
-- ALTER ROLE [db_datawriter] ADD MEMBER [kanban];

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Quadros` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Descricao` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Cor` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '#1565C0',
        `TemplateQuadroId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Quadros` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Sistemas` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Ativo` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Sistemas` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `TemplatesQuadro` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Descricao` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `EstruturaJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_TemplatesQuadro` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Usuarios` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Ativo` tinyint(1) NOT NULL,
        `NotificarPorEmail` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Usuarios` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Etiquetas` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Cor` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '#607D8B',
        `QuadroId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Etiquetas` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Etiquetas_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Listas` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Ordem` int NOT NULL,
        `QuadroId` int NOT NULL,
        `ListaPaiId` int NULL,
        `LimiteWip` int NULL,
        `EhBacklog` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Listas` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Listas_Listas_ListaPaiId` FOREIGN KEY (`ListaPaiId`) REFERENCES `Listas` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Listas_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Sprints` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Meta` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `DataInicio` datetime(6) NOT NULL,
        `DataFim` datetime(6) NOT NULL,
        `Ativa` tinyint(1) NOT NULL,
        `Fechada` tinyint(1) NOT NULL,
        `QuadroId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Sprints` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Sprints_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `TemplatesCartao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `TitulopadraO` varchar(500) CHARACTER SET utf8mb4 NULL,
        `DescricaoPadrao` longtext CHARACTER SET utf8mb4 NULL,
        `QuadroId` int NOT NULL,
        `ItensTarefaPadrao` longtext CHARACTER SET utf8mb4 NULL,
        `CamposPadrao` longtext CHARACTER SET utf8mb4 NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_TemplatesCartao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_TemplatesCartao_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Cartoes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Titulo` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Descricao` longtext CHARACTER SET utf8mb4 NULL,
        `ListaId` int NOT NULL,
        `Ordem` int NOT NULL,
        `DataInicio` datetime(6) NULL,
        `Prazo` datetime(6) NULL,
        `Estimativa` decimal(10,2) NULL,
        `Prioridade` int NULL,
        `SistemaId` int NULL,
        `SolicitanteId` int NULL,
        `CartaoPaiId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Cartoes` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Cartoes_Cartoes_CartaoPaiId` FOREIGN KEY (`CartaoPaiId`) REFERENCES `Cartoes` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Cartoes_Listas_ListaId` FOREIGN KEY (`ListaId`) REFERENCES `Listas` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Cartoes_Sistemas_SistemaId` FOREIGN KEY (`SistemaId`) REFERENCES `Sistemas` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Cartoes_Usuarios_SolicitanteId` FOREIGN KEY (`SolicitanteId`) REFERENCES `Usuarios` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `DefinicoesCampo` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Tipo` int NOT NULL,
        `ListaId` int NOT NULL,
        `Obrigatorio` tinyint(1) NOT NULL,
        `PedirNovamenteACadaEntrada` tinyint(1) NOT NULL,
        `OpcoesSelecao` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `Ordem` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_DefinicoesCampo` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_DefinicoesCampo_Listas_ListaId` FOREIGN KEY (`ListaId`) REFERENCES `Listas` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `RegrasAutomacao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Ativa` tinyint(1) NOT NULL,
        `ListaId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_RegrasAutomacao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RegrasAutomacao_Listas_ListaId` FOREIGN KEY (`ListaId`) REFERENCES `Listas` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Anexos` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `NomeOriginal` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `NomeArmazenado` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Caminho` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `ContentType` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `TamanhoBytes` bigint NOT NULL,
        `EnviadoPorId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Anexos` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Anexos_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Anexos_Usuarios_EnviadoPorId` FOREIGN KEY (`EnviadoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `CartaoDesenvolvedores` (
        `CartaoId` int NOT NULL,
        `UsuarioId` int NOT NULL,
        `Principal` tinyint(1) NOT NULL,
        CONSTRAINT `PK_CartaoDesenvolvedores` PRIMARY KEY (`CartaoId`, `UsuarioId`),
        CONSTRAINT `FK_CartaoDesenvolvedores_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_CartaoDesenvolvedores_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `CartaoEtiquetas` (
        `CartaoId` int NOT NULL,
        `EtiquetaId` int NOT NULL,
        CONSTRAINT `PK_CartaoEtiquetas` PRIMARY KEY (`CartaoId`, `EtiquetaId`),
        CONSTRAINT `FK_CartaoEtiquetas_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_CartaoEtiquetas_Etiquetas_EtiquetaId` FOREIGN KEY (`EtiquetaId`) REFERENCES `Etiquetas` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `EmailsCartao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `Para` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Cc` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Assunto` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `CorpoHtml` longtext CHARACTER SET utf8mb4 NOT NULL,
        `EnviadoEm` datetime(6) NOT NULL,
        `EnviadoPorId` int NOT NULL,
        `Enviado` tinyint(1) NOT NULL,
        `ErroEnvio` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_EmailsCartao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_EmailsCartao_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_EmailsCartao_Usuarios_EnviadoPorId` FOREIGN KEY (`EnviadoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `HistoricoAtividades` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `Tipo` int NOT NULL,
        `ListaOrigemId` int NULL,
        `ListaDestinoId` int NULL,
        `Descricao` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `ValorAnterior` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `ValorNovo` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `UsuarioId` int NOT NULL,
        `OcorridoEm` datetime(6) NOT NULL,
        CONSTRAINT `PK_HistoricoAtividades` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_HistoricoAtividades_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_HistoricoAtividades_Listas_ListaDestinoId` FOREIGN KEY (`ListaDestinoId`) REFERENCES `Listas` (`Id`),
        CONSTRAINT `FK_HistoricoAtividades_Listas_ListaOrigemId` FOREIGN KEY (`ListaOrigemId`) REFERENCES `Listas` (`Id`),
        CONSTRAINT `FK_HistoricoAtividades_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `ItensTarefa` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Titulo` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Concluido` tinyint(1) NOT NULL,
        `CartaoId` int NOT NULL,
        `Ordem` int NOT NULL,
        `Descricao` longtext CHARACTER SET utf8mb4 NULL,
        `DesenvolvedorId` int NULL,
        `CartaoPromovidoId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_ItensTarefa` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ItensTarefa_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ItensTarefa_Cartoes_CartaoPromovidoId` FOREIGN KEY (`CartaoPromovidoId`) REFERENCES `Cartoes` (`Id`),
        CONSTRAINT `FK_ItensTarefa_Usuarios_DesenvolvedorId` FOREIGN KEY (`DesenvolvedorId`) REFERENCES `Usuarios` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Notificacoes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `DestinatarioId` int NOT NULL,
        `Tipo` int NOT NULL,
        `Mensagem` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Lida` tinyint(1) NOT NULL,
        `LidaEm` datetime(6) NULL,
        `CartaoOrigemId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Notificacoes` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Notificacoes_Cartoes_CartaoOrigemId` FOREIGN KEY (`CartaoOrigemId`) REFERENCES `Cartoes` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Notificacoes_Usuarios_DestinatarioId` FOREIGN KEY (`DestinatarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `RelacoesCartao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoOrigemId` int NOT NULL,
        `CartaoDestinoId` int NOT NULL,
        `Tipo` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_RelacoesCartao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RelacoesCartao_Cartoes_CartaoDestinoId` FOREIGN KEY (`CartaoDestinoId`) REFERENCES `Cartoes` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_RelacoesCartao_Cartoes_CartaoOrigemId` FOREIGN KEY (`CartaoOrigemId`) REFERENCES `Cartoes` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `SprintCartoes` (
        `SprintId` int NOT NULL,
        `CartaoId` int NOT NULL,
        `AdicionadoEm` datetime(6) NOT NULL,
        CONSTRAINT `PK_SprintCartoes` PRIMARY KEY (`SprintId`, `CartaoId`),
        CONSTRAINT `FK_SprintCartoes_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_SprintCartoes_Sprints_SprintId` FOREIGN KEY (`SprintId`) REFERENCES `Sprints` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `ValoresCampoCartao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `DefinicaoCampoId` int NOT NULL,
        `Valor` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `NumeroEntradaNaLista` int NOT NULL,
        `DataPreenchimento` datetime(6) NOT NULL,
        `PreenchidoPorId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_ValoresCampoCartao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ValoresCampoCartao_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ValoresCampoCartao_DefinicoesCampo_DefinicaoCampoId` FOREIGN KEY (`DefinicaoCampoId`) REFERENCES `DefinicoesCampo` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_ValoresCampoCartao_Usuarios_PreenchidoPorId` FOREIGN KEY (`PreenchidoPorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `AcoesAutomacao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RegraAutomacaoId` int NOT NULL,
        `Tipo` int NOT NULL,
        `ListaDestinoId` int NULL,
        `DefinicaoCampoId` int NULL,
        `ValorCampo` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `SomenteSeVazio` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_AcoesAutomacao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId` FOREIGN KEY (`DefinicaoCampoId`) REFERENCES `DefinicoesCampo` (`Id`),
        CONSTRAINT `FK_AcoesAutomacao_Listas_ListaDestinoId` FOREIGN KEY (`ListaDestinoId`) REFERENCES `Listas` (`Id`),
        CONSTRAINT `FK_AcoesAutomacao_RegrasAutomacao_RegraAutomacaoId` FOREIGN KEY (`RegraAutomacaoId`) REFERENCES `RegrasAutomacao` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Comentarios` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Texto` longtext CHARACTER SET utf8mb4 NOT NULL,
        `AutorId` int NOT NULL,
        `DataHora` datetime(6) NOT NULL,
        `Editado` tinyint(1) NOT NULL,
        `CartaoId` int NULL,
        `ItemTarefaId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Comentarios` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Comentarios_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Comentarios_ItensTarefa_ItemTarefaId` FOREIGN KEY (`ItemTarefaId`) REFERENCES `ItensTarefa` (`Id`),
        CONSTRAINT `FK_Comentarios_Usuarios_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `Reunioes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Data` datetime(6) NOT NULL,
        `Ata` longtext CHARACTER SET utf8mb4 NOT NULL,
        `AutorId` int NOT NULL,
        `CartaoId` int NULL,
        `ItemTarefaId` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Reunioes` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Reunioes_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Reunioes_ItensTarefa_ItemTarefaId` FOREIGN KEY (`ItemTarefaId`) REFERENCES `ItensTarefa` (`Id`),
        CONSTRAINT `FK_Reunioes_Usuarios_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE TABLE `ReuniaoParticipantes` (
        `ReuniaoId` int NOT NULL,
        `UsuarioId` int NOT NULL,
        CONSTRAINT `PK_ReuniaoParticipantes` PRIMARY KEY (`ReuniaoId`, `UsuarioId`),
        CONSTRAINT `FK_ReuniaoParticipantes_Reunioes_ReuniaoId` FOREIGN KEY (`ReuniaoId`) REFERENCES `Reunioes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ReuniaoParticipantes_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `Quadros` (`Id`, `AlteradoEm`, `AlteradoPorId`, `Cor`, `CriadoEm`, `CriadoPorId`, `Descricao`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `Nome`, `TemplateQuadroId`)
    VALUES (1, NULL, NULL, '#1565C0', TIMESTAMP '2026-01-01 00:00:00', 1, 'Quadro principal do time', FALSE, NULL, NULL, 'Time de Desenvolvimento', NULL);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `Sistemas` (`Id`, `AlteradoEm`, `AlteradoPorId`, `Ativo`, `CriadoEm`, `CriadoPorId`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `Nome`)
    VALUES (1, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Portal do Cliente'),
    (2, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'ERP Interno'),
    (3, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'App Mobile'),
    (4, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Integrações');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `Usuarios` (`Id`, `AlteradoEm`, `AlteradoPorId`, `Ativo`, `CriadoEm`, `CriadoPorId`, `Email`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `Nome`, `NotificarPorEmail`)
    VALUES (1, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, 'admin@empresa.com', FALSE, NULL, NULL, 'Admin', FALSE),
    (2, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, 'ana.lima@empresa.com', FALSE, NULL, NULL, 'Ana Lima', TRUE),
    (3, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, 'bruno.costa@empresa.com', FALSE, NULL, NULL, 'Bruno Costa', TRUE),
    (4, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, 'carla.mendes@empresa.com', FALSE, NULL, NULL, 'Carla Mendes', FALSE),
    (5, NULL, NULL, TRUE, TIMESTAMP '2026-01-01 00:00:00', 1, 'diego.santos@empresa.com', FALSE, NULL, NULL, 'Diego Santos', TRUE);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `Etiquetas` (`Id`, `AlteradoEm`, `AlteradoPorId`, `Cor`, `CriadoEm`, `CriadoPorId`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `Nome`, `QuadroId`)
    VALUES (1, NULL, NULL, '#E53935', TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Bug', 1),
    (2, NULL, NULL, '#43A047', TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Feature', 1),
    (3, NULL, NULL, '#1E88E5', TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Melhoria', 1),
    (4, NULL, NULL, '#FB8C00', TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Urgente', 1),
    (5, NULL, NULL, '#8E24AA', TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 'Técnico', 1);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `Listas` (`Id`, `AlteradoEm`, `AlteradoPorId`, `CriadoEm`, `CriadoPorId`, `EhBacklog`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `LimiteWip`, `ListaPaiId`, `Nome`, `Ordem`, `QuadroId`)
    VALUES (1, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, TRUE, FALSE, NULL, NULL, NULL, NULL, 'Backlog', 1, 1),
    (2, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, 10, NULL, 'A Fazer', 2, 1),
    (3, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, 5, NULL, 'Em Andamento', 3, 1),
    (4, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, 3, NULL, 'Code Review', 4, 1),
    (5, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, NULL, NULL, 'Homologação', 5, 1),
    (6, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, NULL, NULL, 'Pré-produção', 6, 1),
    (7, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, FALSE, NULL, NULL, NULL, NULL, 'Concluído', 7, 1);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `DefinicoesCampo` (`Id`, `AlteradoEm`, `AlteradoPorId`, `CriadoEm`, `CriadoPorId`, `Excluido`, `ExcluidoEm`, `ExcluidoPorId`, `ListaId`, `Nome`, `Obrigatorio`, `OpcoesSelecao`, `Ordem`, `PedirNovamenteACadaEntrada`, `Tipo`)
    VALUES (1, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 5, 'Data Homologado', TRUE, NULL, 1, TRUE, 3),
    (2, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 5, 'Homologado Por', FALSE, NULL, 2, FALSE, 1),
    (3, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 6, 'Data Prevista de Change', TRUE, NULL, 1, FALSE, 3),
    (4, NULL, NULL, TIMESTAMP '2026-01-01 00:00:00', 1, FALSE, NULL, NULL, 6, 'Número do Change', FALSE, NULL, 2, FALSE, 1);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_AcoesAutomacao_DefinicaoCampoId` ON `AcoesAutomacao` (`DefinicaoCampoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_AcoesAutomacao_ListaDestinoId` ON `AcoesAutomacao` (`ListaDestinoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_AcoesAutomacao_RegraAutomacaoId` ON `AcoesAutomacao` (`RegraAutomacaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Anexos_CartaoId` ON `Anexos` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Anexos_EnviadoPorId` ON `Anexos` (`EnviadoPorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_CartaoDesenvolvedores_UsuarioId` ON `CartaoDesenvolvedores` (`UsuarioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_CartaoEtiquetas_EtiquetaId` ON `CartaoEtiquetas` (`EtiquetaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Cartoes_CartaoPaiId` ON `Cartoes` (`CartaoPaiId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Cartoes_ListaId` ON `Cartoes` (`ListaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Cartoes_SistemaId` ON `Cartoes` (`SistemaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Cartoes_SolicitanteId` ON `Cartoes` (`SolicitanteId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Comentarios_AutorId` ON `Comentarios` (`AutorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Comentarios_CartaoId` ON `Comentarios` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Comentarios_ItemTarefaId` ON `Comentarios` (`ItemTarefaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_DefinicoesCampo_ListaId` ON `DefinicoesCampo` (`ListaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_EmailsCartao_CartaoId` ON `EmailsCartao` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_EmailsCartao_EnviadoPorId` ON `EmailsCartao` (`EnviadoPorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Etiquetas_QuadroId` ON `Etiquetas` (`QuadroId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_HistoricoAtividades_CartaoId_OcorridoEm` ON `HistoricoAtividades` (`CartaoId`, `OcorridoEm`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_HistoricoAtividades_ListaDestinoId_OcorridoEm` ON `HistoricoAtividades` (`ListaDestinoId`, `OcorridoEm`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_HistoricoAtividades_ListaOrigemId` ON `HistoricoAtividades` (`ListaOrigemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_HistoricoAtividades_UsuarioId` ON `HistoricoAtividades` (`UsuarioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ItensTarefa_CartaoId` ON `ItensTarefa` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ItensTarefa_CartaoPromovidoId` ON `ItensTarefa` (`CartaoPromovidoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ItensTarefa_DesenvolvedorId` ON `ItensTarefa` (`DesenvolvedorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Listas_ListaPaiId` ON `Listas` (`ListaPaiId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Listas_QuadroId` ON `Listas` (`QuadroId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Notificacoes_CartaoOrigemId` ON `Notificacoes` (`CartaoOrigemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Notificacoes_DestinatarioId_Lida` ON `Notificacoes` (`DestinatarioId`, `Lida`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_RegrasAutomacao_ListaId` ON `RegrasAutomacao` (`ListaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_RelacoesCartao_CartaoDestinoId` ON `RelacoesCartao` (`CartaoDestinoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_RelacoesCartao_CartaoOrigemId` ON `RelacoesCartao` (`CartaoOrigemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ReuniaoParticipantes_UsuarioId` ON `ReuniaoParticipantes` (`UsuarioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Reunioes_AutorId` ON `Reunioes` (`AutorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Reunioes_CartaoId` ON `Reunioes` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Reunioes_ItemTarefaId` ON `Reunioes` (`ItemTarefaId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_SprintCartoes_CartaoId` ON `SprintCartoes` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_Sprints_QuadroId` ON `Sprints` (`QuadroId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_TemplatesCartao_QuadroId` ON `TemplatesCartao` (`QuadroId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Usuarios_Email` ON `Usuarios` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ValoresCampoCartao_CartaoId_DefinicaoCampoId_NumeroEntradaNa~` ON `ValoresCampoCartao` (`CartaoId`, `DefinicaoCampoId`, `NumeroEntradaNaLista`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ValoresCampoCartao_DefinicaoCampoId` ON `ValoresCampoCartao` (`DefinicaoCampoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    CREATE INDEX `IX_ValoresCampoCartao_PreenchidoPorId` ON `ValoresCampoCartao` (`PreenchidoPorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260924032807_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260924032807_InitialCreate', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    ALTER TABLE `Listas` ADD `OrdemFluxo` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 5;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 6;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    UPDATE `Listas` SET `OrdemFluxo` = NULL
    WHERE `Id` = 7;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927153352_CaminhoListas') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260927153352_CaminhoListas', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `ValoresCampoCartao` ADD `CriadoAutomaticamente` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `ValoresCampoCartao` ADD `HistoricoOrigemId` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `ValoresCampoCartao` ADD `ValorAnteriorAutomatico` varchar(2000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `Listas` ADD `BloquearEntradaComPendencias` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `Listas` ADD `CamposFixosExigidos` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `HistoricoAtividades` ADD `AlterouDataInicio` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `HistoricoAtividades` ADD `DataInicioAnterior` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `HistoricoAtividades` ADD `Estornado` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `DefinicoesCampo` ADD `DefineDataInicioCartao` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `DefinicoesCampo` ADD `Preenchimento` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `DefinicoesCampo` ADD `RegraReentrada` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `DefinicoesCampo` SET `DefineDataInicioCartao` = FALSE, `Preenchimento` = 0, `RegraReentrada` = 0
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `DefinicoesCampo` SET `DefineDataInicioCartao` = FALSE, `Preenchimento` = 0, `RegraReentrada` = 0
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `DefinicoesCampo` SET `DefineDataInicioCartao` = FALSE, `Preenchimento` = 0, `RegraReentrada` = 0
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `DefinicoesCampo` SET `DefineDataInicioCartao` = FALSE, `Preenchimento` = 0, `RegraReentrada` = 0
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 5;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 6;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    UPDATE `Listas` SET `BloquearEntradaComPendencias` = FALSE, `CamposFixosExigidos` = 0
    WHERE `Id` = 7;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    CREATE INDEX `IX_ValoresCampoCartao_HistoricoOrigemId` ON `ValoresCampoCartao` (`HistoricoOrigemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    ALTER TABLE `ValoresCampoCartao` ADD CONSTRAINT `FK_ValoresCampoCartao_HistoricoAtividades_HistoricoOrigemId` FOREIGN KEY (`HistoricoOrigemId`) REFERENCES `HistoricoAtividades` (`Id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260927223851_RegrasEtapaEEstorno') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260927223851_RegrasEtapaEEstorno', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` DROP FOREIGN KEY `FK_RegrasAutomacao_Listas_ListaId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `Usuarios` ADD `ReceberEmailsAutomacao` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` MODIFY COLUMN `ListaId` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `CondicoesJson` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `Descricao` varchar(1000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `ExigirTodasCondicoes` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `Gatilho` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `ParametrosGatilhoJson` varchar(4000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `QuadroId` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `SomenteHorarioComercial` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD `UltimaExecucaoEm` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` ADD `Ordem` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` ADD `ParametrosJson` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN


    UPDATE `RegrasAutomacao` r JOIN `Listas` l ON l.`Id` = r.`ListaId`
    SET r.`QuadroId` = l.`QuadroId`, r.`Gatilho` = 2, r.`ExigirTodasCondicoes` = TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN


    UPDATE `AcoesAutomacao` a
    LEFT JOIN `DefinicoesCampo` d ON d.`Id` = a.`DefinicaoCampoId`
    SET a.`Ordem` = a.`Id`,
        a.`ParametrosJson` = CASE a.`Tipo`
            WHEN 1 THEN JSON_OBJECT('ListaId', a.`ListaDestinoId`)
            WHEN 2 THEN JSON_OBJECT('Destinatarios', 'Desenvolvedores')
            WHEN 3 THEN JSON_OBJECT('CampoNome', d.`Nome`, 'Valor', a.`ValorCampo`,
                                    'SomenteSeVazio', IF(a.`SomenteSeVazio`, CAST('true' AS JSON), CAST('false' AS JSON)))
            ELSE NULL END;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN


    DELETE a FROM `AcoesAutomacao` a JOIN `RegrasAutomacao` r ON r.`Id` = a.`RegraAutomacaoId` WHERE r.`QuadroId` = 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    DELETE FROM `RegrasAutomacao` WHERE `QuadroId` = 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP FOREIGN KEY `FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP FOREIGN KEY `FK_AcoesAutomacao_Listas_ListaDestinoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP INDEX `IX_AcoesAutomacao_DefinicaoCampoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP INDEX `IX_AcoesAutomacao_ListaDestinoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP COLUMN `DefinicaoCampoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP COLUMN `ListaDestinoId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP COLUMN `SomenteSeVazio`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `AcoesAutomacao` DROP COLUMN `ValorCampo`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE TABLE `AlertasCartao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `RegraAutomacaoId` int NULL,
        `Mensagem` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Severidade` int NOT NULL,
        `Resolvido` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `ResolvidoEm` datetime(6) NULL,
        `ResolvidoPorId` int NULL,
        CONSTRAINT `PK_AlertasCartao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AlertasCartao_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_AlertasCartao_RegrasAutomacao_RegraAutomacaoId` FOREIGN KEY (`RegraAutomacaoId`) REFERENCES `RegrasAutomacao` (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE TABLE `ExecucoesAutomacao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RegraAutomacaoId` int NOT NULL,
        `QuadroId` int NOT NULL,
        `CartaoId` int NULL,
        `Gatilho` int NOT NULL,
        `ChaveDisparo` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Status` int NOT NULL,
        `Resumo` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
        `Erro` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `DesfazerJson` longtext CHARACTER SET utf8mb4 NULL,
        `EmailsEnviados` int NOT NULL,
        `UsuarioId` int NOT NULL,
        `OcorridoEm` datetime(6) NOT NULL,
        `DesfeitaEm` datetime(6) NULL,
        `DesfeitaPorId` int NULL,
        CONSTRAINT `PK_ExecucoesAutomacao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ExecucoesAutomacao_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`),
        CONSTRAINT `FK_ExecucoesAutomacao_RegrasAutomacao_RegraAutomacaoId` FOREIGN KEY (`RegraAutomacaoId`) REFERENCES `RegrasAutomacao` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE TABLE `SugestoesAutomacao` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RegraAutomacaoId` int NOT NULL,
        `QuadroId` int NOT NULL,
        `CartaoId` int NOT NULL,
        `Descricao` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `PropostaJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Status` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `DecididoEm` datetime(6) NULL,
        `DecididoPorId` int NULL,
        CONSTRAINT `PK_SugestoesAutomacao` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SugestoesAutomacao_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_SugestoesAutomacao_RegrasAutomacao_RegraAutomacaoId` FOREIGN KEY (`RegraAutomacaoId`) REFERENCES `RegrasAutomacao` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE
    WHERE `Id` = 5;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_RegrasAutomacao_QuadroId_Gatilho` ON `RegrasAutomacao` (`QuadroId`, `Gatilho`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_AlertasCartao_CartaoId_Resolvido` ON `AlertasCartao` (`CartaoId`, `Resolvido`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_AlertasCartao_RegraAutomacaoId` ON `AlertasCartao` (`RegraAutomacaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_ExecucoesAutomacao_CartaoId` ON `ExecucoesAutomacao` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_ExecucoesAutomacao_QuadroId_OcorridoEm` ON `ExecucoesAutomacao` (`QuadroId`, `OcorridoEm`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_ExecucoesAutomacao_RegraAutomacaoId_CartaoId_ChaveDisparo` ON `ExecucoesAutomacao` (`RegraAutomacaoId`, `CartaoId`, `ChaveDisparo`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_SugestoesAutomacao_CartaoId_Status` ON `SugestoesAutomacao` (`CartaoId`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_SugestoesAutomacao_QuadroId_Status` ON `SugestoesAutomacao` (`QuadroId`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    CREATE INDEX `IX_SugestoesAutomacao_RegraAutomacaoId` ON `SugestoesAutomacao` (`RegraAutomacaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD CONSTRAINT `FK_RegrasAutomacao_Listas_ListaId` FOREIGN KEY (`ListaId`) REFERENCES `Listas` (`Id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    ALTER TABLE `RegrasAutomacao` ADD CONSTRAINT `FK_RegrasAutomacao_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928002604_MotorAutomacoes') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260928002604_MotorAutomacoes', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    ALTER TABLE `Anexos` ADD `Comprimido` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    ALTER TABLE `Anexos` ADD `TamanhoArmazenadoBytes` bigint NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    UPDATE `Anexos` SET `TamanhoArmazenadoBytes` = `TamanhoBytes`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE TABLE `EventosAutomacaoPendentes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `Gatilho` int NOT NULL,
        `ListaId` int NULL,
        `Campo` int NOT NULL,
        `UsuarioAlvoId` int NULL,
        `UsuarioId` int NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `ExecutarEm` datetime(6) NOT NULL,
        CONSTRAINT `PK_EventosAutomacaoPendentes` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE TABLE `Feriados` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Data` date NOT NULL,
        `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Tipo` int NOT NULL,
        `RecorrenteAnual` tinyint(1) NOT NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `CriadoPorId` int NOT NULL,
        `AlteradoEm` datetime(6) NULL,
        `AlteradoPorId` int NULL,
        `Excluido` tinyint(1) NOT NULL,
        `ExcluidoEm` datetime(6) NULL,
        `ExcluidoPorId` int NULL,
        CONSTRAINT `PK_Feriados` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE TABLE `ParametrosSistema` (
        `Chave` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Valor` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `AlteradoEm` datetime(6) NOT NULL,
        `AlteradoPorId` int NOT NULL,
        CONSTRAINT `PK_ParametrosSistema` PRIMARY KEY (`Chave`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE INDEX `IX_EventosAutomacaoPendentes_CartaoId` ON `EventosAutomacaoPendentes` (`CartaoId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE INDEX `IX_EventosAutomacaoPendentes_ExecutarEm` ON `EventosAutomacaoPendentes` (`ExecutarEm`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    CREATE INDEX `IX_Feriados_Data` ON `Feriados` (`Data`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928010150_ParametrizacaoFeriadosAnexos') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260928010150_ParametrizacaoFeriadosAnexos', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    ALTER TABLE `Listas` ADD `StatusPortal` varchar(100) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    ALTER TABLE `ExecucoesAutomacao` ADD `MensagensTeams` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    ALTER TABLE `Comentarios` ADD `Publico` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    ALTER TABLE `Cartoes` ADD `OrigemPortal` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    ALTER TABLE `Cartoes` ADD `TipoSolicitacao` varchar(100) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    CREATE TABLE `VinculosGit` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CartaoId` int NOT NULL,
        `Tipo` int NOT NULL,
        `Repositorio` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Identificador` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Titulo` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Url` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Autor` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Estado` int NULL,
        `CriadoEm` datetime(6) NOT NULL,
        `AtualizadoEm` datetime(6) NOT NULL,
        CONSTRAINT `PK_VinculosGit` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_VinculosGit_Cartoes_CartaoId` FOREIGN KEY (`CartaoId`) REFERENCES `Cartoes` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 5;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 6;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    UPDATE `Listas` SET `StatusPortal` = NULL
    WHERE `Id` = 7;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    CREATE UNIQUE INDEX `IX_VinculosGit_CartaoId_Tipo_Repositorio_Identificador` ON `VinculosGit` (`CartaoId`, `Tipo`, `Repositorio`, `Identificador`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928050555_PortalGitTeams') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260928050555_PortalGitTeams', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    ALTER TABLE `Listas` ADD `CamposFixosBloqueados` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 1;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 2;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 3;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 4;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 5;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 6;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    UPDATE `Listas` SET `CamposFixosBloqueados` = 0
    WHERE `Id` = 7;
    SELECT ROW_COUNT();


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260928052928_CamposBloqueadosEtapa') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260928052928_CamposBloqueadosEtapa', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;


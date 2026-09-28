-- Migration MotorAutomacoes: motor de automações (gatilhos, condições, ações, execuções, sugestões e alertas).
-- Converte as regras existentes para o novo formato. Rode depois de 2026-09-27_RegrasEtapaEEstorno.sql.
START TRANSACTION;
ALTER TABLE `RegrasAutomacao` DROP FOREIGN KEY `FK_RegrasAutomacao_Listas_ListaId`;

ALTER TABLE `Usuarios` ADD `ReceberEmailsAutomacao` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `RegrasAutomacao` MODIFY COLUMN `ListaId` int NULL;

ALTER TABLE `RegrasAutomacao` ADD `CondicoesJson` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `RegrasAutomacao` ADD `Descricao` varchar(1000) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `RegrasAutomacao` ADD `ExigirTodasCondicoes` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `RegrasAutomacao` ADD `Gatilho` int NOT NULL DEFAULT 0;

ALTER TABLE `RegrasAutomacao` ADD `ParametrosGatilhoJson` varchar(4000) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `RegrasAutomacao` ADD `QuadroId` int NOT NULL DEFAULT 0;

ALTER TABLE `RegrasAutomacao` ADD `SomenteHorarioComercial` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `RegrasAutomacao` ADD `UltimaExecucaoEm` datetime(6) NULL;

ALTER TABLE `AcoesAutomacao` ADD `Ordem` int NOT NULL DEFAULT 0;

ALTER TABLE `AcoesAutomacao` ADD `ParametrosJson` longtext CHARACTER SET utf8mb4 NULL;

UPDATE `RegrasAutomacao` r JOIN `Listas` l ON l.`Id` = r.`ListaId`
SET r.`QuadroId` = l.`QuadroId`, r.`Gatilho` = 2, r.`ExigirTodasCondicoes` = TRUE;

UPDATE `AcoesAutomacao` a
LEFT JOIN `DefinicoesCampo` d ON d.`Id` = a.`DefinicaoCampoId`
SET a.`Ordem` = a.`Id`,
    a.`ParametrosJson` = CASE a.`Tipo`
        WHEN 1 THEN JSON_OBJECT('ListaId', a.`ListaDestinoId`)
        WHEN 2 THEN JSON_OBJECT('Destinatarios', 'Desenvolvedores')
        WHEN 3 THEN JSON_OBJECT('CampoNome', d.`Nome`, 'Valor', a.`ValorCampo`,
                                'SomenteSeVazio', IF(a.`SomenteSeVazio`, CAST('true' AS JSON), CAST('false' AS JSON)))
        ELSE NULL END;

DELETE a FROM `AcoesAutomacao` a JOIN `RegrasAutomacao` r ON r.`Id` = a.`RegraAutomacaoId` WHERE r.`QuadroId` = 0;

DELETE FROM `RegrasAutomacao` WHERE `QuadroId` = 0;

UPDATE `Usuarios` SET `ReceberEmailsAutomacao` = TRUE;

ALTER TABLE `AcoesAutomacao` DROP FOREIGN KEY `FK_AcoesAutomacao_DefinicoesCampo_DefinicaoCampoId`;

ALTER TABLE `AcoesAutomacao` DROP FOREIGN KEY `FK_AcoesAutomacao_Listas_ListaDestinoId`;

ALTER TABLE `AcoesAutomacao` DROP INDEX `IX_AcoesAutomacao_DefinicaoCampoId`;

ALTER TABLE `AcoesAutomacao` DROP INDEX `IX_AcoesAutomacao_ListaDestinoId`;

ALTER TABLE `AcoesAutomacao` DROP COLUMN `DefinicaoCampoId`;

ALTER TABLE `AcoesAutomacao` DROP COLUMN `ListaDestinoId`;

ALTER TABLE `AcoesAutomacao` DROP COLUMN `SomenteSeVazio`;

ALTER TABLE `AcoesAutomacao` DROP COLUMN `ValorCampo`;

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

CREATE INDEX `IX_RegrasAutomacao_QuadroId_Gatilho` ON `RegrasAutomacao` (`QuadroId`, `Gatilho`);

CREATE INDEX `IX_AlertasCartao_CartaoId_Resolvido` ON `AlertasCartao` (`CartaoId`, `Resolvido`);

CREATE INDEX `IX_AlertasCartao_RegraAutomacaoId` ON `AlertasCartao` (`RegraAutomacaoId`);

CREATE INDEX `IX_ExecucoesAutomacao_CartaoId` ON `ExecucoesAutomacao` (`CartaoId`);

CREATE INDEX `IX_ExecucoesAutomacao_QuadroId_OcorridoEm` ON `ExecucoesAutomacao` (`QuadroId`, `OcorridoEm`);

CREATE INDEX `IX_ExecucoesAutomacao_RegraAutomacaoId_CartaoId_ChaveDisparo` ON `ExecucoesAutomacao` (`RegraAutomacaoId`, `CartaoId`, `ChaveDisparo`);

CREATE INDEX `IX_SugestoesAutomacao_CartaoId_Status` ON `SugestoesAutomacao` (`CartaoId`, `Status`);

CREATE INDEX `IX_SugestoesAutomacao_QuadroId_Status` ON `SugestoesAutomacao` (`QuadroId`, `Status`);

CREATE INDEX `IX_SugestoesAutomacao_RegraAutomacaoId` ON `SugestoesAutomacao` (`RegraAutomacaoId`);

ALTER TABLE `RegrasAutomacao` ADD CONSTRAINT `FK_RegrasAutomacao_Listas_ListaId` FOREIGN KEY (`ListaId`) REFERENCES `Listas` (`Id`);

ALTER TABLE `RegrasAutomacao` ADD CONSTRAINT `FK_RegrasAutomacao_Quadros_QuadroId` FOREIGN KEY (`QuadroId`) REFERENCES `Quadros` (`Id`) ON DELETE CASCADE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260928002604_MotorAutomacoes', '9.0.5');

COMMIT;


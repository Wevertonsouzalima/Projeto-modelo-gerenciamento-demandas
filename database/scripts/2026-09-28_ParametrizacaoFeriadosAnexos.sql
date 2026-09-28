-- Migration ParametrizacaoFeriadosAnexos: parametrização pela tela, fila de espera das automações,
-- cadastro de feriados e compactação de anexos. Rode depois de 2026-09-28_MotorAutomacoes.sql.
START TRANSACTION;
ALTER TABLE `Anexos` ADD `Comprimido` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Anexos` ADD `TamanhoArmazenadoBytes` bigint NOT NULL DEFAULT 0;

UPDATE `Anexos` SET `TamanhoArmazenadoBytes` = `TamanhoBytes`;

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

CREATE TABLE `ParametrosSistema` (
    `Chave` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Valor` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `AlteradoEm` datetime(6) NOT NULL,
    `AlteradoPorId` int NOT NULL,
    CONSTRAINT `PK_ParametrosSistema` PRIMARY KEY (`Chave`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_EventosAutomacaoPendentes_CartaoId` ON `EventosAutomacaoPendentes` (`CartaoId`);

CREATE INDEX `IX_EventosAutomacaoPendentes_ExecutarEm` ON `EventosAutomacaoPendentes` (`ExecutarEm`);

CREATE INDEX `IX_Feriados_Data` ON `Feriados` (`Data`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260928010150_ParametrizacaoFeriadosAnexos', '9.0.5');

COMMIT;


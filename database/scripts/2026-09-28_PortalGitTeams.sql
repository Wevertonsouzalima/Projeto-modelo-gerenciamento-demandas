-- Migration PortalGitTeams: portal de solicitações (status público por lista, origem/tipo da solicitação,
-- comentários públicos), vínculos com GitHub e contagem de mensagens do Teams.
-- Rode depois de 2026-09-28_ParametrizacaoFeriadosAnexos.sql.
START TRANSACTION;
ALTER TABLE `Listas` ADD `StatusPortal` varchar(100) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `ExecucoesAutomacao` ADD `MensagensTeams` int NOT NULL DEFAULT 0;

ALTER TABLE `Comentarios` ADD `Publico` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Cartoes` ADD `OrigemPortal` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Cartoes` ADD `TipoSolicitacao` varchar(100) CHARACTER SET utf8mb4 NULL;

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

CREATE UNIQUE INDEX `IX_VinculosGit_CartaoId_Tipo_Repositorio_Identificador` ON `VinculosGit` (`CartaoId`, `Tipo`, `Repositorio`, `Identificador`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260928050555_PortalGitTeams', '9.0.5');

COMMIT;


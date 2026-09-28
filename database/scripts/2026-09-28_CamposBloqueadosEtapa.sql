-- Migration CamposBloqueadosEtapa: campos que uma lista não permite (ex.: desenvolvedor e prazo no Backlog).
-- Rode depois de 2026-09-28_PortalGitTeams.sql.
-- Valores (somáveis): 1 Desenvolvedor, 2 Prazo, 4 Estimativa, 8 Sistema, 16 Solicitante, 32 Data de início.
START TRANSACTION;
ALTER TABLE `Listas` ADD `CamposFixosBloqueados` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260928052928_CamposBloqueadosEtapa', '9.0.5');

COMMIT;

-- Opcional: Backlog sem desenvolvedor, prazo e data de início (1 + 2 + 32 = 35).
-- Também pode ser feito pela tela (engrenagem da coluna > "Não permitido nesta etapa").
-- UPDATE `Listas` SET `CamposFixosBloqueados` = 35 WHERE `EhBacklog` = 1;

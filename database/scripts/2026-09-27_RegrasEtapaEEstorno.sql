-- Migration RegrasEtapaEEstorno: exigências por etapa, campos automáticos e estorno de vai-e-volta.
-- Só é necessário rodar manualmente se a aplicação não aplicar as migrations na inicialização.
START TRANSACTION;

ALTER TABLE `ValoresCampoCartao` ADD `CriadoAutomaticamente` tinyint(1) NOT NULL DEFAULT FALSE;
ALTER TABLE `ValoresCampoCartao` ADD `HistoricoOrigemId` int NULL;
ALTER TABLE `ValoresCampoCartao` ADD `ValorAnteriorAutomatico` varchar(2000) CHARACTER SET utf8mb4 NULL;
ALTER TABLE `Listas` ADD `BloquearEntradaComPendencias` tinyint(1) NOT NULL DEFAULT FALSE;
ALTER TABLE `Listas` ADD `CamposFixosExigidos` int NOT NULL DEFAULT 0;
ALTER TABLE `HistoricoAtividades` ADD `AlterouDataInicio` tinyint(1) NOT NULL DEFAULT FALSE;
ALTER TABLE `HistoricoAtividades` ADD `DataInicioAnterior` datetime(6) NULL;
ALTER TABLE `HistoricoAtividades` ADD `Estornado` tinyint(1) NOT NULL DEFAULT FALSE;
ALTER TABLE `DefinicoesCampo` ADD `DefineDataInicioCartao` tinyint(1) NOT NULL DEFAULT FALSE;
ALTER TABLE `DefinicoesCampo` ADD `Preenchimento` int NOT NULL DEFAULT 0;
ALTER TABLE `DefinicoesCampo` ADD `RegraReentrada` int NOT NULL DEFAULT 0;

CREATE INDEX `IX_ValoresCampoCartao_HistoricoOrigemId` ON `ValoresCampoCartao` (`HistoricoOrigemId`);
ALTER TABLE `ValoresCampoCartao` ADD CONSTRAINT `FK_ValoresCampoCartao_HistoricoAtividades_HistoricoOrigemId`
    FOREIGN KEY (`HistoricoOrigemId`) REFERENCES `HistoricoAtividades` (`Id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260927223851_RegrasEtapaEEstorno', '9.0.5');

COMMIT;

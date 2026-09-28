# Banco MySQL

O projeto usa MySQL 8.0 ou superior com o provider `Pomelo.EntityFrameworkCore.MySql`.

## Configuracao

A connection string fica em `src/KanbanDemandas.Web/appsettings.json`:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=KanbanDemandas;User=kanban;Password=change-me;"
```

Altere usuario e senha para o ambiente real. Nao versione credenciais reais.

## Criacao do banco

Com um usuario administrador do MySQL:

```sql
CREATE DATABASE KanbanDemandas CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER 'kanban'@'localhost' IDENTIFIED BY 'uma-senha-segura';
GRANT ALL PRIVILEGES ON KanbanDemandas.* TO 'kanban'@'localhost';
FLUSH PRIVILEGES;
```

Depois, atualize a connection string e execute um dos scripts:

- `scripts/KanbanDemandas.MySql.sql`: cria o schema completo para banco vazio.
- `scripts/KanbanDemandas.MySql.idempotent.sql`: aplica apenas migrations ainda nao registradas em `__EFMigrationsHistory`.

Exemplo:

```bash
mysql -u kanban -p -h localhost -P 3306 KanbanDemandas < database/scripts/KanbanDemandas.MySql.idempotent.sql
```

Tambem e possivel aplicar via EF Core:

```bash
dotnet ef database update --project src/KanbanDemandas.Infrastructure --startup-project src/KanbanDemandas.Web --framework net9.0
```

A aplicacao NAO aplica migrations na inicializacao: rode os scripts acima (ou `dotnet ef database update`) antes de publicar uma versao nova.

Scripts incrementais (na ordem), para bancos que ja existiam:

1. `scripts/2026-09-27_RegrasEtapaEEstorno.sql` — exigencias por etapa, campos automaticos, estorno de vai-e-volta.
2. `scripts/2026-09-28_MotorAutomacoes.sql` — motor de automacoes (converte as regras antigas para o novo formato).
3. `scripts/2026-09-28_ParametrizacaoFeriadosAnexos.sql` — parametrizacao pela tela, espera das automacoes, feriados e compactacao de anexos.
4. `scripts/2026-09-28_PortalGitTeams.sql` — portal de solicitacoes (status publico por lista, comentarios publicos), vinculos com GitHub e mensagens do Teams.
5. `scripts/2026-09-28_CamposBloqueadosEtapa.sql` — campos nao permitidos por etapa (ex.: Backlog sem desenvolvedor).

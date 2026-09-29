# Banco SQL Server

O projeto usa SQL Server 2016 ou superior com o provider `Microsoft.EntityFrameworkCore.SqlServer`.
Não há migrations: o banco é criado por um único script.

## Criação do banco

Execute `KanbanDemandas.SqlServer.sql` com um usuário que possa criar bancos:

- SSMS: abra o arquivo e execute (F5).
- Linha de comando: `sqlcmd -S <servidor> -E -b -i database/KanbanDemandas.SqlServer.sql`

O script cria o banco `KanbanDemandas`, todas as tabelas (schema `dbo`), chaves e índices, e os dados
iniciais (usuários, sistemas, quadro de exemplo). Tudo roda numa transação: se algo falhar, nada fica
criado pela metade e o script pode ser executado de novo. Se as tabelas já existirem, ele para sem alterar nada.

Configurações do banco aplicadas ao criá-lo:

- Collation `Latin1_General_100_CI_AI`: comparações e buscas ignoram maiúsculas e acentos.
- `READ_COMMITTED_SNAPSHOT ON`: leituras não esperam gravações em andamento (sem isso, telas abertas
  ao mesmo tempo podem se bloquear).

Se o banco for criado pelo DBA, o script usa o banco existente (vazio) e pula essa parte: peça essas
duas configurações na criação.

No fim do script há um bloco comentado para criar o login e o usuário da aplicação (`kanban`), com
leitura e escrita. Descomente e troque a senha se precisar.

## Configuração

A connection string fica em `src/KanbanDemandas.Web/appsettings.json` e `src/KanbanDemandas.Portal/appsettings.json`:

```json
"DefaultConnection": "Server=localhost;Database=KanbanDemandas;User Id=kanban;Password=change-me;TrustServerCertificate=True;"
```

Com autenticação do Windows: `Server=localhost;Database=KanbanDemandas;Trusted_Connection=True;TrustServerCertificate=True;`.
Não versione credenciais reais: em desenvolvimento use user-secrets (os dois projetos compartilham o `UserSecretsId`).

## Alterações no modelo

Ao mudar entidades ou configurações do EF, gere de novo o schema a partir do modelo:

```bash
dotnet ef dbcontext script --project src/KanbanDemandas.Infrastructure --startup-project src/KanbanDemandas.Infrastructure -o modelo.sql
```

Use a saída para atualizar `KanbanDemandas.SqlServer.sql` (bancos novos), mantendo o formato do arquivo:
um lote só dentro da transação (sem `GO`), nomes com `[dbo].`, defaults com nome (`DF_Tabela_Coluna`) e
`SET IDENTITY_INSERT` direto. Para os bancos que já existem, escreva à mão o `ALTER TABLE` correspondente.

# Sistema de Gestão de Demandas — guia de recriação (com acesso ao código original)

> **Para quem é este documento:** uma IA de desenvolvimento (ex.: Claude Code) que vai recriar o sistema **tendo este repositório em mãos**.
> O código atual é a **referência funcional**: o que ele faz é o que a nova versão precisa fazer. Este guia diz o que muda (SQL Server + BBICore), o que deve ser mantido igual, onde está cada coisa e quais armadilhas já foram resolvidas.
> Se existir também o arquivo `Recriar-sem-acesso-ao-codigo.md`, use-o como checklist de funcionalidades e critérios de aceite.

---

## 0. Objetivo e regras do trabalho

Recriar o sistema com **as mesmas funcionalidades e conceitos**, mudando apenas:

| Tema | Hoje (este repositório) | Nova versão |
|---|---|---|
| Banco | **SQL Server** via `Microsoft.EntityFrameworkCore.SqlServer` (já trocado; sem migrations, schema por script) | Mantém |
| Usuário atual | `FakeUsuarioAtualProvider` + `SessaoUsuario` (seletor provisório no topo) | **Fluxo de login completo da BBICore** (sistema principal e portal) |
| UI | MudBlazor 8.9 | **Componentes da BBICore** onde existirem; MudBlazor (ou o que já estiver no padrão da BBICore) para o resto |
| Permissões | Nenhuma | Perfis mínimos (seção 3.4) |

Regras:
1. **Não reescreva regra de negócio "de cabeça".** Porte a lógica dos arquivos indicados na seção 5, mantendo nomes, comportamentos e testes. Mudanças de comportamento só se exigidas por SQL Server/BBICore — e registradas em `docs/DECISOES.md`.
2. Pode reaproveitar os arquivos que não dependem de MySQL nem do usuário fake **quase sem alteração** (Core inteiro, serviços, motor de automações, relatórios, testes).
3. Build com **0 erros e 0 avisos** e **todos os testes passando** ao fim de cada etapa.
4. Não aplique migrations na inicialização; gere scripts SQL (seção 4.3).

---

## 1. Leitura obrigatória antes de começar

Nesta ordem:

1. `Sistema de Gestão de Demandas — Prompt para Claude Code.md` — especificação original da v1 (parte dela foi superada; ver item 2).
2. `Status de Implementação — Plano Original.md` — tudo o que foi implementado além da especificação, rodada a rodada (regras por etapa, estorno, motor de automações, parametrização, feriados, relatórios, portal, GitHub, Teams, campos proibidos, ajustes de uso real). **Onde divergir do item 1, vale o item 2.**
3. `docs/portal.md`, `docs/integracoes/github.md`, `docs/integracoes/teams.md`.
4. `database/README.md`.
5. Os testes em `tests/KanbanDemandas.Tests` — descrevem os comportamentos esperados de forma executável.

---

## 2. Mapa do repositório atual

```
src/KanbanDemandas.Core/Dominio.cs                   entidades, enums, interfaces, regras puras
src/KanbanDemandas.Infrastructure/Infraestrutura.cs  DbContext, configurações EF, interceptors, seed, serviços compartilhados
src/KanbanDemandas.Web/                              sistema principal (Blazor Server + MudBlazor); C# fora das telas em Servicos.cs
src/KanbanDemandas.Portal/                           portal de solicitações (Blazor Server + MudBlazor); C# fora das telas em Servicos.cs
tests/KanbanDemandas.Tests/Testes.cs                 xUnit + EF InMemory (89 testes)
database/KanbanDemandas.SqlServer.sql                criação completa do banco (SQL Server)
docs/                                                guias
```

O código C# foi consolidado em poucos arquivos por projeto (facilita copiar o projeto sem `git clone`). Os namespaces não mudaram, e cada arquivo antigo virou um `#region` com o caminho original: na tabela abaixo, `Core/Regras/ListasQuadro.cs` é o `#region Regras/ListasQuadro.cs` de `Core/Dominio.cs`, `Web/Services/...` fica em `Web/Servicos.cs`, e assim por diante. Telas (`.razor`) continuam um arquivo por componente.

### 2.1 Onde está cada conceito

| Conceito | Arquivos |
|---|---|
| Entidades e enums | `Core/Entities/*`, `Core/Enums/*` (`Automacao.cs` concentra gatilhos, condições, operadores, destinatários etc.) |
| Listas agrupadoras, primeira folha, nome completo, "Concluído" | `Core/Regras/ListasQuadro.cs` |
| Caminho do quadro (aviso ao pular/voltar, trava de reordenação) | `Core/Regras/CaminhoQuadro.cs`, `Web/Components/Shared/DialogCaminhoQuadro.razor` |
| Exigências/proibições/pendências da etapa | `Core/Regras/RegrasEtapa.cs` (`FixosFaltando`, `BloqueadosPreenchidos`, `PendenciasEtapa`, `DadosEtapa`) |
| Valor mais recente de campo | `Core/Regras/CamposCartao.cs` |
| Código `KB-123` | `Core/Regras/CodigoCartao.cs` |
| Status público do portal, flags do que o solicitante vê | `Core/Regras/StatusPortal.cs` |
| **Movimentação única** (estorno, campos automáticos, remoção de proibidos, item promovido, pendências) | `Infrastructure/Services/MovimentacaoCartaoService.cs` |
| Fluxo de interface da movimentação (confirmações, diálogo da etapa, toasts) | `Web/Services/AssistenteMovimentacao.cs`, `Web/Components/Shared/DialogEntradaEtapa.razor` |
| Gravação de campos/dados da etapa | `Web/Services/ValoresCampoService.cs` |
| Auditoria automática | `Infrastructure/Data/AuditoriaInterceptor.cs` |
| Tempo real | `Infrastructure/Data/AlteracoesTempoRealInterceptor.cs`, `Web/Services/PublicadorAlteracoesTempoReal.cs`, `Web/Hubs/QuadroHub.cs` |
| Geração de eventos de automação | `Infrastructure/Data/EventosAutomacaoInterceptor.cs` (contexto `AsyncLocal` de profundidade/cadeia) |
| Espera das automações (fila no banco, debounce por cartão) | `Infrastructure/Services/AgendaEventosAutomacao.cs`, `Web/Services/Automacoes/ProcessamentoAutomacoes.cs` |
| Motor de automações | `Web/Services/Automacoes/MotorAutomacoes.cs` + `AvaliadorCondicoes`, `FotoQuadro`, `ConflitosAgenda`, `DiasUteis`, `MarcadoresAutomacao`, `ModelosAutomacao` (JSON de gatilho/condição/ação/desfazer), `DescricaoAutomacao` (frases e ajuda), `ModelosProntosAutomacao` |
| Teams | `Web/Services/Automacoes/MensagemTeams.cs` |
| GitHub | `Web/Services/Integracoes/GitHubWebhookService.cs` + endpoint em `Web/Program.cs` |
| Parametrização | `Infrastructure/Configuracao/ParametrosBancoConfiguration.cs` (provider do banco), `Web/Configuracao/CatalogoParametros.cs` (catálogo), `Web/Configuracao/ParametrosService.cs`, `Web/Components/Pages/Admin/Parametrizacao.razor` |
| Feriados | `Web/Services/FeriadosService.cs` (BrasilAPI + cálculo local), `Admin/Feriados.razor` |
| Anexos (nome, colisão, gzip) | `Infrastructure/Services/LocalDiskAnexoStorage.cs`, `Web/Services/CompactacaoAnexosService.cs`, endpoint `/api/anexos/{id}` |
| Exportação Excel/PDF | `Infrastructure/Services/ExportacaoService.cs` (ClosedXML, QuestPDF), endpoints `/api/export/*` |
| Relatórios (CFD, vazão, ciclo, envelhecimento, Monte Carlo, prazos, devs) | `Web/Services/Relatorios/RelatoriosService.cs`, `Pages/Relatorios.razor` |
| Board | `Pages/QuadroBoard.razor` (+ helper JS de drag-and-drop em `Web/Components/App.razor`) |
| Detalhes do cartão | `Shared/DialogDetalhesCartao.razor` |
| Criação/edição do cartão | `Shared/DialogNovoCartao.razor` |
| Configuração da coluna (campos, exigidos, proibidos, bloqueio) | `Shared/DialogCamposLista.razor`, `Shared/CampoListaEditor.razor` |
| Editor de regras | `Shared/DialogRegraAutomacao.razor`, `Shared/DialogSimulacaoAutomacao.razor`, `Pages/Admin/Automacoes.razor` |
| Multisseleção com filtro | `Shared/MultiSelecaoFiltro.razor` |
| Aviso ao solicitante (e-mail) | `Web/Services/Portal/NotificadorSolicitante.cs` |
| Portal | `Portal/Services/PortalService.cs`, `Portal/Services/PontesSistemaPrincipal.cs` (fila → banco, aviso ao hub do principal), `Portal/Components/Pages/*` |
| Usuário atual (a substituir) | `Core/Interfaces/IUsuarioAtualProvider.cs`, `Infrastructure/Services/FakeUsuarioAtualProvider.cs` (`SessaoUsuario`), seletores em `Web/Components/Layout/MainLayout.razor` e `Portal/Components/Layout/MainLayout.razor` |
| Seed | `Infrastructure/Data/SeedData.cs` |
| Registro de DI | `Infrastructure/InfrastructureServiceExtensions.cs`, `Web/Program.cs`, `Portal/Program.cs` |

---

## 3. BBICore (fazer primeiro)

Este guia não conhece a API da BBICore. **Levante antes de alterar telas.**

### 3.1 Levantamento

1. Liste namespaces, componentes Razor, serviços e extensões de DI/pipeline da DLL (`AddBBICore…`, `UseBBICore…`, `MapBBICore…`), além de XML de documentação/README se existirem.
2. Descubra: como ligar o **login** (rotas, middleware, cookie/token, logout); como obter o **usuário autenticado** (claims/serviço/`AuthenticationStateProvider`); se há **perfis/permissões**; quais **componentes de UI** existem (layout/menu, tabela, formulário, diálogo, seleção, upload, toast); se há serviço de **e-mail/log**.
3. Documente em `docs/BBICore.md` o mapeamento "necessidade → componente BBICore" e o que continuará com MudBlazor.
4. Se algo essencial for ambíguo, **pergunte ao usuário** em vez de adivinhar.

### 3.2 Substituir o usuário fake

- Todo o código já depende só de `IUsuarioAtualProvider` (`ObterUsuarioAtual()`, `ObterIdUsuarioAtual()`). **Mantenha essa interface.**
- Crie `UsuarioAtualBBICore : IUsuarioAtualProvider` e troque o registro em `InfrastructureServiceExtensions` (hoje `AddScoped<IUsuarioAtualProvider, FakeUsuarioAtualProvider>()` e `AddScoped<SessaoUsuario>()`).
- Remova `SessaoUsuario` e os seletores de usuário dos dois `MainLayout.razor`. Procure todos os usos: `grep -rn "SessaoUsuario\|Sessao\." src`. Em especial:
  - `AuditoriaInterceptor` e `EventosAutomacaoInterceptor` recebem `SessaoUsuario` hoje — passe a usar `IUsuarioAtualProvider` (com fallback para `Automacao:UsuarioSistemaId` fora de requisição/circuito).
  - Portal: `MinhasSolicitacoes`, `NovaSolicitacao`, `VerSolicitacao` leem `Sessao.UsuarioId` e assinam `Sessao.OnChange` — troque pelo id do usuário autenticado e remova as assinaturas.
- Tabela `Usuarios` continua (FKs, atribuição, menções). Adicione `LoginExterno` (único, filtrado quando não nulo) para vincular ao usuário BBICore; crie o usuário local no primeiro login e atualize nome/e-mail nos seguintes.
- Serviços em segundo plano (agendador, fila, webhook) continuam usando `Automacao:UsuarioSistemaId`.
- `Program.cs` do Web registra um aviso em Production sobre "sem autenticação real" — remova.

### 3.3 Endpoints e hub

- `/api/anexos/{id}` e `/api/export/*`: exigir autenticação; o de anexo deve também conferir se o usuário pode ver o cartão.
- `/api/integracoes/github`: continua **anônimo** (assinatura HMAC) e sem antiforgery.
- Hub `/hubs/quadro`: exige usuário; o método `AvisarAlteracao` é chamado pelo **portal** (`PontesSistemaPrincipal.PublicadorViaSistemaPrincipal`). Defina como o portal se autentica no hub (token de serviço, chave compartilhada em header ou o mecanismo que a BBICore oferecer) e registre em `docs/DECISOES.md`.

### 3.4 Perfis mínimos

| Perfil | Acesso |
|---|---|
| Administrador | Tudo, incluindo `/admin/*` (parametrização, usuários, sistemas, feriados, templates), automações e configuração de colunas |
| Time | Quadros, cartões, sprints, dashboard, relatórios, busca, notificações, automações do quadro |
| Solicitante | Só o portal |

Use o modelo da BBICore se houver. **Confirme com o usuário** antes de fechar essa etapa, sem bloquear o resto.

---

## 4. Migração MySQL → SQL Server

> **Já feita neste repositório** (28/09): provider trocado, migrations removidas, schema em `database/KanbanDemandas.SqlServer.sql` validado no LocalDB. As configurações de FK já não tinham cascata múltipla. Os itens abaixo ficam como checklist para o que a BBICore mudar no modelo (ex.: `LoginExterno`).

### 4.1 Pacotes e configuração

- Remova `Pomelo.EntityFrameworkCore.MySql`; adicione `Microsoft.EntityFrameworkCore.SqlServer` 9.x (mesma versão do EF usado).
- `InfrastructureServiceExtensions`: `UseMySql(... MySqlServerVersion ...)` → `UseSqlServer(connectionString, sql => sql.MigrationsAssembly(...))`. Mantenha os três interceptors (auditoria, tempo real, eventos de automação) e o `DbContext` **transient** (ou migre para `IDbContextFactory`, mantendo "um contexto por operação" — ver armadilha 2 na seção 7).
- `KanbanDbContextFactory` (design-time) → SQL Server.
- `ParametrosBancoConfiguration.cs` monta um `DbContextOptionsBuilder` próprio com `UseMySql(...)` para ler `ParametrosSistema` — troque por `UseSqlServer(...)`.
- Os projetos Portal e Tests referenciam `Microsoft.EntityFrameworkCore.Relational` explicitamente para alinhar versão; ajuste para a versão do provider SQL Server.

### 4.2 Configurações EF a revisar (`Infrastructure/Data/Configurations`)

- **Cascatas múltiplas** são proibidas no SQL Server. Revise cada `HasOne/WithMany` e use `DeleteBehavior.Restrict`/`NoAction` em: auto-FKs (`Lista.ListaPaiId`, `Cartao.CartaoPaiId`), `RelacaoCartao` (origem/destino), `HistoricoAtividade` (lista origem/destino), FKs para `Usuario`, `Comentario`/`Reuniao` (cartão **ou** item), `ItemTarefa.CartaoPromovidoId`, `ValorCampoCartao.HistoricoOrigemId`. A exclusão é lógica, então cascata física praticamente não é usada.
- Remova anotações/tipos específicos de MySQL (`utf8mb4`, `longtext`, `tinyint(1)` implícitos). Use `nvarchar(n)`, `nvarchar(max)`, `datetime2`, `decimal(10,2)` (estimativa), `bit`.
- Índice único de `VinculoGit` (CartaoId, Tipo, Repositorio(300), Identificador(300)) cabe no limite de 1700 bytes do SQL Server; mantenha os tamanhos.
- Mantenha todos os `HasQueryFilter` (`!Excluido`, e `!Estornado` em `HistoricoAtividade`).
- `ParametroSistema` tem PK `Chave` (string) — mantenha.

### 4.3 Migrations e scripts

- Não há migrations: o schema vem do modelo via `dotnet ef dbcontext script` e fica em `database/KanbanDemandas.SqlServer.sql` (ver `database/README.md`). Ao mudar o modelo (ex.: `LoginExterno`), regenere o script e escreva o `ALTER TABLE` para bancos existentes.
- A aplicação não cria nem altera o banco na inicialização — manter assim.
- Seed (`SeedData.cs`): revise ids fixos e `HasData`; inclua o usuário de sistema e o quadro de exemplo. Configure o Backlog do exemplo com `CamposFixosBloqueados = Desenvolvedor|Prazo|DataInicio` (35).
- Connection string: user-secrets em dev (os dois projetos compartilham o `UserSecretsId`), variável de ambiente/cofre em produção; appsettings só com placeholder.

### 4.4 Testes

- Os testes usam EF InMemory e não dependem de MySQL — devem continuar passando sem alteração, exceto os que tocam `SessaoUsuario` (ex.: `new EventosAutomacaoInterceptor(new SessaoUsuario { UsuarioId = 1 }, fila)` em `MotorAutomacoesTests`) — adapte para o novo provider (crie um fake de `IUsuarioAtualProvider`).
- Adicione testes para: sincronização de usuário no primeiro login, provider BBICore com fallback para usuário de sistema, portal filtrando pelo usuário autenticado.
- Opcional recomendado: alguns testes de integração com SQL Server (LocalDB ou container) para validar as configurações de FK/cascata e o script gerado.

---

## 5. O que portar sem mudar o comportamento

Porte a lógica destes pontos **fielmente** (são os que mais têm regras sutis; os testes existentes cobrem boa parte):

1. **Movimentação** (`MovimentacaoCartaoService.MoverAsync`): primeira folha; reordenação; estorno do vai-e-volta (último movimento invertido, mesmo usuário, dentro de `Movimentacao:JanelaCorrecaoMinutos`, desfaz valores automáticos e Data de início); campos automáticos com regra de reentrada; remoção de campos proibidos com registro no histórico (exceto solicitante de cartão do portal); sincronização do item promovido; aviso de caminho.
2. **Assistente de movimentação**: confirmação de campos proibidos → diálogo de pendências (antes se a lista bloqueia; depois se só avisa) → toasts.
3. **Motor de automações**: eventos pelo interceptor; espera no banco com debounce por cartão (`Automacao:AtrasoMinutos`); eventos de automação imediatos; **revalidação na execução**; anti-loop (profundidade + regra não repete na cadeia); chaves de disparo de tempo; limites de e-mail/Teams por hora; desfazer; simular; sugestões; modelos prontos; ações que falham em lista que proíbe o campo.
4. **Portal**: status público (listas sem status mantêm o anterior), visibilidade configurável, mensagens/anexos bloqueados em concluída **no serviço**, e-mail ao solicitante só quando a mudança de status foi causada pela entrada atual.
5. **Parametrização** sobrepondo appsettings em tempo real nas duas aplicações.
6. **GitHub** (assinatura em tempo constante, upsert sem duplicar, gatilhos) e **Teams** (Adaptive Card, https, segredo mascarado, limite, "Testar envio").
7. **Relatórios** (percentis, Monte Carlo 10.000 cenários, "Concluído" por nome de lista, início = primeira entrada fora de backlog/conclusão).

---

## 6. Trocando MudBlazor por componentes da BBICore

- Faça a troca **por tela**, mantendo o comportamento. Se a BBICore não tiver um equivalente, mantenha MudBlazor naquele ponto e registre.
- Pontos que precisam de atenção ao trocar:
  - **Selects** precisam exibir o nome do item selecionado (no MudBlazor isso exige `ToStringFunc`; confira no componente novo).
  - **Multisseleção com filtro** (`MultiSelecaoFiltro.razor`): chips + digitação na mesma caixa, lista aberta enquanto marca, limpa o texto ao selecionar, teclado. Só substitua se a BBICore tiver algo equivalente.
  - **Drag-and-drop** do board usa o helper JS em `App.razor` (define `dataTransfer` de forma síncrona). Não dependa de eventos de arraste do Blazor.
  - **Diálogos em tela cheia** (detalhes do cartão, editor de regras) e diálogos de confirmação de exclusão.
  - **Tooltips em todos os botões de ação.**
  - **Paleta dos relatórios** (validada para daltonismo): `#2a78d6,#eb6834,#1baf7a,#eda100,#e87ba4,#008300,#4a3aa7,#e34948`, com tabela ao lado de cada gráfico.
- Layout: menu lateral do sistema principal (Quadros, Sprints, Dashboard, Relatórios, Busca, Notificações, Admin) e app bar do portal (Minhas solicitações, Nova solicitação) passam a usar o layout da BBICore, com o usuário logado e logout.

---

## 7. Armadilhas já resolvidas (não reintroduza)

1. Drag-and-drop sem helper JS → cursor de bloqueio.
2. **Concorrência do DbContext** no Blazor Server (recarregar o quadro durante gravação) → contexto transient/por operação; board carrega com contexto próprio e `AsSplitQuery()`; recargas com debounce.
3. Selects mostrando ids.
4. **Duplicação ao adicionar** tarefa/comentário/anexo/reunião/e-mail numa coleção de entidade rastreada (o EF já inclui a entidade nova na navegação) → helper `IncluirUmaVez` em `DialogDetalhesCartao.razor`.
5. **Closure em `@for`** gerando itens de seleção (todos ficavam com o último valor; ex.: ano em Feriados) → `foreach`.
6. **Padrões `<`/`>` no início de linha dentro de `@code`** (ex.: `switch` com `< 1024 =>`) → o editor Razor do Visual Studio acusa dezenas de erros, embora a CLI compile. Lógica assim vai para `.cs` (ex.: `Portal/Services/Formatacao.cs`).
7. `MudGrid` vazio com margem negativa cobrindo alerta acima (Relatórios › Previsão) → só renderizar a grade com conteúdo.
8. Colunas flex sem `min-width:0` estourando a largura (detalhes do cartão).
9. Cor "secundária" do tema do portal é azul → textos auxiliares usam classe de texto secundário (cinza).
10. Migrations não são automáticas — sempre gerar e documentar scripts.
11. Senha de banco já vazou em `appsettings.json` na versão antiga — nunca commitar credenciais.

---

## 8. Ordem sugerida

1. Levantamento BBICore + `docs/BBICore.md`.
2. Troca de provider para SQL Server, revisão das configurações EF, `InitialCreate`, scripts, seed. Testes passando.
3. Login BBICore no sistema principal (provider, sincronização de usuário, remoção do seletor, endpoints, hub).
4. Login BBICore no portal (idem, portal filtrando pelo usuário autenticado; autenticação do portal no hub).
5. Perfis e autorização de telas/endpoints.
6. Troca de componentes para BBICore, tela a tela, conferindo cada fluxo no navegador.
7. Revisão final: segurança (segredos, endpoints), documentação (`README.md`, `database/README.md`, `docs/*`), atualização de `Status de Implementação — Plano Original.md` com esta migração.

---

## 9. Critérios de aceite

- Build **0 erros / 0 avisos**; todos os testes (os 89 atuais adaptados + os novos) passando.
- Scripts SQL Server completo e idempotente em `database/scripts`, com `database/README.md` atualizado.
- Login BBICore nas duas aplicações; nenhum resquício de `SessaoUsuario`/seletor de usuário (`grep -rn "SessaoUsuario" src` vazio).
- Portal mostra apenas as solicitações do usuário autenticado; tentar abrir `/solicitacoes/{id}` de outra pessoa mostra "não encontrada".
- Fluxos verificados no navegador contra SQL Server real:
  - arrastar cartões/listas; cartão nunca fica em lista agrupadora; caminho avisa;
  - Backlog → A Fazer (pede desenvolvedor, preenche "Atribuído em") → Em Andamento (preenche início) → voltar dentro da janela desfaz;
  - mover para Backlog com desenvolvedor/prazo pede confirmação e remove;
  - regra "entrou em X" executa após a espera e não executa se o cartão saiu antes; desfazer execução funciona;
  - solicitação aberta no portal aparece no quadro em tempo real; comentário público aparece no portal e gera e-mail;
  - webhook do GitHub vincula e dispara regra; mensagem chega ao canal do Teams;
  - Parametrização altera comportamento sem reiniciar nas duas aplicações.
- `docs/BBICore.md` e `docs/DECISOES.md` criados.

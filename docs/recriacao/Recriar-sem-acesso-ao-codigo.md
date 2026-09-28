# Sistema de Gestão de Demandas — especificação para recriação (sem acesso ao código original)

> **Para quem é este documento:** uma IA de desenvolvimento (ex.: Claude Code) que vai construir o sistema do zero, **sem** acesso ao código da versão original.
> Tudo o que precisa ser replicado está descrito aqui: conceitos, modelo de dados, regras de negócio, telas, automações, integrações e armadilhas já conhecidas.
> Onde este documento não detalhar, tome a decisão mais simples compatível com o restante e registre-a no README.

---

## 0. Como usar este documento

1. Leia o documento inteiro antes de escrever código. As regras das seções 5 a 9 se cruzam (movimentação ↔ campos ↔ automações ↔ portal).
2. Faça primeiro a **seção 2 (BBICore)**: a autenticação e os componentes da BBICore definem como várias telas serão montadas.
3. Siga a ordem de construção da **seção 17**. Cada etapa termina com build sem erros/avisos e testes passando.
4. Os critérios de aceite da **seção 18** são o "pronto" do projeto.
5. Registre decisões e desvios em `docs/DECISOES.md` (o que decidiu, por quê, alternativa descartada).

### Diferenças obrigatórias em relação à versão original

| Tema | Versão original | Esta versão |
|---|---|---|
| Banco | MySQL 8 (Pomelo) | **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Login | Não havia; seletor provisório de usuário | **Fluxo de login completo da BBICore** no sistema principal e no portal |
| Componentes de UI | MudBlazor | **Componentes da BBICore** sempre que existirem; MudBlazor (ou equivalente) só para o que a BBICore não cobrir |
| Permissões | Não havia | Perfis mínimos (seção 2.4), usando o modelo da BBICore se houver |

Todo o resto (funcionalidades, regras, conceitos) deve ser **idêntico**.

---

## 1. Visão geral

Sistema web interno de gestão de demandas de um time de desenvolvimento (15+ pessoas), no estilo Trello/Jira, substituindo o Microsoft Planner. É composto por **duas aplicações** que compartilham o mesmo banco:

1. **Sistema principal** — quadros Kanban, cartões, campos configuráveis por coluna, regras por etapa, sprints, automações, notificações, relatórios, parametrização e integrações (GitHub e Teams).
2. **Portal de solicitações** — aplicação separada onde quem pede demandas abre solicitações e acompanha o andamento, sem ver o quadro.

Diferenciais que **não podem ser simplificados**:
- **Motor de campos configuráveis por coluna** (cada lista define seus campos; valores podem ser pedidos de novo a cada entrada).
- **Regras por etapa**: campos exigidos, campos proibidos, preenchimento automático, bloqueio de entrada.
- **Proteção contra vai-e-volta**: movimentos desfeitos logo em seguida não sujam histórico nem métricas.
- **Motor de automações** Quando → Se → Então, com espera configurável, anti-loop, desfazer, simulação e sugestões.

### Stack

- **.NET 9**, **Blazor Server** (render mode InteractiveServer).
- **EF Core 9 + SQL Server**, soft-delete e auditoria automáticos.
- **SignalR** (hub próprio) para tempo real por quadro.
- **BBICore** (DLL interna) para login e componentes.
- Excel: ClosedXML. PDF: QuestPDF. E-mail: MailKit (ou o serviço de e-mail da BBICore, se existir).
- Testes: xUnit + EF Core InMemory (ou SQLite in-memory se precisar de comportamento relacional).

### Estrutura da solução

```
KanbanDemandas.slnx
src/
  KanbanDemandas.Core/            entidades, enums, interfaces e regras puras (sem EF, sem ASP.NET)
  KanbanDemandas.Infrastructure/  DbContext, configurações EF, interceptors, migrations, serviços de dados compartilhados
  KanbanDemandas.Web/             sistema principal (Blazor Server)
  KanbanDemandas.Portal/          portal de solicitações (Blazor Server)
tests/
  KanbanDemandas.Tests/
database/scripts/                 scripts SQL gerados das migrations
docs/                             guias (integrações, portal, decisões)
```

Regra de dependência: `Core` não depende de ninguém; `Infrastructure` depende de `Core`; `Web` e `Portal` dependem de ambos. **Regras de negócio que o portal também usa ficam em `Core/Regras` ou `Infrastructure/Services`**, nunca dentro do `Web`.

---

## 2. BBICore (fazer primeiro)

A BBICore é uma DLL interna com componentes prontos, incluindo o **fluxo de login completo**. Este documento não conhece a API dela — **descubra antes de construir as telas**.

### 2.1 Levantamento obrigatório

1. Localize a DLL (e XML de documentação/README, se houver) e liste namespaces, componentes Razor, serviços de DI e métodos de extensão (`AddBBICore...`, `UseBBICore...`, `MapBBICore...`).
2. Identifique:
   - como registrar e ativar o **login** (middleware, páginas, rotas de login/logout, cookie/token);
   - como obter o **usuário autenticado** (claims, serviço, `AuthenticationStateProvider`);
   - se há **perfis/permissões** e como consultá-los;
   - quais **componentes de UI** existem (layout, menu, grid/tabela, formulários, diálogos, seletores, upload, toasts);
   - se há serviço de **e-mail**, **log** ou **configuração** reaproveitável.
3. Escreva `docs/BBICore.md` com o que encontrou e o mapeamento "necessidade do sistema → componente BBICore". Onde não houver equivalente, registre qual biblioteca foi usada no lugar.
4. Se algo essencial for ambíguo (ex.: não fica claro como obter o id do usuário), **pare e pergunte ao usuário** em vez de adivinhar.

### 2.2 Contrato de usuário atual

Todo o sistema depende de uma única abstração:

```csharp
public interface IUsuarioAtualProvider
{
    Usuario ObterUsuarioAtual();
    int ObterIdUsuarioAtual();
}
```

- Implemente-a sobre a BBICore (`UsuarioAtualBBICore`). Nenhuma outra classe deve ler claims ou a BBICore diretamente.
- A tabela local `Usuarios` continua existindo (é usada em FKs, atribuição, menções, e-mails). Vincule-a ao usuário da BBICore por um identificador estável (ex.: coluna `LoginExterno` única — login/matrícula/id da BBICore).
- **Primeiro login** de um usuário sem registro local: crie o `Usuario` automaticamente (nome, e-mail vindos da BBICore). Logins seguintes: atualize nome/e-mail se mudaram.
- Processos em segundo plano (automações, webhook do GitHub) usam o **usuário de sistema** configurado em `Automacao:UsuarioSistemaId` — eles não têm usuário autenticado.
- Os interceptors de auditoria e de eventos precisam do usuário atual fora de componentes; garanta que o provider funcione no escopo do circuito Blazor e caia no usuário de sistema quando não houver usuário (serviços hospedados).

### 2.3 Login nas duas aplicações

- Sistema principal e portal exigem login BBICore. Remova qualquer ideia de "seletor de usuário".
- O portal mostra ao usuário **somente as solicitações em que ele é o solicitante** — o id vem do usuário autenticado, nunca de parâmetro da URL.
- Endpoints HTTP (`/api/anexos/{id}`, exportações) exigem usuário autenticado; o de anexo confere se o usuário pode ver o cartão.
- O webhook do GitHub (`/api/integracoes/github`) é **anônimo** (autenticado por assinatura HMAC) e o hub SignalR aceita a chamada do portal (seção 13.6) — configure as exceções explicitamente.

### 2.4 Perfis mínimos

Use o modelo de perfis da BBICore se existir; senão, uma tabela simples de perfis por usuário. Perfis:

| Perfil | Acesso |
|---|---|
| **Administrador** | Tudo, incluindo Parametrização, cadastros (usuários, sistemas, feriados, templates), automações e configuração de colunas |
| **Time** | Quadros, cartões, sprints, relatórios, notificações, busca; cria/edita automações do quadro |
| **Solicitante** | Só o portal |

> Confirme com o usuário se esses perfis atendem antes de fechar a etapa de permissões. Não bloqueie o restante do desenvolvimento por isso.

---

## 3. Conceitos centrais (glossário)

| Termo | Significado |
|---|---|
| **Quadro** | Kanban de um time/frente. Tem listas, etiquetas, sprints, templates, regras de automação. |
| **Lista / coluna** | Etapa do fluxo. Pode ter **sublistas** (auto-FK). |
| **Lista agrupadora** | Lista que tem sublistas. **Não recebe cartões**: cartão sempre fica numa lista "folha". Soltar numa agrupadora envia para a primeira sublista. |
| **Caminho do quadro** | Ordem oficial do fluxo (`OrdemFluxo`). Trava a reordenação de colunas contra o fluxo e **avisa, sem bloquear**, quando o cartão pula/volta etapas. |
| **Etapa** | Uma lista vista pelas suas regras: campos exigidos, proibidos, automáticos, bloqueio de entrada. |
| **Campo configurável** | `DefinicaoCampo` de uma lista (texto, número, data, seleção, checkbox, área de texto). |
| **Campo fixo** | Desenvolvedor(es), Prazo, Data de início, Estimativa, Sistema, Solicitante. |
| **Vai-e-volta / estorno** | Voltar à lista de origem dentro da janela de correção desfaz o movimento anterior. |
| **Código do cartão** | `KB-123` (prefixo configurável + id). Usado em commits, e-mails, portal. |
| **Status público** | Nome que o solicitante vê no portal, definido por lista. |
| **Espera das automações** | Eventos gerados por pessoas só executam após N minutos sem nova alteração no cartão. |

---

## 4. Modelo de dados

### 4.1 Convenções

- `EntidadeBase` (maioria das entidades): `Id int`, `CriadoEm datetime2`, `CriadoPorId int`, `AlteradoEm datetime2?`, `AlteradoPorId int?`, `Excluido bit`, `ExcluidoEm datetime2?`, `ExcluidoPorId int?`.
- **Soft-delete**: `HasQueryFilter(e => !e.Excluido)` em todas as entidades com `Excluido`. Exclusão = marcar `Excluido`, `ExcluidoEm`, `ExcluidoPorId`. Nada é apagado fisicamente (exceto junções N:N e registros técnicos indicados).
- **Auditoria**: interceptor de `SaveChanges` preenche `CriadoEm/CriadoPorId` em `Added` e `AlteradoEm/AlteradoPorId` em `Modified`.
- **Datas em UTC** no banco; converta para local só na exibição. Campos "só data" (ex.: feriado) guardados como data à meia-noite.
- **Enums** gravados como `int`. **Flags** (`[Flags]`) também como `int`.
- JSON em colunas `nvarchar(max)` quando indicado.

### 4.2 Entidades

Tamanhos sugeridos entre parênteses. `?` = anulável.

**Usuario** (EntidadeBase): `Nome`(200), `Email`(200), `LoginExterno`(200, único — vínculo com BBICore), `Ativo` (padrão true), `NotificarPorEmail` (recebe e-mail quando mencionado; padrão false), `ReceberEmailsAutomacao` (padrão true).

**Sistema** (EntidadeBase): `Nome`(200), `Ativo`. Cadastro global.

**Quadro** (EntidadeBase): `Nome`(200), `Descricao?`, `Cor`(20, padrão `#1565C0`), `TemplateQuadroId?`.

**Lista** (EntidadeBase):
- `Nome`(200), `Ordem int`, `QuadroId`, `ListaPaiId?` (auto-FK, sublistas), `LimiteWip int?` (só sinaliza), `EhBacklog bit` (no máximo uma por quadro), `OrdemFluxo int?` (posição no caminho; null = fora).
- `CamposFixosExigidos` (flags `CamposFixosCartao`), `CamposFixosBloqueados` (flags), `BloquearEntradaComPendencias bit`.
- `StatusPortal`(100)? — status público no portal.

**Cartao** (EntidadeBase):
- `Titulo`(300), `Descricao?`(4000+), `ListaId`, `Ordem int`.
- `DataInicio?`, `Prazo?`, `Estimativa decimal(10,2)?` (pontos), `Prioridade?` (enum).
- `SistemaId?`, `SolicitanteId?` (Usuario), `CartaoPaiId?` (auto-FK).
- `OrigemPortal bit`, `TipoSolicitacao`(100)?.
- Coleções: desenvolvedores, etiquetas, itens de tarefa, valores de campo, comentários, reuniões, anexos, histórico, relações (origem/destino), sprints, e-mails, alertas, vínculos Git.

**CartaoDesenvolvedor** (junção, PK composta CartaoId+UsuarioId): `Principal bit`.

**Etiqueta** (EntidadeBase): `Nome`(100), `Cor`(20), `QuadroId`. **CartaoEtiqueta** (junção CartaoId+EtiquetaId).

**DefinicaoCampo** (EntidadeBase): `Nome`(200), `Tipo` (enum `TipoCampo`), `ListaId`, `Obrigatorio`, `PedirNovamenteACadaEntrada`, `OpcoesSelecao?` (separadas por `|`), `Ordem`, `Preenchimento` (enum), `RegraReentrada` (enum), `DefineDataInicioCartao bit`.

**ValorCampoCartao** (EntidadeBase): `CartaoId`, `DefinicaoCampoId`, `Valor?` (string: data ISO `yyyy-MM-dd`, número invariante, checkbox `true/false`), `NumeroEntradaNaLista int` (1, 2, ...), `DataPreenchimento`, `PreenchidoPorId`, `HistoricoOrigemId?` (movimento que preencheu automaticamente), `CriadoAutomaticamente bit`, `ValorAnteriorAutomatico?`.

**HistoricoAtividade** (sem EntidadeBase): `Id`, `CartaoId`, `Tipo` (enum), `ListaOrigemId?`, `ListaDestinoId?`, `Descricao`(1000), `ValorAnterior?`, `ValorNovo?`, `UsuarioId`, `OcorridoEm`, `Estornado bit`, `AlterouDataInicio bit`, `DataInicioAnterior?`. **Query filter `!Estornado`** (estornos ficam no banco para auditoria mas somem de tudo).

**ItemTarefa** (EntidadeBase): `Titulo`(300), `Concluido`, `CartaoId`, `Ordem`, `Descricao?`, `DesenvolvedorId?` (restrito aos devs do cartão pai), `CartaoPromovidoId?`.

**Comentario** (EntidadeBase): `Texto`, `AutorId`, `DataHora`, `Editado`, `Publico bit` (visível ao solicitante), `CartaoId?` **ou** `ItemTarefaId?`.

**Reuniao** (EntidadeBase): `Data`, `Ata`, `AutorId`, `CartaoId?` **ou** `ItemTarefaId?`. **ReuniaoParticipante** (junção ReuniaoId+UsuarioId).

**RelacaoCartao** (EntidadeBase): `CartaoOrigemId`, `CartaoDestinoId`, `Tipo` (`BloqueadoPor`, `Bloqueia`, `RelacionadoA`, `DuplicadoDe`). Gravada sempre a partir do cartão aberto (origem).

**Anexo** (EntidadeBase): `CartaoId`, `NomeOriginal`(260), `NomeArmazenado`(260), `Caminho`(500), `ContentType`(200), `TamanhoBytes bigint`, `Comprimido bit`, `TamanhoArmazenadoBytes bigint`, `EnviadoPorId`.

**EmailCartao** (EntidadeBase): `CartaoId`, `Para`(1000), `Cc?`, `Assunto`(500), `CorpoHtml`, `EnviadoEm`, `EnviadoPorId`, `Enviado bit`, `ErroEnvio?`.

**Notificacao** (EntidadeBase): `DestinatarioId`, `Tipo` (enum), `Mensagem`(1000), `Lida`, `LidaEm?`, `CartaoOrigemId?`.

**Sprint** (EntidadeBase): `Nome`, `Meta?`, `DataInicio`, `DataFim`, `Ativa`, `Fechada`, `QuadroId`. **SprintCartao** (junção SprintId+CartaoId): `AdicionadoEm`.

**TemplateQuadro** (EntidadeBase): `Nome`, `Descricao?`, `EstruturaJson` (listas, sublistas, campos, automações).
**TemplateCartao** (EntidadeBase): `Nome`, `TituloPadrao?`, `DescricaoPadrao?`, `QuadroId`, `ItensTarefaPadrao?` (JSON lista de strings), `CamposPadrao?` (JSON dicionário nome→valor).

**RegraAutomacao** (EntidadeBase): `Nome`, `Descricao?`, `Ativa`, `QuadroId`, `Gatilho` (enum), `ListaId?`, `ParametrosGatilhoJson?`, `CondicoesJson?`, `ExigirTodasCondicoes bit` (E/OU), `SomenteHorarioComercial bit`, `UltimaExecucaoEm?`.
**AcaoAutomacao** (EntidadeBase): `RegraAutomacaoId`, `Tipo` (enum), `Ordem`, `ParametrosJson?`.
**ExecucaoAutomacao**: `Id`, `RegraAutomacaoId`, `QuadroId`, `CartaoId?`, `Gatilho`, `ChaveDisparo?`(200), `Status` (enum), `Resumo`, `Erro?`, `DesfazerJson?`, `EmailsEnviados int`, `MensagensTeams int`, `UsuarioId`, `OcorridoEm`, `DesfeitaEm?`, `DesfeitaPorId?`. Índice em (RegraAutomacaoId, CartaoId, ChaveDisparo).
**SugestaoAutomacao**: `Id`, `RegraAutomacaoId`, `QuadroId`, `CartaoId`, `Descricao`, `PropostaJson`, `Status` (enum), `CriadoEm`, `DecididoEm?`, `DecididoPorId?`.
**AlertaCartao**: `Id`, `CartaoId`, `RegraAutomacaoId?`, `Mensagem`, `Severidade` (enum), `Resolvido`, `CriadoEm`, `ResolvidoEm?`, `ResolvidoPorId?`.
**EventoAutomacaoPendente**: `Id`, `CartaoId`, `Gatilho`, `ListaId?`, `Campo` (enum `CampoMonitorado`), `UsuarioAlvoId?`, `UsuarioId`, `CriadoEm`, `ExecutarEm`. Índice em `ExecutarEm` e em (CartaoId, Gatilho).

**ParametroSistema**: `Chave`(200, PK), `Valor?`, `AlteradoEm`, `AlteradoPorId`.
**Feriado** (EntidadeBase): `Data`, `Nome`(200), `Tipo` (enum), `RecorrenteAnual bit`.
**VinculoGit**: `Id`, `CartaoId`, `Tipo` (Branch=1, Commit=2, PullRequest=3), `Repositorio`(300), `Identificador`(300), `Titulo`(500), `Url`(1000), `Autor`(200)?, `Estado?` (Aberto=1, Fechado=2, Mergeado=3), `CriadoEm`, `AtualizadoEm`. **Índice único** (CartaoId, Tipo, Repositorio, Identificador).

### 4.3 Enums (valores exatos)

```
Prioridade: Baixa=1, Media=2, Alta=3, Critica=4   (exibir "Média", "Crítica")
TipoCampo: Texto=1, Numero=2, Data=3, Selecao=4, Checkbox=5, AreaTexto=6
PreenchimentoAutomatico: Manual=0, DataEntrada=1 (só Data), UsuarioQueMoveu=2 (só Texto/AreaTexto)
RegraReentrada: ManterPrimeiro=0, AtualizarUltimo=1, NovoRegistro=2
[Flags] CamposFixosCartao: Nenhum=0, Desenvolvedor=1, Prazo=2, Estimativa=4, Sistema=8, Solicitante=16, DataInicio=32
TipoHistoricoAtividade: CartaoEntrou=1, CartaoSaiu=2, CampoAlterado=3, CartaoCriado=4, CartaoMovido=5, ComentarioAdicionado=6,
  ReuniaoRegistrada=7, AnexoAdicionado=8, ItemTarefaAlterado=9, SprintVinculada=10, EmailEnviado=11, CartaoAtribuido=12, PrazoCriado=13, PrazoAlterado=14
TipoNotificacao: AtribuicaoCartao=1, MencaoComentario=2, CampoPendente=3, ConflitoData=4, BloqueioCartao=5, AutomacaoRegra=6, MencaoReuniao=7, MensagemSolicitante=8
TipoRelacaoCartao: BloqueadoPor=1, Bloqueia=2, RelacionadoA=3, DuplicadoDe=4
TipoFeriado: Nacional=1, Estadual=2, Municipal=3, PontoFacultativo=4, Empresa=5

TipoGatilho (≥20 = gatilho de tempo):
  CartaoCriado=1, CartaoEntrouNaLista=2, CartaoSaiuDaLista=3, CampoAlterado=4, CartaoAtribuido=5, ComentarioAdicionado=6,
  ChecklistConcluido=7, CartaoBloqueado=8, ConflitoDatas=9, CommitVinculado=10, PullRequestAberto=11, PullRequestMergeado=12,
  PrazoProximo=20, PrazoVencido=21, ParadoNaLista=22, Agendado=23, SprintIniciada=24, SprintEncerrando=25
CampoMonitorado: Qualquer=0, Prazo=1, DataInicio=2, Prioridade=3, Desenvolvedores=4, Etiquetas=5, Sistema=6, Solicitante=7, Estimativa=8, CampoPersonalizado=9
TipoCondicao: Lista=1, Prioridade=2, Etiqueta=3, Sistema=4, Desenvolvedor=5, Solicitante=6, CampoPersonalizado=7, TemPrazo=8, Atrasado=9,
  Bloqueado=10, DiasNaLista=11, Estimativa=12, TemConflito=13, PrazoEmRisco=14, ChecklistCompleto=15, Titulo=16, DiasParaPrazo=17
OperadorCondicao: EstaEm=1, NaoEstaEm=2, Igual=3, Diferente=4, Contem=5, NaoContem=6, Vazio=7, NaoVazio=8, MaiorOuIgual=9, MenorOuIgual=10, Sim=11, Nao=12
[Flags] DestinatariosAutomacao: Nenhum=0, Desenvolvedores=1, DesenvolvedorPrincipal=2, Solicitante=4, Criador=8, UsuariosEspecificos=16
ModoAtribuicao: UsuarioEspecifico=1, MenorCarga=2, Revezamento=3, Solicitante=4, QuemDisparou=5
BaseDataAutomacao: Hoje=1, PrazoAtual=2, Limpar=3
EstrategiaConflito: MenorPrioridade=1, MaisRecente=2, NaoIniciado=3, MaisAntigo=4
ResolucaoConflito: EmpurrarDatas=1, MoverParaLista=2, ApenasAvisar=3
ModoAplicacao: Sugerir=1, Aplicar=2
SeveridadeAlerta: Info=1, Aviso=2, Critico=3
StatusExecucaoAutomacao: Sucesso=1, Parcial=2, Erro=3, Desfeita=4, Simulada=5
StatusSugestao: Pendente=1, Aprovada=2, Rejeitada=3, Descartada=4
TipoAcaoAutomacao: MoverCartao=1, NotificarResponsavel=2, PreencherCampo=3, AtribuirDesenvolvedor=4, RemoverDesenvolvedores=5,
  AdicionarEtiqueta=6, RemoverEtiqueta=7, DefinirPrioridade=8, DefinirPrazo=9, AdicionarChecklist=10, CriarCartaoFilho=11,
  Comentar=12, EnviarEmail=13, CriarAlerta=14, ResolverConflitoDatas=15, EnviarResumo=16, ResolverAlertas=17,
  PostarNoTeams=18, PostarResumoNoTeams=19
[Flags] CamposPortal: Nenhum=0, Prazo=1, DataInicio=2, Prioridade=4, Sistema=8, Desenvolvedores=16, Estimativa=32, Etiquetas=64,
  AnexosDoTime=128, HistoricoStatus=256, Comentarios=512
```

### 4.4 SQL Server — cuidados específicos

- **Múltiplos caminhos de cascata** são proibidos no SQL Server. Use `DeleteBehavior.Restrict`/`NoAction` em: auto-FKs (`Lista.ListaPaiId`, `Cartao.CartaoPaiId`), `RelacaoCartao` (duas FKs para Cartao), `HistoricoAtividade` (ListaOrigem/ListaDestino), todas as FKs para `Usuario` (autor, criador, destinatário...), `Comentario`/`Reuniao` (Cartao **ou** ItemTarefa), `ItemTarefa.CartaoPromovidoId`, `ValorCampoCartao.HistoricoOrigemId`. Como a exclusão é lógica, cascata física quase nunca é necessária.
- Tipos: `datetime2`, `decimal(10,2)` para estimativa, `nvarchar` com tamanhos da 4.2, `nvarchar(max)` para JSON/HTML/texto longo.
- Índices únicos com filtro quando convier (ex.: `Usuario.LoginExterno` único onde não nulo).
- Chave do índice único de `VinculoGit`: 4 + 4 + 600 + 600 bytes, dentro do limite de 1700 bytes de índice não clusterizado.
- **Migrations não são aplicadas na inicialização.** Gere scripts SQL (`dotnet ef migrations script --idempotent`) em `database/scripts/` e documente em `database/README.md` a ordem de execução. Um método `MigrarBancoAsync` pode existir para desenvolvimento, mas **não é chamado** no `Program.cs`.
- Connection string fora do repositório (user-secrets em dev, variável de ambiente/cofre em produção). O `appsettings.json` só tem placeholder.

### 4.5 Dados iniciais (seed)

- Usuário de sistema (Id 1, "Sistema"/"Admin") usado por automações e webhook.
- Um quadro de exemplo com listas: Backlog (EhBacklog, bloqueia Desenvolvedor+Prazo+DataInicio), A Fazer (exige Desenvolvedor, campo automático "Atribuído em"), Em Andamento (campo automático "Data de início" que define a Data de início do cartão), Homologação (sublistas Code Review e Testes do Usuário), Concluído.
- Alguns sistemas e etiquetas (Bug, Feature, Melhoria).

---

## 5. Quadro, listas e cartões

### 5.1 Listas e sublistas

- N quadros, N listas por quadro, ordenáveis por arrastar (pelo cabeçalho) e por botões ←/→.
- Sublistas aparecem **dentro da coluna da lista pai** (empilhadas verticalmente), não como colunas soltas.
- **Lista agrupadora não recebe cartões**: drop ou criação nela vai para a primeira sublista (ordenada por `Ordem`). Ao criar a **primeira** sublista de uma lista que já tem cartões, mova esses cartões para a nova sublista.
- **WIP**: se cartões ativos > `LimiteWip`, cabeçalho/contador em destaque. Não bloqueia.
- Backlog: switch "É o backlog do quadro" (um por quadro) e chip "Backlog" no cabeçalho.
- Helpers puros (em `Core/Regras/ListasQuadro`): `EhGrupo`, `PrimeiraFolha(listaId, listas)` (desce pela primeira sublista até uma folha), `FolhasEmOrdem` (para selects de lista), `NomeCompleto` ("Homologação › Code Review"), `EhConcluido(listaId, listas)` = nome da lista é "Concluído" (case-insensitive).

### 5.2 Caminho do quadro

- Diálogo "Caminho" define a ordem oficial (`OrdemFluxo`) das listas que fazem parte do fluxo.
- Reordenar colunas contra o caminho é **bloqueado** (com mensagem).
- Mover cartão para lista que não é a próxima (pular etapas ou voltar) **avisa sem bloquear** (toast de aviso). Movimentos para listas fora do caminho não avisam.

### 5.3 Board (tela do quadro)

- Drag-and-drop de cartões (posição exata entre cartões, dentro e entre listas) e de listas. **Atenção:** no Blazor Server os eventos de arraste chegam tarde demais para `dataTransfer`; use um helper JS que define `dataTransfer` de forma síncrona e chama o .NET no drop. Destaque visual da coluna/posição de destino.
- **Otimista**: mova o cartão na tela antes de gravar; em erro, recarregue.
- Carregamento com `DbContext` próprio e `AsSplitQuery()`; agrupe avisos de recarga seguidos (debounce) para evitar recargas concorrentes.
- Colunas mostram até `Quadro:CartoesPorColuna` cartões e botão "Mostrar mais".
- **Filtros combináveis** (board e tabela): texto, lista, sistema, prioridade, desenvolvedor, solicitante, etiqueta, intervalo de datas (por prazo, início ou criação — selecionável) e atalho **"Somente meus cartões"** (usuário logado entre os desenvolvedores).
- **Visão Tabela**: mesmos filtros, ordenável por todas as colunas, incluindo campos configuráveis (campos de mesmo nome em listas diferentes viram uma coluna), paginada, com exportação Excel/PDF do que está filtrado.
- Botões do cabeçalho de coluna (todos com tooltip): configurar etapa (campos e regras), adicionar sublista, adicionar cartão, mover lista ←/→.
- Tooltips em **todos** os botões de ação.

### 5.4 Layout do cartão (estilo Trello, tamanho confortável)

De cima para baixo:
1. Código (`KB-123`, pequeno) + ícone de portal se `OrigemPortal`; menu ⋮ (editar, detalhes, excluir...).
2. **Título em destaque**.
3. Descrição limitada a `Cartao:LinhasDescricao` linhas com reticências (pode ser desligada por `Cartao:MostrarDescricao`).
4. Etiquetas (chips coloridos com **nome**, nunca id).
5. Sistema (ícone + nome).
6. Desenvolvedores (ícone + nomes, principal primeiro).
7. **Campos da lista atual**: valor mais recente de cada campo configurável da lista; obrigatório vazio aparece como "pendente".
8. Rodapé com badges: prioridade, prazo (cor: no prazo / vence em ≤2 dias / atrasado), início (só se preenchido), estimativa, contadores de comentários/reuniões/anexos, tarefas "3/5", subtarefas (filhos) "2/4", campo pendente, **conflito de data** (clicável: mostra com quais cartões conflita), **bloqueio**, chip de PR (quantos PRs vinculados/abertos), chip de sugestão de automação pendente, alertas de automação (clicar resolve).
9. Borda lateral com a cor da prioridade.

**Selects nunca mostram ids**: todo seletor precisa exibir o nome (no MudBlazor 8 isso exige `ToStringFunc`; confira o equivalente no componente da BBICore).

### 5.5 Criação e edição do cartão (mesmo diálogo)

- Template de cartão (opcional, só na criação): aplica título, descrição, itens de tarefa padrão e valores de campo padrão (por nome do campo da lista).
- Campos: título*, lista (só folhas; editável também na edição — mover por aqui passa pelo fluxo de movimentação), descrição, início (só aparece se já existir; normalmente vem da etapa), prazo, estimativa, prioridade, desenvolvedores (multisseleção), desenvolvedor principal (se >1), sistema, solicitante, etiquetas (multisseleção), campos configuráveis da lista (automáticos em somente leitura).
- Rótulo com `*` nos campos que a lista exige; campos proibidos pela lista **desabilitados** com texto "Não permitido em <lista>" (ou "será removido ao mover" se o usuário escolheu outra lista).
- Validação: início ≤ prazo.
- Criação numa lista com `BloquearEntradaComPendencias` exige o que falta antes de salvar.
- Ao salvar: notifica desenvolvedores recém-atribuídos; verifica conflito de data e notifica (sem duplicar notificação não lida do mesmo tipo/cartão); aplica campos automáticos da lista de criação; descarta campos proibidos.

### 5.6 Multisseleção com filtro

Componente próprio (ou da BBICore, se tiver igual): **chips e digitação na mesma caixa**, lista de opções **permanece aberta** enquanto se marca, filtro conforme digita, check nos selecionados, botão limpar, teclado (↑ ↓ Enter Esc Backspace remove o último chip). **Ao selecionar um item, o texto digitado é limpo.** Usado em desenvolvedores, etiquetas, participantes de reunião e cartões da sprint.

### 5.7 Detalhes do cartão (tela cheia)

- Cabeçalho: chip do código (tooltip "cite em commits, branches e PRs"), chip "Portal · <tipo>" se veio do portal, título, botão "Excluir cartão".
- Duas colunas: abas (8/12) e painel lateral de resumo (4/12). **As duas colunas precisam de `min-width:0`** para as abas não estourarem a largura.
- Painel lateral: alertas de automação (fechar = resolver), sugestões pendentes (aprovar/rejeitar), aviso de exigências faltando, **seletor de lista** (move pelo fluxo completo), sistema, desenvolvedores, solicitante, prioridade, prazo, início, estimativa, etiquetas.
- Abas:
  1. **Linha do tempo** unificada, paginada ("Carregar mais"): comentários, reuniões, e-mails, movimentações (sem estornos), PRs, execuções de automação.
  2. **Detalhes**: descrição (salvar) + campos da lista atual (salvar). Campo "pedir a cada entrada" mostra histórico das entradas anteriores.
  3. **Tarefas**: nova tarefa (botão e Enter; proteja contra clique duplo), lista com checkbox, promover a cartão filho, expandir (descrição, comentários do item, reuniões do item), **excluir**.
  4. **Comentários**: novo comentário, "Visível ao solicitante" (só se o cartão tem solicitante **e** a conversa está habilitada no portal), editar e **excluir** (só o autor), selo "público", paginação.
  5. **Reuniões**: data, participantes (multisseleção), ata; lista com **excluir**.
  6. **Anexos**: upload (valida extensão, tamanho por arquivo e total), baixar, **excluir**.
  7. **E-mail** de cobrança (seção 10.3).
  8. **Código**: vínculos do GitHub agrupados (PRs, branches, commits) com estado do PR; se vazio, instrução para citar o código.
  9. **Relações**: vincular cartão + tipo; **remover**.
- **Toda exclusão é lógica e pede confirmação.** Excluir tarefa exclui também comentários e reuniões dela; o cartão filho promovido continua existindo.
- **Armadilha conhecida:** se o cartão é carregado com rastreamento do EF, ao salvar uma entidade filha o EF já a inclui na coleção de navegação; incluir de novo na tela **duplica** o item. Use um helper "incluir uma vez" ou recarregue.

### 5.8 Itens de tarefa (checklist evoluído)

- Modo simples (título + checkbox) e detalhado (descrição, comentários e reuniões próprios).
- `DesenvolvedorId` restrito aos devs do cartão pai.
- **Promoção a cartão filho**: cria cartão na mesma lista com `CartaoPaiId`, herda título, descrição, sistema, etiquetas e o desenvolvedor do item (como principal); migra comentários e reuniões do item; o item passa a apontar para o filho, mostra "Ver cartão filho", fica com checkbox travado e **acompanha o status do filho** (concluído enquanto o filho estiver em "Concluído"; atualizado sempre que o filho é movido).

### 5.9 Relações e bloqueio

- `BloqueadoPor` gravado a partir do cartão A → **A** fica bloqueado pelo destino. `Bloqueia` → quem fica bloqueado é o **destino**.
- Cartão bloqueado por outro não concluído recebe badge; ao criar a relação, notifica os desenvolvedores do cartão bloqueado (`BloqueioCartao`).

### 5.10 Conflito de data

- Dois cartões conflitam se compartilham ao menos um desenvolvedor e os intervalos `[DataInicio ?? Prazo, Prazo]` (por dia) se sobrepõem. Sem prazo = não conflita. Cartões concluídos não conflitam.
- Calculado sob demanda (ao carregar quadro e ao salvar datas).

---

## 6. Motor de campos e regras por etapa

### 6.1 Campos configuráveis

- Configurados pela engrenagem da coluna (diálogo "Configuração de <lista>"): nome, tipo, opções (seleção), obrigatório, pedir a cada entrada, **preenchimento** (manual / data de entrada / usuário que moveu — validando tipo), **regra de reentrada**, "define a Data de início do cartão" (só para data automática), ordem.
- Renderização dinâmica por um componente único que recebe a definição e desenha o input correto.
- Valor mais recente = maior `NumeroEntradaNaLista`, depois maior `DataPreenchimento`.
- "Pedir a cada entrada": cada entrada gera um novo `ValorCampoCartao` com `NumeroEntradaNaLista` = nº de entradas daquele cartão na lista, **contado no histórico** (movimentos e criação, sem estornos).
- Obrigatório vazio **não bloqueia** (a menos que a lista bloqueie a entrada): gera badge "pendente" e notificação `CampoPendente` aos desenvolvedores.

### 6.2 Campos automáticos

Ao entrar na lista (movimento ou criação direto nela), cada campo com `Preenchimento != Manual`:
- `DataEntrada` → data de hoje (`yyyy-MM-dd`); `UsuarioQueMoveu` → nome do usuário.
- Reentrada: `ManterPrimeiro` (não mexe se já tem valor), `AtualizarUltimo` (sobrescreve, guardando `ValorAnteriorAutomatico`), `NovoRegistro` (novo registro por entrada).
- Guarda `HistoricoOrigemId` = movimento que preencheu e `CriadoAutomaticamente`.
- Se `DefineDataInicioCartao` e aplicou (e, para `ManterPrimeiro`, o cartão ainda não tem início): `Cartao.DataInicio = hoje`, e o movimento guarda `AlterouDataInicio = true` e `DataInicioAnterior`.

Exemplo esperado: Backlog → A Fazer (exige desenvolvedor; "Atribuído em" automático) → Em Andamento ("Data de início" automática que define o início do cartão). Cartão no Backlog **não mostra início**.

### 6.3 Exigências da etapa

- `CamposFixosExigidos` (Desenvolvedor, Prazo, Estimativa, Sistema, Solicitante) + campos configuráveis obrigatórios.
- `BloquearEntradaComPendencias`:
  - **true**: antes de mover abre o diálogo "Entrada em <lista>" pedindo o que falta; cancelar = não move.
  - **false**: move e depois oferece o mesmo diálogo (opcional); o cartão fica sinalizado e os responsáveis notificados.
- O diálogo também pede campos "a cada entrada".

### 6.4 Campos não permitidos na etapa

- `CamposFixosBloqueados` (Desenvolvedor, Prazo, Data de início, Estimativa, Sistema, Solicitante). Um campo não pode ser exigido e proibido na mesma lista (checkboxes mutuamente exclusivos).
- Na edição: campos desabilitados.
- **Ao mover pela tela** para uma lista que proíbe algo preenchido: confirmação "‹Lista› não permite: X, Y. Ao mover, esses dados serão removidos." → confirmar remove e registra `HistoricoAtividade` `CampoAlterado` ("Removido ao entrar em ‹lista›..."); cancelar não move.
- Movimento feito por automação: remove sem perguntar (registra no histórico).
- Criação direta na lista: descarta os proibidos.
- Automação que tentaria preencher um campo proibido (atribuir, definir prazo/início) **falha a ação** com mensagem "a lista X não permite Y".
- Ao salvar a regra, contar cartões já na lista com dados proibidos e oferecer "Remover agora".
- **Exceção:** nunca remover o solicitante de cartão com `OrigemPortal` (ele perderia acesso no portal).

### 6.5 Movimentação (serviço único)

Um único serviço (`MovimentacaoCartaoService`, em Infrastructure) usado por board, detalhes, edição, automações e portal. `MoverAsync(cartaoId, listaDestinoId, cartaoAlvoId?, permitirEstorno = true)`:

1. Resolve destino para a primeira folha (se agrupadora; nesse caso ignora a posição alvo).
2. Reordena a lista de destino inserindo antes do alvo (ou no fim) e reordena a origem.
3. Se mudou de lista:
   - **Estorno (vai-e-volta)**: se o último movimento (não estornado) do cartão foi exatamente origem↔destino invertido, pelo **mesmo usuário**, dentro de `Movimentacao:JanelaCorrecaoMinutos` (padrão 10) e `permitirEstorno`: marca aquele movimento `Estornado = true`, desfaz os valores que ele preencheu (exclui os criados automaticamente; restaura `ValorAnteriorAutomatico` dos atualizados) e restaura a Data de início se ele a alterou. **Não** cria novo histórico.
   - Senão: grava `CartaoMovido`, aplica campos automáticos, notifica pendências.
   - Remove campos proibidos (6.4).
   - Sincroniza item de tarefa promovido (5.8).
4. Salva e retorna: mudou de lista, destino, aviso de caminho (5.2), se estornou, campos removidos.

Automações chamam com `permitirEstorno = false`.

Um "assistente" de interface (Web) envolve o serviço: confirmação de campos proibidos → diálogo de pendências (antes ou depois) → mover → toasts (movido, desfeito, aviso de caminho, campos removidos).

Excluir cartão: lógico, reordena a lista, pede confirmação.

---

## 7. Colaboração

### 7.1 Comentários, menções e reuniões

- Comentários editáveis e excluíveis pelo autor ("(editado)").
- `@Nome` em comentários e atas → `Notificacao` (`MencaoComentario`/`MencaoReuniao`) e, se `NotificarPorEmail`, e-mail (falha de e-mail nunca interrompe o fluxo).
- Busca global permite filtrar reuniões por participante.

### 7.2 Notificações

- Sino no topo com contador de não lidas, atualizado em tempo real; tela `/notificacoes` paginada, marcar lida/todas, clique abre o cartão.
- Geradas por: atribuição, menção, campo pendente, conflito de data (sem duplicar não lida), bloqueio, automações, mensagem do solicitante no portal.

### 7.3 E-mail de cobrança (aba E-mail)

- Sugestões de destinatário (solicitante, desenvolvedores) como chips; múltiplos destinatários separados por `;` ou `,`.
- Modelo de cobrança com marcadores `{titulo}`, `{prazo}`, `{lista}`, `{solicitante}`, `{link}`.
- **Rascunho/revisão** antes de enviar. Texto do usuário sempre codificado em HTML (anti-injeção).
- Link direto: `{Aplicacao:UrlBase}/quadros/{quadroId}?cartao={id}` (abre o quadro com o cartão aberto). Se `UrlBase` vazio, use o endereço da primeira requisição recebida.
- E-mail registrado em `EmailCartao` (com erro, se falhou) e aparece na linha do tempo.

### 7.4 Anexos

- Parâmetros: extensões permitidas, MB por arquivo, MB total por cartão, estratégia de nome (`Guid` | `Original` | `CartaoOriginal` = `{id}_{nome}`), política de colisão (`Renomear` → "nome (2).ext" | `Sobrescrever` | `Rejeitar`), normalização do nome contra path traversal.
- **Compactação**: gzip para extensões configuradas se o ganho ≥ `Anexos:GanhoMinimoPercentual`; download devolve o original. Botão na Parametrização para compactar anexos existentes, com estatística de espaço.
- Pasta base configurável (`Anexos:PastaBase`), **compartilhada com o portal**.
- Download por endpoint autenticado que confere exclusão lógica do anexo e do cartão.

---

## 8. Sprints, dashboard, relatórios, busca, templates, exportação

### 8.1 Sprints

- `/sprints`: criar (nome, meta, início, fim), selecionar cartões do backlog (multisseleção), iniciar, **fechar** escolhendo: devolver não concluídos ao backlog **ou** mover para outra sprint.
- Painel da sprint (`/sprints/{id}`): só os cartões da sprint + **burndown** por pontos de estimativa (fallback: contagem de cartões), com linha ideal. Conclusão = primeira entrada em "Concluído" no histórico.

### 8.2 Dashboard (`/dashboard`)

Filtros: quadro, sistema, desenvolvedor, solicitante, período. Indicadores (todos do `HistoricoAtividade`, sem estornos):
- Lead time médio por lista/sublista; tempo de fila por lista; tempo até início (criação → primeira saída do backlog);
- Atrasados/vencendo (contagem + lista); volume por sistema (abertos/concluídos);
- Carga por desenvolvedor (cartões ativos, itens de tarefa ativos, cruzamento com conflito).
- Exportação Excel/PDF. Tabelas paginadas.

### 8.3 Relatórios avançados (`/relatorios`)

Filtros: quadro, período, sistema, desenvolvedor. Abas, cada gráfico **acompanhado de tabela** e exportação Excel:
- **Fluxo acumulado (CFD)**: por dia, quantos cartões em cada lista.
- **Vazão semanal**: cartões e pontos concluídos por semana.
- **Tempo de ciclo / lead time**: por cartão; percentis P50/P85/P95; histograma. "Concluído" = entrada na lista com esse nome; "início" = primeira entrada numa lista que não é backlog nem conclusão.
- **Envelhecimento**: cartões em andamento, dias na lista, idade, atrasado.
- **Previsão (Monte Carlo, 10.000 cenários)** sorteando a vazão semanal real das últimas N semanas: "quando terminam X cartões" (P50/P85/P95 de data) e "quantos até a data alvo". Sem entregas no histórico → aviso "não há base para prever" (não renderize a grade vazia abaixo do aviso, ela sobrepõe o texto).
- **Prazos**: cumprimento por sistema e por desenvolvedor (entregues, no prazo, atrasados, %, atraso médio).
- **Desenvolvedores**: concluídos, pontos, lead time médio, ativos, ativos atrasados.
- Paleta categórica acessível a daltônicos (ex.: `#2a78d6,#eb6834,#1baf7a,#eda100,#e87ba4,#008300,#4a3aa7,#e34948`), cores atribuídas por entidade (não por posição), legenda sempre presente.

### 8.4 Busca global (`/busca`)

Campo no topo. Busca por título, descrição e comentários em todos os quadros acessíveis, trecho com o termo em contexto (reticências), filtro "reunião com participante", paginação no servidor, clique abre o cartão em diálogo.

### 8.5 Templates

- **Quadro**: `EstruturaJson` com listas, sublistas, campos (com todas as opções da 6.1) e automações; aplicado de fato ao criar quadro a partir dele.
- **Cartão**: tela de administração com título/descrição padrão, itens de tarefa padrão e campos padrão (JSON nome→valor).

### 8.6 Exportação

`/api/export/cartoes.xlsx` e `.pdf` aceitando todos os filtros do board (quadro, lista, sistema, prioridade, etiqueta, desenvolvedor, solicitante, somente meus, intervalo por prazo/início/criação, busca). Colunas: código, título, lista, sistema, prioridade, desenvolvedores, solicitante, etiquetas, início, prazo, estimativa, criação.

---

## 9. Motor de automações

### 9.1 Modelo da regra (Quando → Se → Então)

- **Quando**: `Gatilho` + `ListaId?` (entrar/sair/parado; lista-pai vale para as sublistas) + `ParametrosGatilhoJson`:
  ```
  ParametrosGatilho { Campo: CampoMonitorado, Dias: int (=2), Horario: "HH:mm" (="08:00"), DiasSemana: DayOfWeek[] (=seg–sex) }
  ```
- **Se**: `CondicoesJson` = lista de `Condicao { Tipo, Operador, Ids: int[], CampoNome?, Texto?, Numero? }`, combinadas por E (`ExigirTodasCondicoes`) ou OU.
- **Então**: ações ordenadas, cada uma com `ParametrosAcao` (JSON):
  ```
  ListaId?, NoTopo, ModoAtribuicao, UsuarioId?, UsuarioIds[], SubstituirAtuais, EtiquetaId?, Prioridade?, AumentarUmNivel,
  BaseData, Dias, DiasUteis (=true), AplicarEmDataInicio, CampoNome?, Valor?, SomenteSeVazio (=true), Itens[], Titulo?,
  Texto?, Assunto?, Destinatarios (flags), DestinatarioIds[], EmailsFixos?, UrlWebhook?, Severidade,
  Estrategia, Resolucao, Modo (Sugerir|Aplicar), TodosAlertasDoCartao
  ```
- JSON com enums como string; leitura tolerante (JSON inválido → objeto padrão).

### 9.2 Gatilhos

- **De evento** — detectados por um interceptor de `SaveChanges` (valem para qualquer tela, portal e webhook): cartão criado; entrou/saiu de lista; campo alterado (qual campo); desenvolvedor atribuído; comentário adicionado; checklist concluído (último item marcado); cartão bloqueado (relação criada); conflito de datas (datas/devs mudaram e passou a conflitar); commit vinculado; PR aberto; PR mergeado.
- **De tempo** (serviço em segundo plano a cada `Automacao:IntervaloVerificacaoSegundos`): prazo próximo (N dias antes), prazo vencido (repetir a cada N dias; 0 = uma vez), parado na lista N dias, agendado (horário + dias da semana), sprint iniciada, sprint terminando (N dias antes).
- Cada disparo de tempo tem uma **chave** gravada em `ExecucaoAutomacao.ChaveDisparo` para **nunca repetir** o mesmo disparo (regra + cartão + chave). Formatos: prazo vencido `vencido:{prazo:yyyyMMdd}` (com repetição: `vencido:{prazo:yyyyMMdd}:{(diasDeAtraso-1)/N}`), parado `parado:{listaId}:{entradaNaLista:yyyyMMddHHmmss}`, agendado `agendado:{hoje:yyyyMMdd}`, sprint `sprint:{id}:inicio` / `sprint:{id}:fim`, prazo próximo `prazo-proximo:{prazo:yyyyMMdd}` (dispara quando faltam 0..N dias). Mudar o prazo gera uma chave nova (e portanto um novo aviso). Cartões concluídos não disparam prazo. Revezamento de atribuição: próximo candidato depois do último desenvolvedor adicionado pela própria regra (lido do histórico de execuções).
- `SomenteHorarioComercial`: gatilhos de tempo só rodam em dia útil entre `HorarioComercialInicio` e `HorarioComercialFim`.

### 9.3 Condições

Lista (inclui pais), prioridade, etiqueta, sistema, desenvolvedor, solicitante, campo personalizado (por **nome**; texto, número, data, checkbox — valor mais recente, preferindo a definição da lista atual), tem prazo, atrasado, bloqueado, dias na lista, estimativa, tem conflito, **prazo em risco** (estimativa em dias úteis = pontos ÷ `Automacao:PontosPorDia` não cabe nos dias úteis até o prazo), checklist completo, título (contém), dias até o prazo.

### 9.4 Ações

| Ação | Comportamento |
|---|---|
| Mover | Movimento completo (6.5, sem estorno); respeita bloqueio de entrada da etapa (se a lista bloqueia e há pendências, a ação falha). Opção "no topo". |
| Notificar | Notificação in-app para os destinatários. |
| Preencher campo | Campo personalizado por nome; "somente se vazio"; valor aceita marcadores e `{hoje}`. |
| Atribuir | Usuário específico, **menor carga** (menos pontos em andamento entre candidatos), **revezamento** (próximo após o último atribuído pela regra), solicitante, quem disparou. Opção substituir atuais. Notifica o atribuído. Falha se a lista proíbe desenvolvedor. |
| Remover desenvolvedores | Todos ou os informados. |
| Adicionar/remover etiqueta | — |
| Prioridade | Fixa ou +1 nível. |
| Prazo / início | Relativo a hoje ou ao prazo atual, ± N dias (úteis ou corridos), ou limpar. Falha se a lista proíbe o campo. |
| Checklist | Adiciona itens. |
| Cartão filho | Cria filho com título (marcadores). |
| Comentar | Comentário com marcadores (autor = usuário de sistema). |
| E-mail | Assunto/corpo com marcadores, destinatários por flags + usuários + e-mails fixos; respeita `ReceberEmailsAutomacao` e o limite por regra/hora. |
| Alerta | Alerta no cartão com severidade. |
| Resolver alertas | Os da própria regra ou todos do cartão. |
| **Resolver conflito de datas** | Quem cede: menor prioridade, mais recente, mais antigo ou não iniciado. Resolução: **remarcar** para o primeiro período livre do(s) desenvolvedor(es) mantendo a duração em dias úteis; **mover** para uma lista; ou só avisar. Modo **sugerir** (cria `SugestaoAutomacao`) ou **aplicar**. |
| Resumo por e-mail | Execução de quadro: para cada pessoa, e-mail personalizado com seus cartões atrasados, vencendo, bloqueados e em conflito. |
| Postar no Teams / Resumo no Teams | Seção 12. |

**Marcadores** (texto de e-mail, comentário, alerta, Teams): `{codigo}`, `{titulo}`, `{lista}`, `{prazo}`, `{inicio}`, `{prioridade}`, `{desenvolvedores}`, `{solicitante}`, `{sistema}`, `{dias_na_lista}`, `{conflitos}`, `{hoje}`, `{regra}`, `{link}`. Em HTML, valores codificados.

### 9.5 Execução, espera e anti-loop

1. O interceptor gera `EventoAutomacao(Gatilho, CartaoId, UsuarioId, Profundidade, CadeiaDeRegras, ListaId?, Campo?, UsuarioAlvoId?)` e enfileira numa fila em memória (`IFilaEventosAutomacao`). A profundidade/cadeia atual vem de um contexto `AsyncLocal` (quando a alteração foi feita **por** uma automação, o evento herda profundidade+1 e a cadeia).
2. Um serviço hospedado consome a fila:
   - **Evento de pessoa (profundidade 0)** → grava em `EventosAutomacaoPendentes` com `ExecutarEm = agora + Automacao:AtrasoMinutos` (padrão **10**). Se já existe pendente para o mesmo cartão, **todas** as pendências do cartão são adiadas (debounce por cartão: a regra só roda quando o cartão "assenta").
   - **Evento de automação (profundidade > 0)** → executa na hora.
3. O agendador periódico executa os pendentes vencidos e os gatilhos de tempo.
4. **Na execução, revalide a situação**: ex.: "entrou na lista X" só vale se o cartão **ainda** está em X. Assim um vai-e-volta dentro da espera não dispara nada.
5. Anti-loop: profundidade máxima `Automacao:ProfundidadeMaxima` (padrão 3) e uma regra não roda de novo na própria cadeia.
6. Cada evento roda "como" o usuário que o gerou (auditoria), com o usuário de sistema como autor de comentários/e-mails.
7. `ExecucaoAutomacao` registra status (sucesso, parcial, erro), resumo legível, erro, e-mails/mensagens enviados e **operações de desfazer** (`OperacaoDesfazer { Tipo, CartaoId, Id?, ValorAnterior?, Flag }` com tipos: lista, prioridade, prazo, inicio, dev+, dev-, etiqueta+, etiqueta-, campo, alerta+, alerta-, item+, cartao+, comentario+, sugestao+).

### 9.6 Telas de automação (`/admin/automacoes`, por quadro)

- Abas: **Regras** (lista com frase-resumo "Quando… se… então…", ativar/pausar, duplicar, excluir, simular, executar agora), **Histórico** (paginado no servidor; **desfazer** execução), **Na fila** (pendentes: antecipar ou descartar), **Sugestões** (aprovar/rejeitar).
- Editor em tela cheia com seções Quando/Se/Então, ajuda contextual por gatilho/ação, validação, pré-visualização da frase e **modelos prontos**:
  resolver conflitos de agenda (sugerir); conflito: remarcar e voltar para A Fazer; escalonar cartão parado; lembrete de prazo por e-mail; cartão atrasado; resumo diário por e-mail; atribuir por menor carga; prazo em risco; ao concluir: limpar alertas e avisar solicitante; checklist completo → próxima etapa; cartão bloqueado; resumo diário no canal do Teams; avisar o canal do Teams.
- **Simular**: roda condições e ações sem gravar (status `Simulada`), mostrando o que aconteceria com cada cartão.

---

## 10. Parametrização (`/admin/parametros`)

- Valores gravados em `ParametrosSistema` **sobrepõem o appsettings** e valem **sem reiniciar**: implemente um `ConfigurationProvider` que lê a tabela (recarregado ao salvar) e é adicionado ao `IConfiguration` das duas aplicações.
- Cada parâmetro tem: chave, grupo, rótulo, ajuda, tipo (`Texto, Inteiro, Decimal, Booleano, Horario, Opcoes, Usuario, Segredo, Quadro, Lista, MultiOpcoes`), limites, mostra o **padrão** (appsettings) e permite **restaurar**.
- Segredos mascarados, com botão mostrar/gerar.

| Grupo | Chave | Padrão | Uso |
|---|---|---|---|
| Automações | `Automacao:AtrasoMinutos` | 10 | Espera antes de executar eventos de pessoas |
| | `Automacao:IntervaloVerificacaoSegundos` | 60 | Ciclo do agendador |
| | `Automacao:ProfundidadeMaxima` | 3 | Encadeamento |
| | `Automacao:LimiteEmailsPorRegraPorHora` | 50 | — |
| | `Automacao:LimiteTeamsPorRegraPorHora` | 100 | — |
| | `Automacao:PontosPorDia` | 1 | Prazo em risco / remarcação |
| | `Automacao:UsuarioSistemaId` | 1 | Autor das automações e do webhook |
| | `Automacao:HorarioComercialInicio` / `Fim` | 08:00 / 18:00 | — |
| Movimentação | `Movimentacao:JanelaCorrecaoMinutos` | 10 | Vai-e-volta |
| Cartões | `Cartao:PrefixoCodigo` | KB | Código |
| | `Cartao:MostrarDescricao` | true | — |
| | `Cartao:LinhasDescricao` | 2 | — |
| | `Quadro:CartoesPorColuna` | 30 | "Mostrar mais" |
| Geral | `Paginacao:ItensPorPagina` | 25 | Todas as tabelas/listas |
| | `Calendario:ConsiderarFeriados` | true | Dias úteis |
| | `Aplicacao:UrlBase` | "" | Links de e-mail |
| Portal | `Portal:QuadroId`, `Portal:ListaEntradaId` | 0 | Onde entram solicitações |
| | `Portal:UrlBase` | "" | Links nos e-mails ao solicitante |
| | `Portal:StatusInicial` | Recebida | — |
| | `Portal:TiposSolicitacao` | Melhoria,Erro,Dúvida,Nova funcionalidade | — |
| | `Portal:CamposVisiveis` | Prazo,Prioridade,Sistema,HistoricoStatus,Comentarios | Flags `CamposPortal` |
| | `Portal:PermitirAnexos` / `PermitirComentarios` / `NotificarSolicitante` | true | — |
| Integrações | `Integracoes:GitHub:Habilitado` | false | — |
| | `Integracoes:GitHub:SegredoWebhook` | "" | Segredo |
| Anexos | `Anexos:ExtensoesPermitidas` | .pdf,.docx,.xlsx,.png,.jpg,.jpeg,.gif,.zip,.txt | — |
| | `Anexos:TamanhoMaximoMbPorArquivo` / `TamanhoMaximoMbTotal` | 20 / 100 | — |
| | `Anexos:EstrategiaNome` / `PoliticaColisao` | Guid / Renomear | — |
| | `Anexos:Comprimir` / `ExtensoesComprimir` / `GanhoMinimoPercentual` | true / .txt,.csv,.log,.json,.xml,.sql,.md,.html,.htm,.svg,.bmp,.tif,.tiff,.eml,.rtf / 10 | — |

Fora da tela (appsettings/segredos): connection string, SMTP (`Email:Host/Port/Remetente/NomeRemetente/Usuario/Senha`), `Anexos:PastaBase`, `Portal:UrlSistemaPrincipal` (no portal).

Na aba Portal, além dos parâmetros: tabela **"Status por lista"** (lista → status público) com salvar. Na aba Integrações: URL do webhook a configurar no GitHub.

### 10.1 Feriados (`/admin/feriados`)

- CRUD de feriados (nacional, estadual, municipal, ponto facultativo, empresa), "repete todo ano".
- Seletor de ano (ano anterior até +3). **Armadilha:** gerar itens de seleção com `for` e variável de laço capturada faz todos terem o último valor — use `foreach` sobre `Enumerable.Range`.
- Importar nacionais: **BrasilAPI** (`https://brasilapi.com.br/api/feriados/v1/{ano}`) ou **cálculo local** (fixos + móveis a partir da Páscoa: Carnaval seg/ter, Sexta-feira Santa, Corpus Christi) quando sem internet.
- Dias úteis = seg–sex menos feriados (se `Calendario:ConsiderarFeriados`), usados em prazos relativos, remarcação, prazo em risco e horário comercial.

### 10.2 Cadastros

`/admin/usuarios` (com a BBICore, principalmente preferências: ativo, notificar por e-mail, receber e-mails de automação, perfil), `/admin/sistemas`, `/admin/templates`.

---

## 11. Tempo real

- Hub SignalR `/hubs/quadro` com grupos `quadro:{id}` (entrar/sair ao abrir/fechar o quadro) e por usuário (notificações).
- Interceptor de `SaveChanges` (`AlteracoesTempoReal`) coleta quadros afetados (cartão, campos, comentários, reuniões, e-mails, anexos, relações, listas, etiquetas, sprints, alertas, sugestões, vínculos Git, regras) e destinatários de notificação, e publica **após** o commit via `IPublicadorAlteracoes`. As telas recarregam com debounce.
- Outros quadros não recebem avisos.

---

## 12. Integrações (sem Entra ID)

### 12.1 GitHub corporativo (webhook)

- Endpoint `POST /api/integracoes/github`: 404 se desabilitado; 503 se sem segredo; 401 se assinatura inválida (`X-Hub-Signature-256` = HMAC-SHA256 do corpo com o segredo, comparação em tempo constante); anônimo e sem antiforgery.
- Eventos (`X-GitHub-Event`): `ping` → pong; `create` (branch); `push` (branch + commits); `pull_request` (`opened`/`reopened` → PR aberto; `closed` com `merged` → PR mergeado; outros atualizam estado).
- Código do cartão extraído de nome de branch, mensagem de commit, título/corpo/branch do PR: regex `(?<![A-Za-z0-9])KB[-_ ]?(\d+)\b`, case-insensitive, com o prefixo configurado. Commits de uma branch que cita o cartão também vinculam.
- `VinculoGit` com upsert pela chave única (reentrega não duplica). Cartão inexistente é ignorado.
- Enfileira eventos `CommitVinculado`, `PullRequestAberto`, `PullRequestMergeado` como usuário de sistema (um por cartão+gatilho).
- Documentação `docs/integracoes/github.md`: criar webhook no repositório/organização, content type JSON, segredo, eventos, testar com ping, exemplo de regra "PR mergeado → mover para Homologação".

### 12.2 Microsoft Teams (Workflows / webhook de canal)

- Ações "Postar no canal do Teams" e "Resumo no canal do Teams" com URL de webhook do canal (gerada pelo app **Workflows** no canal, modelo "Postar em um canal quando uma solicitação de webhook for recebida"). **Sem Entra ID, sem app registrado, sem mensagens diretas.**
- URL: só `https`, tratada como segredo (mascarada na tela; em descrições mostre só o host).
- Mensagem = Adaptive Card 1.4: `{ type: "message", attachments: [{ contentType: "application/vnd.microsoft.card.adaptive", content: {...} }] }` com título, texto, fatos (lista, prazo, responsáveis...), itens (resumo) e botão "Abrir cartão".
- HttpClient nomeado com timeout de 15 s; erro HTTP = falha da ação. Limite por regra/hora. Botão "Testar envio" no editor.
- Documentação `docs/integracoes/teams.md` explicando passo a passo, limites e segurança.

---

## 13. Portal de solicitações (projeto separado)

### 13.1 Arquitetura

- Aplicação Blazor Server própria, mesmo banco, mesmas regras (`Core`, `Infrastructure`), **login BBICore**.
- **Não executa automações**: sua fila de eventos grava os eventos em `EventosAutomacaoPendentes` com a mesma espera; o sistema principal executa. O sistema principal precisa estar rodando.
- Avisa o sistema principal para atualizar telas abertas: cliente SignalR chama `AvisarAlteracao(int[] quadroIds, int[] destinatariosNotificacao)` no hub do principal (`Portal:UrlSistemaPrincipal`). Falha é ignorada (dado já está no banco). O método só dispara recarga; limite o tamanho dos arrays.
- Usa a **mesma pasta de anexos** (`Anexos:PastaBase` igual nos dois; em servidor, caminho absoluto/compartilhamento).
- Lê a mesma Parametrização (provider de configuração do banco).

### 13.2 Telas

| Rota | Conteúdo |
|---|---|
| `/` Minhas solicitações | Solicitações em que o usuário logado é solicitante (abertas pelo portal ou cadastradas pelo time): código, título, tipo, status público (chip; verde se concluída), aberta em, última atualização. Busca por código/título, filtro por status, "ocultar concluídas", paginação. Aviso se o portal não está configurado. |
| `/nova` Nova solicitação | Título*, tipo* (se houver tipos configurados), sistema, urgência opcional (→ prioridade), descrição* (com dica: contexto, esperado, passos para reproduzir), anexos (se permitido). |
| `/solicitacoes/{id}` Detalhe | Código, status, título, aberta em, informações liberadas, descrição, **conversa com o time** (se liberada), anexos (enviar se permitido; baixar), **andamento** (linha do tempo de status, se liberada). |

- Textos auxiliares em cinza (não use a cor "secundária" do tema se ela for colorida).
- Solicitação concluída não aceita mensagens nem anexos (validado **no serviço**, não só na tela).
- Envio de mensagens desligado: conversa em modo leitura com nota explicativa.
- Downloads via stream pelo circuito, conferindo no servidor se o usuário pode ver o anexo (dele, ou do time se `AnexosDoTime`).

### 13.3 Criar solicitação

Cartão no quadro `Portal:QuadroId`, na primeira folha de `Portal:ListaEntradaId`, `OrigemPortal = true`, solicitante = usuário logado, tipo, sistema, prioridade = urgência; histórico `CartaoCriado`; aplica campos automáticos e remove proibidos da lista; grava anexos. Falha clara se o portal não está configurado ou a lista não existe mais.

### 13.4 Status público

- Histórico de status: começa com `Portal:StatusInicial` na criação; percorre as entradas em listas (criação e movimentos, sem estornos) em ordem; lista com `StatusPortal` muda o status; lista sem status **mantém o anterior**; repetições consecutivas colapsam.
- Status atual = último. "Concluída" = cartão na lista "Concluído".

### 13.5 Conversa e avisos

- Comentário do time com "Visível ao solicitante" → aparece no portal e dispara **e-mail ao solicitante** (se `Portal:NotificarSolicitante` e ele aceita e-mails de automação), com link `{Portal:UrlBase}/solicitacoes/{id}`.
- Mensagem do solicitante → comentário público + notificação `MensagemSolicitante` aos desenvolvedores.
- **Mudança de status público** → e-mail ao solicitante, processado junto das automações (depois da espera; ou seja, vai-e-volta não gera e-mail; entrar em lista sem status não gera e-mail; só envia se a mudança mais recente de status foi causada por esta entrada).
- Desligar "Conversa" em `CamposVisiveis` remove a conversa do portal **e** a opção "Visível ao solicitante" no principal.

### 13.6 Hub do principal

`AvisarAlteracao` precisa aceitar a conexão do portal. Com login obrigatório, defina como o portal se autentica no hub (token de serviço, chave compartilhada em header, ou exceção documentada) — decida com base no que a BBICore oferece e registre em `docs/DECISOES.md`.

---

## 14. Configuração em runtime e serviços em segundo plano

- `ParametrosBancoConfigurationSource/Provider` (Infrastructure) lendo `ParametrosSistema` com recarga ao salvar.
- Serviços hospedados no principal: consumidor da fila de eventos; agendador de automações (pendentes vencidos + gatilhos de tempo); (opcional) compactação de anexos em lote sob demanda.
- **DbContext em Blazor Server**: evite concorrência — registre o `DbContext` como transient ou use `IDbContextFactory` e crie um contexto por operação; nunca compartilhe um contexto entre operações assíncronas simultâneas do mesmo circuito. Serviços em segundo plano criam escopo próprio.

---

## 15. Armadilhas já conhecidas (evite desde o início)

1. **Drag-and-drop no Blazor Server** precisa de helper JS para `dataTransfer` (senão aparece o cursor de bloqueio).
2. **Concorrência do DbContext** ao recarregar o quadro durante uma gravação → contexto próprio por operação.
3. **Selects mostrando ids** → sempre função de texto para o valor selecionado.
4. **Duplicação de itens** ao adicionar numa coleção de entidade rastreada (5.7).
5. **Closure em `for`** dentro de Razor (10.1).
6. **Padrões `<`/`>` no início de linha dentro de `@code`** (ex.: `switch` com `< 1024 =>`) quebram o editor Razor do Visual Studio (a CLI compila, o VS não). Coloque essa lógica em classe `.cs` ou use `if`.
7. **Grade com margem negativa** sobrepondo alertas acima dela — só renderize a grade quando tiver itens.
8. **Colunas flex sem `min-width:0`** estouram a largura com abas/tabelas largas.
9. **Migrations não são automáticas** — documente os scripts.
10. **Datas**: gravar UTC, exibir local; campos de data de `ValorCampoCartao` em `yyyy-MM-dd`.
11. **Textos de usuário em e-mail/HTML** sempre codificados.
12. **Segredos** (connection string, SMTP, segredo do GitHub, URLs do Teams) nunca no repositório nem em logs.

---

## 16. Testes automatizados (mínimo)

xUnit + EF InMemory. Cobrir no mínimo:
- Listas: agrupadora não recebe cartão, primeira folha, nome completo, caminho (aviso ao pular/voltar).
- Movimentação: ordem/posição, histórico, grupo → sublista, aviso de caminho, item promovido sincronizado, notificação de campo pendente, exclusão lógica.
- Campos: data automática, define Data de início, reentradas (3 regras), pendências e bloqueio, atribuição exigida pela etapa.
- **Estorno**: dentro/fora da janela, outro usuário não estorna, desfaz campos e Data de início.
- **Campos proibidos**: pendência aponta o que será removido, mover remove e registra, lista sem bloqueio não mexe, limpar lista, solicitante do portal preservado, automação falha ao atribuir.
- Motor: gatilho/condições/ações principais, anti-loop, espera (evento adiado revalidado não executa), desfazer, simular, limites de e-mail, conflito (remarcar/sugerir), dias úteis com feriados, Páscoa.
- Código do cartão (formatar/extrair), status público (histórico, colapso, listas sem status), configuração do portal (ler/gravar flags).
- Portal: criar (primeira folha, origem portal, anexos, extensão recusada sem criar cartão, sem configuração), listar só os do usuário, visibilidade (campos liberados, comentários públicos, anexos do time), mensagens (pública + notificação; concluída recusa), aviso por e-mail só quando o status muda.
- GitHub: assinatura, push sem duplicar na reentrega, PR mergeado atualiza estado e dispara gatilho.
- Teams: só https, corpo com Adaptive Card, erro HTTP propaga.
- Relatórios: percentis, Monte Carlo com semente fixa.

---

## 17. Ordem de construção

1. Solução, BBICore (levantamento + login + `IUsuarioAtualProvider` + sincronização de usuário), SQL Server, entidades, configurações EF, interceptors de auditoria/soft-delete, migration inicial, seed, scripts SQL.
2. Quadro → listas/sublistas → cartões, board com DnD, criação/edição, detalhes, exclusão, caminho, WIP.
3. Campos configuráveis, regras por etapa (exigidos, proibidos, automáticos, bloqueio), movimentação única com estorno.
4. Papéis fixos, etiquetas, multisseleção, filtros, tabela, badges, conflito de data.
5. Linha do tempo: comentários, reuniões, tarefas (promoção), relações, anexos (com compactação), e-mail de cobrança, menções, notificações.
6. Tempo real (hub + interceptor).
7. Sprints e burndown; dashboard; busca; templates; exportação.
8. Parametrização (provider do banco), feriados, paginação.
9. Motor de automações completo (eventos, espera, tempo, condições, ações, desfazer, simular, sugestões, telas, modelos prontos).
10. Relatórios avançados.
11. GitHub e Teams (+ documentação).
12. Portal (projeto, telas, status público, conversa, avisos, hub) + `docs/portal.md`.
13. Perfis/permissões (confirmar com o usuário), revisão de segurança, README final.

---

## 18. Critérios de aceite

- Build da solução com **0 erros e 0 avisos**; todos os testes passando.
- Scripts SQL Server (completo e idempotente) em `database/scripts` + `database/README.md` com ordem de execução.
- Login BBICore funcionando nas duas aplicações; nenhum seletor de usuário provisório; portal mostra só as solicitações do usuário logado.
- Todos os fluxos das seções 5 a 13 funcionando no navegador, em especial:
  - arrastar cartão/lista; cartão nunca fica numa lista agrupadora;
  - Backlog → A Fazer (pede desenvolvedor) → Em Andamento (preenche início) → voltar dentro da janela desfaz tudo;
  - mover para Backlog com desenvolvedor pede confirmação e remove;
  - regra "entrou em X" só dispara após a espera e não dispara se o cartão saiu antes;
  - solicitação aberta no portal aparece no quadro em tempo real; comentário público aparece no portal e gera e-mail;
  - webhook do GitHub vincula commit/PR e dispara regra; mensagem chega no canal do Teams.
- Documentação: `README.md`, `docs/BBICore.md`, `docs/DECISOES.md`, `docs/portal.md`, `docs/integracoes/github.md`, `docs/integracoes/teams.md`.

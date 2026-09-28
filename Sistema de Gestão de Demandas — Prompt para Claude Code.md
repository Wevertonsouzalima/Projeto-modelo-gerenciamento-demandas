# Sistema de Gestão de Demandas — Prompt para Claude Code

Sep 23, 2026 · @Weverton

## Contexto e objetivo

Construa um sistema web próprio de gestão de demandas e evoluções de um time de desenvolvimento, no estilo Trello/Jira, para substituir o Microsoft Planner (considerado limitado demais pelo time). É um sistema interno, usado por um time de desenvolvimento de mais de 15 pessoas.

O diferencial em relação a ferramentas prontas do mercado é um motor de campos configuráveis por coluna do quadro (ex.: a coluna "Homologação" pode exigir que se informe a data em que foi homologado; a coluna "Pré-produção" pode exigir a data prevista de change), com suporte a pedir o valor novamente a cada vez que o cartão reentra naquela coluna.

Este documento é a especificação funcional completa da v1. Construa o sistema inteiro com base nela, tomando decisões de implementação razoáveis onde o documento não detalhar, mas sem cortar funcionalidade descrita aqui.

## Stack técnica e arquitetura geral

- **Blazor Server** (.NET 9) como framework principal, aproveitando o SignalR nativo do modelo de circuito para atualização em tempo real entre usuários.
- **Entity Framework Core** para persistência, com **soft-delete** (nenhum registro é apagado fisicamente por padrão — cartões, listas, quadros têm uma flag de exclusão lógica) e **auditoria automática** (data/usuário de criação e de última alteração em todas as entidades principais).
- **SignalR**: cada quadro aberto forma um grupo de conexão próprio, para que atualizações (mover cartão, editar campo, novo comentário) sejam propagadas só para quem está vendo aquele quadro específico, não para todos os usuários conectados ao sistema.
- Fique livre para escolher a biblioteca de drag-and-drop, componentes de UI, geração de PDF/Excel e envio de e-mail que julgar melhor — não há restrição de biblioteca nem convenção herdada de projeto anterior a seguir aqui. Prefira soluções simples e bem mantidas.
- Banco de dados relacional (SQL Server, alinhado ao restante do ambiente corporativo).

## Modelo de dados consolidado

Visão geral das entidades principais e como se relacionam. Sirva-se disso como guia — ajuste nomes e tipos ao seu padrão de EF Core.

| Entidade | Relacionamentos principais | Observações |
| --- | --- | --- |
| `Quadro` | N `Lista`; N `Sprint` | Um por time/projeto/frente de trabalho |
| `Lista` | N:1 `Quadro`; N:1 `Lista` (auto-FK `ListaPaiId`, sublistas); N `Cartao`; N `DefinicaoCampo`; campo `LimiteWip` (int, opcional) | Ordenável dentro do quadro/lista pai. Sublista tem sua própria configuração completa (campos, WIP) |
| `Cartao` | N:1 `Lista`; N:1 `Cartao` (auto-FK `CartaoPaiId`); N:1 `Sistema`; N:1 `Solicitante`; N:N `Desenvolvedor` (via `CartaoDesenvolvedor`, um marcado como principal); N `ItemTarefa`; N `ValorCampoCartao`; N `Comentario`; N `Reuniao`; N `Anexo`; N:N `Etiqueta`; N `HistoricoAtividade`; N `RelacaoCartao` (origem e destino); campos `DataInicio`, `Prazo`, `Prioridade`, `Estimativa`, `Titulo`, `Descricao` | Ordenável dentro da lista |
| `Sprint` | N:1 `Quadro`; N:N `Cartao` | Início/fim, meta da sprint |
| `DefinicaoCampo` | N:1 `Lista`; campos `Nome`, `Tipo` (texto/número/data/seleção/checkbox/área de texto), `Obrigatorio` (bool), `PedirNovamenteACadaEntrada` (bool) | Motor de campo configurável — ver seção própria |
| `ValorCampoCartao` | N:1 `Cartao`; N:1 `DefinicaoCampo`; campos `Valor`, `NumeroEntradaNaLista` (int), `DataPreenchimento` | Um registro por preenchimento; múltiplos quando `PedirNovamenteACadaEntrada` é verdadeiro |
| `ItemTarefa` | N:1 `Cartao`; N:1 `Desenvolvedor` (opcional, restrito aos desenvolvedores já vinculados ao cartão pai); N:1 `Cartao` (auto-FK `CartaoPromovidoId`, opcional); campos `Titulo`, `Concluido`, `Descricao` (opcional), N `Comentario`, N `Reuniao` | Substitui o checklist simples — funciona como checkbox leve ou, quando promovido, vira um novo `Cartao` filho |
| `Comentario` | N:1 `Cartao` OU N:1 `ItemTarefa`; campos `Autor`, `Texto`, `DataHora`, `Editado` (bool) | Suporta menção `@usuario` no texto |
| `Reuniao` | N:1 `Cartao` OU N:1 `ItemTarefa`; N `ReuniaoParticipante`; campos `Data`, `Ata` | Registro estruturado, não texto livre |
| `RelacaoCartao` | `CartaoOrigemId`, `CartaoDestinoId`, `Tipo` (enum: BloqueadoPor/Bloqueia, RelacionadoA, DuplicadoDe) | Vínculo horizontal, sem herança nem promoção |
| `Sistema` | Cadastro global: `Nome`, `Ativo` | Campo fixo do cartão, não parte do motor configurável |
| `Etiqueta` | N:N `Cartao` | Cor + nome |
| `Anexo` | N:1 `Cartao` | Ver seção de Anexos |
| `HistoricoAtividade` | N:1 `Cartao` | Trilha de auditoria: entrada/saída de lista, mudança de campo, quem e quando — usada para calcular badges de progresso, indicadores do dashboard e número de entrada de campos "por entrada" |
| `RegraAutomacao` | N:1 `Lista`/sublista; N `AcaoAutomacao` (mover / notificar / preencher campo) | Ver seção de Automação |
| `TemplateCartao`, `TemplateQuadro` | Associados a `Quadro` | Ver seção de Quadros |
| `Notificacao` | N:1 usuário destinatário; campos `Tipo`, `Lida` (bool), `CartaoOrigemId` | Ver seção de Notificações |

## Quadros, listas, sublistas e templates

- Suporte a **N quadros** e **N listas por quadro**, sem limite fixo no código.
- Listas são ordenáveis por arrastar e soltar, tanto entre si quanto os cartões dentro delas.
- **Sublistas**: uma lista pode ter N sublistas (auto-referência `ListaPaiId`). Cada sublista tem sua própria configuração completa — campos configuráveis, limite de WIP, posição — funcionando exatamente como qualquer lista, só que agrupada visualmente sob a lista pai. Exemplo real de uso: lista "Homologação" com sublistas "Pull Request" e "Validação do Usuário", cada uma com suas próprias regras. Um cartão pertence sempre à sublista (ou à lista, se ela não tiver sublistas) — a lista pai é só agrupador visual.
- **Limite de WIP por lista/sublista**: campo opcional `LimiteWip`. Quando o número de cartões ativos na lista ultrapassa o limite, a lista recebe sinalização visual (borda ou contador em destaque). Não bloqueia a entrada de novos cartões, só avisa.
- **Templates de quadro**: ao criar um quadro novo, oferecer a opção de partir de um template já configurado (listas, sublistas, campos, regras de automação prontos).
- **Templates de cartão**: título padrão, descrição padrão, campos pré-preenchidos, itens de tarefa padrão — associados a um quadro. Ao criar um cartão novo, oferecer escolha entre cartão em branco ou a partir de um template.

## Sprints, backlog e burndown

- Cada quadro tem um **backlog geral**, independente de sprint.
- **Sprints** têm início e fim definidos, uma meta descritiva, e cartões vinculados (N:N entre `Sprint` e `Cartao`).
- Ao criar uma sprint, permitir selecionar cartões do backlog para incluir nela.
- **Painel de sprint ativa**: visão separada do quadro Kanban livre, mostrando só os cartões da sprint em andamento.
- **Burndown**: pontos/estimativa restante × dias da sprint. Calcule a partir do `HistoricoAtividade` (quando um cartão saiu de "não concluído" para "concluído" e qual sprint estava vinculada), sem precisar de uma tabela separada de snapshot diário.
- **Fechamento de sprint**: cartões não concluídos voltam automaticamente para o backlog, ou o usuário escolhe manualmente movê-los para a próxima sprint.

## Motor de campos configuráveis (o diferencial do sistema)

Cada **Lista** (ou sublista) pode ter um conjunto de **`DefinicaoCampo`** associadas a ela, criadas pelo próprio usuário em tempo de uso — não fixas no código.

- **Definição de campo**: nome, tipo (texto, número, data, seleção/dropdown, checkbox, área de texto), se é obrigatório, e a qual lista/sublista está vinculado.
- Quando um cartão é movido para uma lista com campos configurados, esses campos passam a ser exibidos automaticamente no cartão.
- Exemplo real: lista "Homologação" → campo `Data Homologado` (tipo data, obrigatório); lista "Pré-produção" → campo `Data Prevista de Change` (tipo data, obrigatório).
- **Validação não bloqueante**: mover o cartão sem o campo obrigatório preenchido é permitido, mas gera um badge de alerta visível no cartão (não só um toast).
- **Campo "pedir novamente a cada entrada"** (opt-in por campo, desligado por padrão):
  - Campo normal: valor único por cartão, persiste entre entradas e saídas da lista, editável a qualquer momento.
  - Campo marcado com essa opção: a cada nova entrada do cartão naquela lista, o sistema pede o valor de novo, sem sobrescrever o anterior — cria um novo `ValorCampoCartao` com `NumeroEntradaNaLista` incrementado. Exemplo: `Data Homologado` marcado assim → 1ª entrada pede a data, homologação é reprovada, cartão volta; ao reentrar em "Homologação", pede a data de novo, e a data anterior continua no histórico, visível ao expandir o campo.
  - O "número da entrada" é derivado do `HistoricoAtividade` (trilha de quando o cartão entrou/saiu daquela lista), não precisa de contador duplicado em outro lugar.
- Renderize os campos dinamicamente: um componente que recebe a `DefinicaoCampo` e desenha o input correto conforme o tipo, sem precisar de tela nova a cada campo criado.

## Estrutura completa do cartão

### Linha do tempo unificada

Um único componente de timeline no cartão, misturando em ordem cronológica:

- **Comentários**: texto livre, autor, data/hora, marca de "editado" quando alterado.
- **Reuniões**: registro estruturado — data, lista de participantes, ata/decisões (texto), autor do registro. Visualmente diferenciado dos comentários (ícone/cor própria), com campo de busca próprio (ex.: filtrar cartões por reunião com determinado participante).
- **E-mails enviados** a partir do cartão (ver seção de Notificações e e-mail).
- **Menções `@usuario`** dentro de comentários e atas disparam notificação direcionada.
- Eventos de movimentação do cartão entre listas (derivados do `HistoricoAtividade`).

### Item de tarefa detalhável (checklist evoluído)

Substitui um checklist simples por um modelo com dois níveis de uso:

- **Modo simples (padrão)**: só título + checkbox de concluído, sem poluir o cartão.
- **Modo detalhado (opcional, por item)**: ao expandir, ganha descrição, comentários e reuniões próprios, isolados naquele item, sem virar cartão separado.
- **Promoção a cartão filho**: ação disponível a qualquer momento — cria um novo `Cartao` com `CartaoPaiId` apontando para o cartão atual. Herda automaticamente: título (do item), `Sistema`, Etiquetas e o `Desenvolvedor` específico que estava atuando naquela tarefa (não todos os desenvolvedores do cartão pai). Se o item já estava em modo detalhado, comentários e reuniões migram para o novo cartão. O item original passa a mostrar um link para o cartão filho, e seu progresso reflete o status do cartão filho.
- Cada item de tarefa pode ter um `Desenvolvedor` associado, **restrito à lista de desenvolvedores já vinculados ao cartão pai** (não ao quadro inteiro de usuários).

### Hierarquia pai/filho

- Campo opcional `CartaoPaiId` (auto-referência) — um cartão pode ter N cartões filhos.
- O filho pode estar em qualquer lista do mesmo quadro, não precisa estar na mesma lista do pai.
- Fora de escopo: herança automática de etiqueta/responsável do pai para um filho criado manualmente (só ocorre na promoção de item de tarefa, descrita acima).

### Relações horizontais entre cartões

- Entidade `RelacaoCartao`: origem, destino e tipo (`BloqueadoPor`/`Bloqueia`, `RelacionadoA`, `DuplicadoDe`).
- Diferente da hierarquia pai/filho — é só vínculo informativo, sem herança nem promoção.
- Cartão bloqueado por outro ainda não concluído ganha badge de alerta.

### Badges no cartão (estilo Trello)

Calculados a partir dos dados já existentes, sem tabela extra:

- **Prazo**: cor conforme está no prazo / próximo do vencimento / atrasado.
- **Checklist/itens de tarefa**: contagem "3/5".
- **Comentários**, **Reuniões**, **Anexos**: ícone + contagem.
- **Subtarefas**: "2/4 concluídas" quando o cartão tem filhos.
- **Campo pendente**: alerta quando falta preencher um campo obrigatório da lista atual.
- **Conflito de data**: alerta quando o cartão conflita com outro do mesmo desenvolvedor (ver seção de Visualizações e filtros).
- **Bloqueio**: alerta quando o cartão está bloqueado por outro não concluído (`RelacaoCartao`).

## Papéis fixos do cartão

- **Sistema**: cadastro próprio e global (`Nome`, `Ativo`), gerenciável em tela de administração. Campo fixo `SistemaId` no cartão (dropdown), não obrigatório por padrão. Diferente do motor de campo configurável porque é usado para filtro/relatório da mesma forma em todos os quadros.
- **Solicitante**: usuário de negócio que fez o pedido. Campo fixo `SolicitanteId`, referência a um usuário do sistema. Usado em filtros e como destinatário sugerido no e-mail de cobrança.
- **Desenvolvedores**: relação N:N (`CartaoDesenvolvedor`) — um cartão pode ter mais de um desenvolvedor vinculado, com um marcado como "principal" (usado como destinatário padrão de notificação/e-mail quando é preciso um único responsável). Usado no cálculo de conflito de data e carga de trabalho do dashboard, considerando todos os desenvolvedores vinculados.

## Visualizações e filtros

- **Board de cartões**: visão Kanban padrão, com drag-and-drop.
- **Visão em Lista/Tabela**: mesma base de dados do board, exibida como tabela filtrável e ordenável (colunas = campos do cartão, incluindo os configuráveis). Reaproveita componentes de tabela existentes.
- **Filtros combináveis** (aplicáveis tanto no board quanto na visão em Lista/Tabela): intervalo de data (prazo, início ou criação, selecionável), status/lista/sublista, Sistema, Solicitante, Desenvolvedor, Etiqueta.
- **"Somente meus cartões"**: atalho de um clique dentro do conjunto de filtros acima, filtrando por `Desenvolvedor = usuário logado` (considerando todos os desenvolvedores vinculados). Escopo apenas do quadro aberto no momento — não é uma tela nova nem cruza quadros.
- **Busca global**: campo de busca no topo do sistema, fora do contexto de um quadro específico, pesquisando por título, descrição e comentários em todos os quadros que o usuário tem acesso. Resultado mostra um trecho do conteúdo + link direto para o cartão, já abrindo no quadro/lista correta.
- **Conflito de data**: dois cartões do mesmo desenvolvedor (considerando todos os vinculados via `CartaoDesenvolvedor`) com intervalos `[DataInicio, Prazo]` sobrepostos disparam o badge de conflito nos dois cartões. Um cartão sem `DataInicio` conta como um intervalo de 1 dia no prazo, para fins de cálculo. O badge, ao clicar, mostra qual outro cartão está conflitando. Cálculo feito sob demanda (ao abrir o quadro ou quando datas mudam), sem job em background.

## Dashboard de indicadores e exportação

Todos os indicadores calculados a partir do `HistoricoAtividade` (trilha de entrada/saída de lista), sem tabela de métrica separada. O painel reaproveita os mesmos filtros de data/sistema/desenvolvedor da visão de board.

- **Lead time por lista/sublista**: tempo médio que os cartões passam em cada etapa.
- **Tempo de fila**: tempo médio entre o cartão ficar pronto para a próxima etapa e alguém de fato começar a mexer nele.
- **Tempo até início**: tempo médio entre a criação do cartão e a primeira movimentação para fora do backlog.
- **Atrasados/vencendo**: contagem e lista de cartões com prazo estourado ou próximo (mesmo cálculo do badge de prazo, agregado).
- **Volume por sistema**: quantidade de cartões abertos/concluídos por `Sistema`, com período selecionável.
- **Carga de trabalho por desenvolvedor**: quantidade de cartões ativos por desenvolvedor, cruzável com o alerta de conflito de data. Considere também detalhamento por item de tarefa, já que cada item pode ter seu próprio desenvolvedor.
- **Exportação**: botão de exportar tanto no dashboard quanto na visão em Lista/Tabela, gerando PDF ou Excel (planilha simples) dos dados filtrados na tela no momento.

## Automação por regras

- Entidade `RegraAutomacao`, vinculada a uma Lista ou sublista, com gatilho = "cartão entra nesta lista".
- **Ações suportadas** (uma regra pode ter mais de uma ação):
  - Mover o cartão para outra lista/sublista.
  - Notificar o responsável (mesmo canal da atribuição de cartão — ver Notificações).
  - Preencher/marcar um campo automaticamente (ex.: ao entrar em "Concluído", marcar `Data Homologado` com a data de hoje, se estiver vazio).
- Execução síncrona, no momento em que o cartão é movido — sem fila nem job em background na v1.
- Nota: vínculo automático com commit/Pull Request do GitHub (ex.: mover cartão automaticamente ao detectar PR mergeado referenciando o cartão) fica fora do escopo desta versão — ver seção "Fora de escopo".

## Notificações in-app, menções e e-mail

### Central de notificações in-app

Painel (sino) agregando: atribuição de cartão, menção `@usuario`, alerta de campo pendente, alerta de conflito de data, alerta de bloqueio (`RelacaoCartao`). Cada notificação marcada como lida/não lida, com link direto para o cartão de origem.

### Menções

Comentários e atas de reunião suportam `@nome`. Ao detectar, cria notificação direcionada para o usuário mencionado, que aparece na central de notificações e, opcionalmente (configurável por usuário), também dispara e-mail.

### E-mail de cobrança a partir do cartão

- Botão "Enviar e-mail" no cartão, abrindo uma composição de e-mail.
- Destinatário sugerido automaticamente: Solicitante ou Desenvolvedor do cartão (editável antes de enviar).
- Corpo do e-mail pode referenciar dados do cartão (título, prazo, link direto para o cartão) via template simples.
- Suporte a composição em HTML com sanitização, e um fluxo simples de rascunho antes de enviar.
- E-mails enviados ficam registrados na linha do tempo do cartão, junto com comentários e reuniões, para manter histórico de cobrança visível sem sair do sistema.

## Anexos em cartões

- Upload e download de arquivos vinculados ao cartão (`Anexo`).
- Parametrizável: extensões aceitas, tamanho máximo por arquivo e por conjunto total, estratégia de nome de arquivo e política de colisão de nomes.
- Compressão opcional no armazenamento (avalie o ganho real para formatos já comprimidos como PDF/JPG).
- Contagem de anexos aparece como badge no cartão (ver seção de Badges).

## Tempo real

- Cada quadro aberto forma um grupo de conexão SignalR próprio.
- Mudanças relevantes (cartão movido, campo editado, novo comentário/reunião/e-mail, notificação) são propagadas em tempo real para todos os usuários com aquele quadro aberto no momento, sem precisar recarregar a página.
- Usuários vendo um quadro diferente não recebem atualizações de quadros que não estão olhando.

## Requisitos não funcionais e ordem sugerida de construção

### Requisitos gerais

- Não há tela de login/autenticação nesta versão — assuma um usuário atual disponível via um mecanismo simples a definir (ex.: injeção de um `IUsuarioAtualProvider` fake ou seleção manual de usuário), de forma que a autenticação real possa ser plugada depois sem redesenhar o restante do sistema.
- Soft-delete e trilha de auditoria (quem criou, quem alterou, quando) em todas as entidades principais.
- Interface responsiva, pensada para desktop como uso principal.
- Priorize simplicidade de manutenção sobre otimização prematura — este é um sistema interno de porte médio (mais de 15 usuários), não uma plataforma multi-tenant de grande escala.

### Ordem sugerida de construção

1. Estrutura base: Quadro → Lista/Sublista → Cartão, drag-and-drop, CRUD básico.
2. Motor de campos configuráveis (`DefinicaoCampo` + `ValorCampoCartao`), incluindo a opção "pedir novamente a cada entrada".
3. Papéis fixos do cartão (Sistema, Solicitante, Desenvolvedores) e limite de WIP.
4. Linha do tempo do cartão: comentários, reuniões, itens de tarefa (com promoção a cartão filho), hierarquia pai/filho, relações horizontais.
5. Badges, conflito de data, filtros e visão em Lista/Tabela.
6. Sprint/backlog e burndown.
7. Automação por regras.
8. Notificações in-app, menções e e-mail.
9. Dashboard de indicadores e exportação.
10. Anexos, tempo real (SignalR), templates de quadro/cartão, busca global.

Essa ordem prioriza ter um quadro funcional cedo, incorporando as camadas de sofisticação (regras, notificações, relatórios) por cima de uma base já usável.

## Fora de escopo desta versão

- Login/autenticação real (Active Directory, SSO ou qualquer outro mecanismo) — fica para depois.
- Vínculo automático com commit/Pull Request do GitHub (fechamento automático de cartão via polling ou webhook na API do GitHub) — depende de validação de acesso de rede com infra/segurança da empresa, fora do controle deste sistema.
- Relatórios além dos indicadores já listados no dashboard (ex.: velocidade média entre sprints, cumulative flow diagram).
- Múltiplos quadros vinculados ou dependência formal entre quadros diferentes.

# Status de Implementação — Plano Original

**Data:** 28/09/2026 (atualizado — ver "Rodada de 28/09" e "Rodada de 27/09" logo abaixo)
**Projeto:** Sistema de Gestão de Demandas
**Status geral:** todas as funcionalidades do plano original estão implementadas. Uma auditoria em 27/09 encontrou lacunas que as rodadas de 26/09 não tinham registrado (data de início/estimativa sem tela, tabela sem ordenação/campos, tempo real parcial, e-mail sem sugestão/modelo/rascunho, item promovido sem acompanhar o filho, política de nomes de anexos); todas foram fechadas. O que resta é fora de escopo da v1 (login real, GitHub, relatórios avançados, dependências entre quadros, compressão de anexos, paginação), trocar a senha exposta do MySQL e a validação no navegador dos itens de 27/09.

## Rodada de 27/09 — ajustes de uso e fechamento das lacunas

Validação em navegador contra o MySQL real começou nesta rodada (pelo usuário) e revelou problemas corrigidos abaixo. Build da solução: **0 erros / 0 avisos**. Testes automatizados: **23 aprovados**.

**Correções encontradas no uso real**
- **Drag-and-drop não funcionava** (cursor de bloqueio): o board usava eventos de arraste do Blazor, que no modo Server chegam tarde demais para liberar o drop. Agora usa o helper JS de `App.razor` (define `dataTransfer` de forma síncrona) para cartões e listas, com destaque visual de coluna/posição.
- **Exceção intermitente ao mover e lentidão**: recarregamento concorrente do quadro. O board agora carrega com `DbContext` próprio, `AsSplitQuery()`, move o cartão na tela antes de gravar e agrupa avisos seguidos de atualização.
- **Selects mostravam IDs em vez de nomes** em todo o sistema (MudBlazor 8 exige `ToStringFunc`) — corrigido em todas as telas.

**Novidades pedidas pelo usuário**
- Serviço único de movimentação (`MovimentacaoCartaoService`) usado pelo board, detalhes e edição: histórico, automações, notificação de campo pendente e campos recorrentes em qualquer caminho.
- Lista do cartão editável nos Detalhes e na Edição.
- **Excluir cartão** (exclusão lógica, com confirmação) pelo menu do cartão e pelos Detalhes.
- **Detalhes em tela cheia**, com painel lateral de resumo (lista, sistema, desenvolvedores, solicitante, prioridade, início, prazo, estimativa, etiquetas).
- **Sublistas dentro da coluna pai**; lista com sublistas é só agrupadora (como pedia o plano): não recebe cartões, drop nela vai para a primeira sublista, e criar a primeira sublista move os cartões do pai para ela.
- **Caminho do quadro** (botão "Caminho"; coluna `Listas.OrdemFluxo`, migration `CaminhoListas`): define a ordem do fluxo, trava reordenar colunas contra ele e avisa (sem bloquear) quando o cartão não vai para a próxima etapa.
- **Cartão no estilo Trello**: título em destaque, etiquetas, sistema, desenvolvedores, campos da lista atual (valor mais recente; obrigatório vazio aparece como "pendente"), rodapé com prioridade/datas/contadores; ações em menu ⋮; tooltips em todos os botões.
- **Multisseleção com filtro** (`MultiSelecaoFiltro`): chips e digitação na mesma caixa, lista aberta enquanto se marca, check nos selecionados, "Limpar", teclado (↑ ↓ Enter Esc Backspace). Usada em desenvolvedores, etiquetas, participantes e cartões da sprint.
- Edição do cartão mostra os campos personalizados da lista (somente leitura).

**Lacunas do plano original fechadas**
- **Data de início e estimativa** agora editáveis (validação início ≤ prazo) e exibidas no cartão, tabela, detalhes e exportação — o conflito de data e o filtro por início passam a funcionar de fato.
- **Burndown por pontos de estimativa** (com fallback para contagem de cartões), linha ideal e gráfico.
- **Visão em Tabela** ordenável por todas as colunas, incluindo campos personalizados (campos de mesmo nome em listas diferentes viram uma coluna).
- **Exportação** respeita todos os filtros do board (lista, prioridade, etiqueta, "somente meus", intervalo por prazo/início/criação) e traz solicitante, desenvolvedores, etiquetas e datas.
- **Tempo real completo**: interceptor de `SaveChanges` (`AlteracoesTempoRealInterceptor`) avisa o quadro afetado em qualquer alteração (cartão, campos, comentários, reuniões, e-mails, anexos, relações, listas, etiquetas, sprints) e atualiza o sino e a tela de notificações do destinatário na hora.
- **Item de tarefa promovido** acompanha o status do cartão filho (concluído só enquanto o filho estiver em "Concluído"); o checkbox fica travado.
- **E-mail de cobrança**: destinatário sugerido (solicitante ou desenvolvedor, vários destinatários), modelo com marcadores `{titulo}`, `{prazo}`, `{lista}`, `{solicitante}`, `{link}`, etapa de rascunho/revisão antes de enviar; texto do usuário sempre codificado. O link abre o cartão direto no board (`/quadros/{id}?cartao={id}`).
- **Anexos**: `Anexos:EstrategiaNome` (`Guid`, `Original`, `CartaoOriginal`) e `Anexos:PoliticaColisao` (`Renomear`, `Sobrescrever`, `Rejeitar`), com normalização do nome contra path traversal.
- **Testes automatizados** (`tests/KanbanDemandas.Tests`, xUnit + EF InMemory): regras de caminho, listas agrupadoras, campos, movimentação (ordem, histórico, grupo → sublista, outro quadro, aviso de caminho, item promovido, notificação de campo pendente) e exclusão.

**Regras por etapa e vai-e-volta** (migration `RegrasEtapaEEstorno`; SQL em `database/scripts/2026-09-27_RegrasEtapaEEstorno.sql`)
- **Exigências da etapa** por coluna (engrenagem da coluna): campos fixos exigidos (desenvolvedor, prazo, estimativa, sistema, solicitante) e opção de **bloquear a entrada** até preencher. Sem bloqueio, o cartão entra, recebe "pendente" no cartão e os responsáveis são notificados.
- Ao mover (board, detalhes ou edição), um diálogo único de **entrada na etapa** pede o que falta: antes de mover (se bloqueia; cancelar = não move) ou depois (se só avisa). Substitui o antigo diálogo de campos recorrentes.
- **Campos automáticos**: um campo pode ser preenchido pelo sistema ao entrar na coluna (data de entrada ou usuário que moveu), com regra de reentrada (manter a primeira, atualizar para a última ou registrar nova ocorrência). Um campo de data pode **definir a Data de início do cartão** — ex.: "Em Andamento" com "Data de início" automática. "Início" deixou de aparecer em cartões que ainda não começaram.
- **Vai-e-volta**: voltar para a coluna de origem dentro da janela de correção (`Movimentacao:JanelaCorrecaoMinutos`, padrão 10 min, mesmo usuário) estorna o movimento: ele fica no banco marcado como `Estornado`, mas sai de métricas, linha do tempo e contagem de entradas, e os preenchimentos automáticos (inclusive a Data de início) são desfeitos. Movimentos fora da janela contam normalmente; as regras de reentrada evitam sobrescrever datas.
- Edição do cartão mostra os campos da coluna editáveis (automáticos somente leitura) e marca com * o que a etapa exige; criar um cartão direto numa coluna aplica os campos automáticos dela.
- Multisseleção limpa o texto digitado ao escolher um item.
- Testes: 30 aprovados (novos: data automática, Data de início, estorno, reentradas, pendências/bloqueio, atribuição pela etapa).

**Motor de automações** (migration `MotorAutomacoes`; SQL em `database/scripts/2026-09-28_MotorAutomacoes.sql`, que converte as regras antigas)
- Regras por quadro no formato **Quando → Se → Então**, com editor em tela cheia, frase-resumo da regra, validação, duplicar, pausar e 13 **modelos prontos**.
- **Gatilhos de evento** (detectados no `SaveChanges`, valem para qualquer tela): cartão criado, entrou/saiu de lista (lista-pai vale para as sublistas), campo alterado (prazo, início, prioridade, desenvolvedores, etiquetas, sistema, solicitante, estimativa, campo personalizado), desenvolvedor atribuído, comentário, checklist concluído, cartão bloqueado, conflito de datas.
- **Gatilhos de tempo** (processo em segundo plano, a cada `Automacao:IntervaloVerificacaoSegundos`): prazo próximo, prazo vencido (com repetição), cartão parado N dias, agendamento por horário e dias da semana, sprint iniciada, sprint terminando. Cada disparo acontece uma única vez (chave de disparo); opção de só rodar em horário comercial.
- **Condições** (E/OU): lista, prioridade, etiqueta, sistema, desenvolvedor, solicitante, campo personalizado (texto, número, data, checkbox), tem prazo, atrasado, bloqueado, dias na lista, estimativa, conflito de datas, prazo em risco, checklist completo, título, dias até o prazo.
- **Ações**: mover (movimento completo, respeitando bloqueio da etapa), notificar, e-mail (modelo com marcadores e link), preencher campo, atribuir (pessoa, menor carga, revezamento, solicitante, quem disparou), remover desenvolvedores, etiquetas, prioridade (fixa ou +1 nível), prazo/início relativo em dias úteis, checklist, cartão filho, comentário, alerta no cartão, resolver alertas, **resolver conflito de datas** (quem cede: menor prioridade, mais recente, mais antigo ou não iniciado; remarcar para o primeiro período livre mantendo a duração, mover para uma lista ou só avisar; modo sugerir ou aplicar) e **resumo por e-mail** personalizado por pessoa.
- **Proteções**: histórico de execuções com status, erros e e-mails; **desfazer** execução; **simular** regra antes de ativar e "executar agora"; anti-loop (profundidade máxima e uma regra não se redispara na própria cadeia); limite de e-mails por regra por hora; preferência por usuário "receber e-mails de automação"; ações em modo sugestão aguardam aprovação (aba Sugestões, painel do cartão e chip no board).
- Alertas de automação aparecem no cartão (clique resolve) e no painel de detalhes; execuções aparecem na linha do tempo do cartão.
- Configurações em `appsettings.json` → `Automacao` e `Aplicacao:UrlBase` (endereço usado nos links de e-mail; se vazio, usa o da primeira requisição recebida).
- Testes: 47 aprovados (17 novos cobrindo o motor, o interceptor de eventos e dias úteis).

**Parametrização, espera das automações, feriados, relatórios, compactação e paginação** (migration `ParametrizacaoFeriadosAnexos`; SQL em `database/scripts/2026-09-28_ParametrizacaoFeriadosAnexos.sql`)
- **Tela de Parametrização** (`/admin/parametros`): automações, movimentação, cartões, anexos e geral. Valores ficam na tabela `ParametrosSistema`, sobrepõem o appsettings e valem na hora (sem reiniciar); cada item mostra o padrão e pode ser restaurado.
- **Espera das automações** (`Automacao:AtrasoMinutos`, padrão 10): eventos gerados por pessoas ficam na tabela `EventosAutomacaoPendentes` e só executam quando o cartão fica esse tempo sem nova alteração (cada alteração reinicia a espera). Na execução, o motor confere se a situação ainda vale (ex.: o cartão continua na lista em que entrou), então um vai-e-volta dentro da espera não dispara nada. Eventos gerados pelas próprias automações seguem imediatos. Aba "Na fila" em Automações mostra, antecipa ou descarta pendentes.
- **Descrição no cartão**: exibida abaixo do título, limitada a `Cartao:LinhasDescricao` linhas (padrão 2) com reticências; pode ser desligada.
- **Feriados** (`/admin/feriados`): cadastro de feriados nacionais, estaduais, municipais, pontos facultativos e recessos, com opção "repete todo ano"; importação dos nacionais pela BrasilAPI ou por cálculo local (Páscoa e móveis). Dias úteis passam a descontar os feriados em prazos relativos, remarcação de conflitos, prazo em risco e horário comercial (parâmetro `Calendario:ConsiderarFeriados`).
- **Relatórios avançados** (`/relatorios`): fluxo acumulado (CFD), vazão semanal, lead time e tempo de ciclo com percentis P50/P85/P95 e histograma, envelhecimento do trabalho em andamento, previsão de entrega por Monte Carlo (quando termina / quantos até uma data), cumprimento de prazos por sistema e por desenvolvedor e desempenho por desenvolvedor. Filtros de quadro, período, sistema e desenvolvedor; exportação em Excel de cada relatório; paleta categórica validada para daltonismo, com tabela ao lado de cada gráfico.
- **Compactação de anexos**: gzip automático dos formatos configurados quando o ganho passa do mínimo (download devolve o original); botão para compactar anexos existentes na Parametrização, com estatística de espaço.
- **Paginação**: no servidor para busca global, notificações e histórico de automações; na tela para tabela do board, dashboard, sprints, cadastros, feriados e relatórios; colunas do board com "Mostrar mais" acima de `Quadro:CartoesPorColuna`; linha do tempo e comentários do cartão com "Carregar mais". Tamanho de página em `Paginacao:ItensPorPagina`.
- Testes: 68 aprovados.

**Correção de documentação**: a aplicação **não** aplica migrations ao iniciar (o método existe, mas não é chamado). Os scripts incrementais em `database/scripts` precisam ser executados a cada versão — ver `database/README.md`.

**Rodada de 28/09 — portal de solicitações, GitHub e Teams** (migration `PortalGitTeams`; SQL em `database/scripts/2026-09-28_PortalGitTeams.sql`)
- **Portal de solicitações** (projeto separado `src/KanbanDemandas.Portal`, mesmo banco): o solicitante vê as solicitações em que é solicitante, abre novas (título, tipo, sistema, urgência, descrição, anexos), acompanha status e conversa com o time. Identificação pelo mesmo seletor provisório de usuário (login fica para depois). Guia completo em `docs/portal.md`.
- **Status público por lista** (Parametrização › Portal): o solicitante vê o status da última lista com status configurado; listas internas sem status não aparecem para ele. Status inicial configurável; estornos ficam fora da linha do tempo.
- **O que o solicitante vê é configurável**: prazo, início, prioridade, sistema, desenvolvedores, estimativa, etiquetas, anexos do time, histórico de status e conversa; também liga/desliga envio de anexos e mensagens.
- **Comentários públicos**: no detalhe do cartão, "Visível ao solicitante" publica o comentário no portal (selo "público"). Mensagens do solicitante viram comentários públicos e notificam os desenvolvedores.
- **E-mail ao solicitante** na mudança de status público (depois da espera das automações, então vai-e-volta não gera e-mail) e em respostas públicas, com link para o portal.
- **Código do cartão** (`KB-123`, prefixo em `Cartao:PrefixoCodigo`) no board, detalhes, portal e e-mails.
- **GitHub (corporativo)** por webhook com assinatura HMAC, sem Entra ID: commits, branches e pull requests que citam o código viram vínculos na aba "Código" do cartão e na linha do tempo; chip de PR no cartão; novos gatilhos "Commit vinculado", "Pull request aberto" e "Pull request mergeado" (ex.: PR mergeado → mover para Homologação). Guia em `docs/integracoes/github.md`.
- **Microsoft Teams** por Workflows/webhook de canal (sem Entra ID nem app registrado): ações "Postar no canal do Teams" e "Resumo no canal do Teams", com cartão adaptável, botão "Testar envio", limite por regra/hora e URL tratada como segredo. Guia em `docs/integracoes/teams.md`.
- Testes: 82 aprovados (novos: código do cartão, status público, portal — criação, visibilidade, mensagens, concluídas —, aviso ao solicitante, webhook do GitHub e envio ao Teams).

**Ajustes de 28/09 (uso real)**
- Tarefa criada aparecia duplicada (o EF já inclui a entidade nova na coleção do cartão rastreado; a tela incluía de novo) — corrigido também para comentários, anexos, reuniões e e-mails.
- Exclusão (lógica, com confirmação) de tarefas (com seus comentários e reuniões), comentários (pelo autor), comentários e reuniões de tarefa, reuniões, anexos e relações.
- Detalhes do cartão: colunas não estouram mais a largura da tela; seção expandida da tarefa reorganizada; tooltips nos botões da aba Tarefas; Enter adiciona tarefa.
- Feriados: seletor de ano repetia o último ano (closure do `for`). Relatórios › Previsão: aviso de "sem entregas" ficava cortado.
- Portal: erros de compilação no Visual Studio causados por `switch` com padrões `<` dentro do `@code` (o editor Razor lê como tag) — movido para C#.
- **Campos não permitidos por etapa** (migration `CamposBloqueadosEtapa`; SQL em `database/scripts/2026-09-28_CamposBloqueadosEtapa.sql`): na engrenagem da coluna, marque o que o cartão não pode ter ali (desenvolvedor, prazo, data de início, estimativa, sistema, solicitante). Na edição esses campos ficam desabilitados; automações não os preenchem (a ação fica como erro); ao mover para a lista, a tela avisa e, confirmando, remove os dados com registro no histórico. Ao salvar a regra, oferece limpar os cartões que já estão na lista. O solicitante de solicitações do portal nunca é removido.
- Testes: 89 aprovados.

**Ainda pendente**
- Validar no navegador os itens desta rodada (em especial multisseleção, tempo real entre dois navegadores, e-mail com SMTP real e exportação filtrada).
- Trocar a senha do MySQL que ficou exposta antes da correção de 26/09.
- Rodar `database/scripts/2026-09-28_PortalGitTeams.sql`, configurar Parametrização › Portal (quadro, lista de entrada, status por lista) e validar o portal no navegador junto com o sistema principal.
- Login real e permissões (adiado a pedido do usuário) — necessário antes de expor o portal fora da rede interna.
- Banco: aplicar a migration `CaminhoListas` (automática na inicialização, ou `ALTER TABLE Listas ADD OrdemFluxo int NULL` + registro em `__EFMigrationsHistory`).

## Resumo executivo

O sistema deixou de ser apenas um esqueleto de entidades e passou a ter um fluxo funcional de quadro, cartões, campos configuráveis, colaboração, sprints, automações, dashboard e exportações.

A persistência foi ajustada para **MySQL 8.0+** usando `Pomelo.EntityFrameworkCore.MySql`. A migration inicial e os scripts SQL MySQL foram recriados.

O build atual está aprovado:

- `0` erros.
- `0` avisos.
- Migration MySQL gerada: `20260924032807_InitialCreate`.
- Script MySQL completo com 26 tabelas.

A aplicação ainda não foi executada contra um servidor MySQL real nem validada em navegador nesta rodada — as mudanças abaixo foram verificadas apenas por compilação (`dotnet build`, 0 erros/0 avisos), não por teste funcional.

**Segurança:** a connection string com credencial real (IP interno, usuário e senha em texto plano) que estava em `appsettings.json` foi removida e movida para `dotnet user-secrets` (escopo local, fora do controle de versão); `appsettings.json` agora tem apenas um placeholder (`kanban/change-me`). Recomenda-se fortemente **trocar essa senha** no servidor MySQL, já que ela ficou exposta em texto plano no arquivo de configuração até esta correção.

## Rodada de 26/09 — o que foi adicionado

A pedido do usuário, o sistema foi retomado usando o prompt original como referência principal, fechando lacunas identificadas em auditoria prévia:

- **Drag-and-drop real**: arrastar um cartão agora permite soltá-lo em uma posição específica (antes de outro cartão), tanto dentro da mesma lista quanto entre listas — não apenas mover para o fim. Listas também podem ser reordenadas por arraste (pelo cabeçalho), além dos botões subir/descer que já existiam.
- **Etiquetas completas**: diálogo de gerenciamento por quadro (criar/excluir, nome + cor), atribuição no cartão (criação e edição), exibição como badge colorido no board, filtro por etiqueta, e herança das etiquetas do cartão pai ao promover um item de tarefa.
- **Filtros do board completos**: além de lista/sistema/prioridade já existentes, foram adicionados Desenvolvedor, Solicitante, Etiqueta, intervalo de data (por prazo, início ou criação) e o atalho "Somente meus cartões".
- **Conflito de data com detalhe**: clicar no badge "Conflito de data" mostra quais outros cartões estão conflitando.
- **Menções `@usuario` em atas de reunião**: antes só funcionava em comentários; agora reuniões também geram notificação. Quando o usuário mencionado tem `NotificarPorEmail` ativado, um e-mail é disparado (best-effort; falha de e-mail não quebra o fluxo).
- **Sanitização de e-mail**: o corpo do e-mail de cobrança agora tem o texto do usuário HTML-encodado antes de virar HTML, evitando injeção de HTML/script.
- **Validação de anexos por configuração**: extensão permitida, tamanho máximo por arquivo e tamanho total por cartão agora respeitam `Anexos:*` do `appsettings.json` (antes só havia um limite de 20 MB fixo no código).
- **Checklist em modo detalhado**: itens de tarefa agora podem ser expandidos para editar descrição própria e ter comentários e reuniões próprios, isolados do cartão. O item promovido a cartão filho passa a mostrar um botão "Ver cartão filho".
- **Fechamento de sprint com escolha manual**: ao fechar uma sprint, o usuário escolhe entre devolver os cartões não concluídos ao backlog ou movê-los para outra sprint específica.
- **Tela de administração de automações** (`/admin/automacoes`): antes `RegraAutomacao`/`AcaoAutomacao` só existiam como entidades sem nenhuma UI; agora é possível criar regras por lista com ações de mover cartão, notificar responsável e preencher campo.
- **Templates de quadro realmente aplicados**: ao criar um quadro a partir de um template, a `EstruturaJson` (listas, sublistas, campos, automações) é interpretada e aplicada de fato — antes o campo existia mas nunca era lido.
- **Templates de cartão com campos padrão**: `CamposPadrao` (JSON nome→valor) agora é aplicado ao criar um cartão a partir de um template; a tela de administração de templates ganhou campos para cadastrar itens de tarefa padrão e campos padrão em JSON (antes só existiam no modelo, sem UI para preenchê-los).
- **Número de entrada de campo recorrente via histórico**: campos com "pedir novamente a cada entrada" agora derivam o número da entrada a partir do `HistoricoAtividade` (contagem de vezes que o cartão entrou naquela lista), como pedia a especificação — antes era um contador baseado na quantidade de registros de valor já salvos.
- **Edição de comentários**: comentários agora podem ser editados pelo autor, marcando `Editado = true` e exibindo "(editado)".
- **Busca global melhorada**: o trecho retornado agora é uma janela de texto ao redor do termo buscado (com reticências), e clicar no resultado abre o cartão diretamente em um diálogo de detalhe, em vez de só navegar para o quadro.

## Rodada de 26/09 — parte 2 (fechamento do plano)

A pedido do usuário ("seguir o plano até o final"), todas as pendências restantes de prioridade média/alta foram endereçadas:

- **Dashboard completo**: filtros de Sistema, Desenvolvedor, Solicitante e período (criação) adicionados; novos indicadores — lead time médio por lista (derivado do `HistoricoAtividade`), tempo de fila por lista (tempo até a primeira atividade registrada após entrar na lista), tempo até início (criação → primeira saída do backlog) e carga de trabalho por desenvolvedor (cartões ativos, itens de tarefa ativos e cruzamento com conflito de data).
- **Filtros aplicados às exportações**: os endpoints `/api/export/cartoes.xlsx` e `.pdf` agora aceitam `desenvolvedorId`, `solicitanteId`, `quadroId` e período, além de `busca`/`sistemaId`. A visão em Tabela do board também ganhou botões de exportar Excel/PDF (antes só existiam no Dashboard).
- **Burndown por histórico real**: a data de conclusão de cada cartão da sprint agora é a primeira vez que ele entrou em uma lista "Concluído" segundo o `HistoricoAtividade`, não mais `Cartao.AlteradoEm` (que podia refletir qualquer edição, não necessariamente a conclusão).
- **Solicitação imediata de campo recorrente**: ao mover um cartão para uma lista com campo "pedir novamente a cada entrada", um diálogo abre automaticamente pedindo o preenchimento, em vez de depender do usuário abrir o cartão manualmente.
- **Migração de comentários/reuniões na promoção**: se o item de tarefa já estava em modo detalhado, seus comentários e reuniões agora são movidos para o cartão filho criado na promoção, como pedia a especificação.
- **Busca de reuniões por participante**: a busca global ganhou um filtro "Reunião com participante", combinável com o texto pesquisado.
- **Linha do tempo unificada**: nova aba "Linha do tempo" no detalhe do cartão, mesclando comentários, reuniões, e-mails enviados e movimentações em ordem cronológica única (usando `MudTimeline`), como descrito na seção "Estrutura completa do cartão" da especificação — antes cada um vivia isolado em sua própria aba.
- **Desenvolvedor principal explícito**: em vez de inferir o "principal" pela ordem de um `HashSet` (que não preserva ordem de clique e já causava imprecisão), agora há um seletor explícito "Desenvolvedor principal" quando mais de um desenvolvedor é vinculado.
- **Badges que faltavam no card do board**: contagem de reuniões e "X/Y subtarefas concluídas" (baseado em `CartosFilhos`) — a especificação pedia os dois e só a contagem de comentários/anexos existia.
- **Correção de bug pré-existente na direção do bloqueio**: `RelacaoCartao` é sempre gravada com origem = cartão que estava aberto na hora do vínculo. A checagem de "cartão bloqueado" consultava a direção errada (mostrava o badge no cartão contrário ao que realmente estava bloqueado, e um `NullReferenceException` latente por faltar Include da lista do cartão de origem). Corrigido, incluindo o caso `Bloqueia` (que aponta o bloqueio para o cartão destino, não o de origem).
- **Notificações que nunca eram criadas**: dos 5 tipos de notificação previstos no sino (atribuição de cartão, menção, campo pendente, conflito de data, bloqueio), só "menção" e automação disparavam algo — os outros 4 existiam como enum mas nunca eram instanciados. Agora: atribuir um desenvolvedor a um cartão notifica; mover um cartão para uma lista com campo obrigatório pendente notifica os desenvolvedores vinculados; salvar um cartão com data que gera conflito notifica (com deduplicação para não repetir enquanto a notificação anterior não for lida); criar uma relação `BloqueadoPor`/`Bloqueia` notifica o cartão que ficou bloqueado.
- **Clique na notificação abre o cartão diretamente** (antes só navegava para o quadro).
- **Flag de "backlog" configurável na UI**: antes só existia via seed/template; `DialogNovaLista` agora tem um switch "É o backlog do quadro" (só uma lista por quadro pode ser marcada), com um chip "Backlog" exibido no cabeçalho da lista no board.
- **Segurança do endpoint de anexos**: passou a exigir que o anexo e o cartão dono não estejam excluídos logicamente (soft-delete), e um aviso é logado na inicialização quando a aplicação roda em `Production` sem autenticação real.

### Deliberadamente fora de escopo (confirmado consistente com a especificação original)

- Autorização de download de anexo por usuário/permissão — depende de login real, que a especificação original já lista como fora de escopo da v1.
- Login/autenticação real (AD/SSO), integração automática com GitHub, relatórios além dos indicadores definidos, dependências formais entre quadros, compressão de anexos, paginação para grandes volumes.

### O que ainda falta antes de considerar pronto para produção

- **Validação funcional em navegador e contra um MySQL real** — todas as mudanças desta sessão (duas rodadas) foram verificadas apenas por `dotnet build` (0 erros/0 avisos), nunca executadas de fato. É o item de maior risco: lógica de drag-and-drop, diálogos novos (Etiquetas, Automações, Campos Recorrentes) e os novos cálculos de indicadores precisam ser testados clicando de verdade.
- **Trocar a senha real do MySQL** que ficou exposta em `appsettings.json` antes da correção de segurança desta sessão (a connection string real está hoje em `dotnet user-secrets`, mas a senha antiga deveria ser considerada comprometida).
- Testes automatizados (nenhum teste unitário/integração foi criado; todo o projeto depende de validação manual).

## O que foi implementado

### Infraestrutura e banco

- Solução alinhada em .NET 9.
- Entity Framework Core com provider MySQL via Pomelo.
- Soft-delete nas entidades principais.
- Auditoria de criação e alteração.
- Interceptor de auditoria.
- Factory de design-time do `DbContext`.
- Migration inicial MySQL.
- Script SQL completo para banco vazio.
- Script SQL idempotente para implantação.
- Seed inicial de usuários, sistemas, quadro, listas, campos e etiquetas.
- Armazenamento local de anexos.
- Serviço de e-mail via MailKit.
- Serviço de exportação Excel e PDF.

Arquivos principais:

- `database/scripts/KanbanDemandas.MySql.sql`
- `database/scripts/KanbanDemandas.MySql.idempotent.sql`
- `database/README.md`
- `src/KanbanDemandas.Infrastructure/Data/Migrations/20260924032807_InitialCreate.cs`

### Quadros, listas e cartões

- CRUD básico de quadros.
- Board Kanban funcional.
- Criação de listas e sublistas.
- Limite WIP visual, sem bloqueio de entrada.
- Criação e edição de cartões.
- Drag-and-drop entre listas.
- Reordenação de cartões dentro da lista.
- Reordenação de listas por controles de subir/descer.
- Prazo e prioridade.
- Sistema e solicitante no cartão.
- Desenvolvedores vinculados em relação N:N.
- Desenvolvedor principal definido pelo primeiro selecionado.
- Busca rápida por título e descrição.
- Visão Board/Tabela.
- Filtros por lista, sistema e prioridade.

### Campos configuráveis

- Campos por lista ou sublista.
- Tipos texto, número, data, seleção, checkbox e área de texto.
- Campos obrigatórios.
- Opção de pedir novamente a cada entrada.
- Cadastro e exclusão lógica de definições de campo.
- Renderização dinâmica no detalhe do cartão.
- Persistência de valores por cartão.
- Badge de campo obrigatório pendente.

### Detalhe e colaboração do cartão

- Descrição editável.
- Checklist evoluído.
- Comentários.
- Menções `@usuario` em comentários gerando notificações.
- Reuniões estruturadas com data, ata e participantes.
- Upload e download de anexos.
- Limite de 20 MB por arquivo.
- Relações horizontais entre cartões:
  - `RelacionadoA`.
  - `BloqueadoPor`.
  - `Bloqueia`.
  - `DuplicadoDe`.
- Badge de cartão bloqueado.
- Promoção de item de tarefa a cartão filho.
- Herança de sistema e desenvolvedor do item promovido.
- Hierarquia pai/filho.
- Envio de e-mail pelo cartão.
- Registro de e-mail enviado ou com erro.

### Sprints

- Cadastro de sprint por quadro.
- Nome, período, meta e status.
- Vínculo de cartões a uma sprint.
- Painel da sprint.
- Indicadores de cartões, concluídos e restantes.
- Burndown simplificado.
- Fechamento de sprint.
- Retorno de cartões não concluídos ao backlog.

### Automações, notificações e tempo real

- Regras automáticas executadas ao entrar em uma lista.
- Ações de mover cartão, notificar responsável e preencher campo.
- Tokens `{hoje}` e `{titulo}` em preenchimento automático.
- Central de notificações in-app.
- Marcação individual e em lote como lida.
- Hub SignalR por quadro.
- Cliente SignalR conectado ao board.
- Atualização do quadro após movimentações.

### Dashboard, busca e templates

- Dashboard com cartões ativos, atrasados, concluídos e lead time médio.
- Volume por sistema.
- Exportação Excel.
- Exportação PDF.
- Filtros de busca e sistema aplicados às exportações.
- Busca global por título, descrição e comentários.
- Templates de quadro.
- Templates de cartão.
- Aplicação de título e descrição padrão na criação do cartão.
- Aplicação de itens de tarefa padrão definidos em JSON.

## Pendências atuais

### Prioridade alta

1. **Executar a aplicação contra um MySQL real**
   - Criar o banco e o usuário conforme `database/README.md`.
   - Substituir `change-me` por uma senha real fora do controle de versão.
   - Aplicar `KanbanDemandas.MySql.idempotent.sql` ou executar `dotnet ef database update`.
   - Validar o startup, seed e operações CRUD no banco real.

2. **Validar a aplicação no navegador**
   - Abrir a página inicial.
   - Criar quadro, lista, sublista e cartão.
   - Testar drag-and-drop e reordenação.
   - Testar campos configuráveis e campos recorrentes.
   - Testar comentários, reuniões, anexos, e-mail e notificações.
   - Testar SignalR com dois circuitos/navegadores.

3. **Corrigir limitações de segurança e operação**
   - ~~Remover credenciais placeholder/reais da configuração versionada~~ — feito em 26/09 (movida para `dotnet user-secrets`); ainda falta **trocar a senha real** que ficou exposta antes da correção.
   - Autorização para download de anexos por usuário — **fora de escopo enquanto não houver login real** (a especificação já define isso como fora da v1); o endpoint agora ao menos valida que o anexo/cartão não estão excluídos.
   - ~~Validar tamanho total de anexos, extensões permitidas~~ — feito em 26/09, falta apenas validação de colisão/normalização de nomes de arquivo (baixo risco, já que o nome de armazenamento é um GUID).
   - ~~Sanitizar HTML de e-mails antes do envio~~ — feito em 26/09 (HTML-encode do corpo digitado pelo usuário).
   - ~~Evitar que o usuário fake seja usado inadvertidamente em produção~~ — mitigado com aviso de log na inicialização em ambiente `Production`; prevenção completa depende de login real (fora de escopo da v1).

### Prioridade média — todas resolvidas em 26/09 (ver "Rodada de 26/09" e "parte 2" acima)

~~Filtros por período/desenvolvedor/solicitante no Dashboard~~ · ~~Filtros aplicados às exportações~~ · ~~Lead time por lista/sublista~~ · ~~Tempo de fila e tempo até início~~ · ~~Carga de trabalho por desenvolvedor e item de tarefa~~ · ~~Conflito de data com detalhamento ao clicar~~ · ~~Número de entrada de campos recorrentes via histórico~~ · ~~Solicitação imediata de campo recorrente~~ · ~~Fechamento de sprint com escolha manual~~ · ~~Burndown por transições históricas~~ · ~~Tela administrativa de automações~~ · ~~Histórico visual unificado~~ · ~~Promoção de tarefa migrando comentários/reuniões~~ · ~~Link para cartão filho no item original~~ · ~~Templates de quadro e de cartão aplicados~~ · ~~Busca global com trecho preciso e abertura direta~~ · ~~Reordenação por arrastar e soltar (cartões e listas)~~.

### Prioridade baixa ou fora do primeiro fechamento

- Tela de autenticação real, AD/SSO.
- Integração automática com GitHub, commits e Pull Requests.
- Dependências formais entre quadros.

## Próximos passos recomendados

### Passo 1 — Preparar o ambiente MySQL

1. Criar banco e usuário.
2. Atualizar a connection string por variável de ambiente ou configuração segura.
3. Executar o script idempotente.
4. Confirmar as 26 tabelas e o seed.

### Passo 2 — Validar o fluxo principal no navegador

1. Criar quadro.
2. Criar listas e sublistas.
3. Criar cartão com sistema, solicitante e desenvolvedores.
4. Mover e reordenar cartão.
5. Conferir histórico e badges.
6. Criar campo obrigatório e testar o preenchimento.

### Passo 3 — Testar colaboração

1. Abrir o mesmo quadro em dois navegadores.
2. Mover um cartão em um navegador.
3. Confirmar atualização automática no outro.
4. Adicionar comentário com menção.
5. Conferir a notificação do usuário mencionado.
6. Testar anexo, relação, reunião e e-mail.

### Passo 4 — Refinar regras de negócio

1. Finalizar cálculo histórico dos campos recorrentes.
2. Completar fechamento de sprint.
3. Completar dashboard com filtros de período e desenvolvedor.
4. Adicionar autorização e validações de segurança.
5. Criar testes automatizados para movimentação, campos, sprints e automações.

### Passo 5 — Preparar entrega

1. Configurar ambiente MySQL de homologação.
2. Executar migration/script em homologação.
3. Fazer teste funcional guiado pelos requisitos do prompt original.
4. Corrigir problemas encontrados no navegador.
5. Criar checklist de deploy e backup.
6. Só então preparar autenticação real.

## Comandos de validação

```bash
dotnet restore KanbanDemandas.slnx
dotnet build KanbanDemandas.slnx --no-restore
dotnet test tests/KanbanDemandas.Tests
dotnet ef migrations list --project src/KanbanDemandas.Infrastructure --startup-project src/KanbanDemandas.Web --framework net9.0
dotnet ef database update --project src/KanbanDemandas.Infrastructure --startup-project src/KanbanDemandas.Web --framework net9.0
```

## Observação final

O projeto está tecnicamente compilável e possui uma base funcional ampla, mas ainda não deve ser considerado pronto para produção antes da conexão com um MySQL real, da validação no navegador e da revisão de segurança das credenciais, anexos, e-mails e ausência de autenticação real.

# Portal de Solicitações

Aplicação separada (`src/KanbanDemandas.Portal`) onde quem pede demandas abre solicitações e acompanha o andamento, sem acesso ao quadro. Usa o **mesmo banco** do sistema principal: cada solicitação é um cartão do quadro configurado, com o solicitante preenchido.

> **Identificação provisória.** Como no sistema principal, ainda não há login: o usuário é escolhido no ícone do canto superior direito. Não publique o portal fora da rede interna antes de o login real entrar. Em `Production` o portal registra um aviso no log ao iniciar.

## O que o solicitante faz

| Tela | Endereço | O que tem |
|---|---|---|
| Minhas solicitações | `/` | Todas as solicitações em que ele é o solicitante (abertas pelo portal ou cadastradas pelo time), com código, tipo, status público, abertura e última atualização. Busca por código/título, filtro por status, "ocultar concluídas" e paginação. |
| Nova solicitação | `/nova` | Título, tipo (lista configurável), sistema, urgência (opcional), descrição e anexos. |
| Detalhe | `/solicitacoes/{id}` | Status atual, informações liberadas pelo admin, descrição, conversa com o time, anexos (enviar e baixar) e linha do tempo de status. |

Um usuário só consegue abrir as solicitações em que é o solicitante; as demais aparecem como "não encontrada".

## Como uma solicitação vira cartão

1. O cartão é criado no quadro `Portal:QuadroId`, na lista `Portal:ListaEntradaId`. Se essa lista tiver sublistas, entra na primeira delas.
2. Recebe `OrigemPortal = true` (chip "Portal" no board e nos detalhes), o solicitante, o tipo, o sistema e a urgência (vira a prioridade).
3. Os campos automáticos da lista de entrada são aplicados, como em qualquer criação.
4. As automações do quadro rodam normalmente — o gatilho "Cartão criado" vale para solicitações do portal. O portal não executa automações: grava o evento na fila do banco (`EventosAutomacaoPendentes`) e o **sistema principal** executa depois da espera configurada (`Automacao:AtrasoMinutos`). Por isso o sistema principal precisa estar rodando.

## Status público

O solicitante não vê o nome das colunas. Cada lista pode ter um **status público** (Parametrização › Portal › "Status por lista"):

- Lista com status: quando o cartão entra nela, o solicitante passa a ver esse status.
- Lista sem status: o solicitante continua vendo o último status público — movimentações internas (ex.: Code Review → Testes) ficam invisíveis.
- Antes de qualquer lista com status, vale o **status inicial** (`Portal:StatusInicial`, padrão "Recebida").
- Movimentos estornados (vai-e-volta dentro da janela de correção) não entram na linha do tempo.
- A solicitação aparece como concluída quando o cartão está na lista "Concluído".

Exemplo de mapeamento:

| Lista | Status público |
|---|---|
| Backlog | *(vazio → "Recebida")* |
| Triagem | Em análise |
| A Fazer | Na fila de desenvolvimento |
| Em Andamento, Code Review, Testes | Em desenvolvimento *(só na primeira; as demais podem ficar vazias)* |
| Homologação | Aguardando sua validação |
| Concluído | Entregue |

## O que o solicitante vê (configurável)

Em Parametrização › Portal › "Informações visíveis ao solicitante":

| Opção | Efeito |
|---|---|
| Prazo | "Previsão de entrega" no detalhe |
| Data de início | Início do trabalho |
| Prioridade, Sistema, Estimativa, Etiquetas | Mostra o valor atual |
| Desenvolvedores responsáveis | Nomes dos desenvolvedores |
| Anexos adicionados pelo time | Sem esta opção, ele só vê os anexos que ele mesmo enviou |
| Histórico de status | Linha do tempo "Andamento" |
| Conversa (comentários públicos) | Mostra a conversa e permite responder (se "Permitir mensagens" também estiver ligado) |

Título, descrição, tipo e status atual sempre aparecem.

## Conversa com o time

- No sistema principal, ao comentar num cartão que tem solicitante, ligue **"Visível ao solicitante"**. O comentário ganha o selo "público" e aparece no portal. Comentários sem essa opção continuam internos.
- Mensagens enviadas pelo solicitante no portal entram como comentários públicos e geram notificação (sino) para os desenvolvedores do cartão.
- Depois que a solicitação é concluída, o portal deixa de aceitar mensagens e anexos.

## Avisos por e-mail ao solicitante

Enviados pelo sistema principal (usa o SMTP configurado lá), se `Portal:NotificarSolicitante` estiver ligado e o usuário aceitar e-mails de automação:

- **Mudança de status público** — processada junto com as automações, depois da espera; um vai-e-volta dentro da espera não gera e-mail, e mover entre listas sem status também não.
- **Resposta pública do time** — na hora em que o comentário é publicado.

Os e-mails trazem o link `{Portal:UrlBase}/solicitacoes/{id}`. Sem `Portal:UrlBase`, o e-mail vai sem link.

## Configuração

### No sistema principal (Parametrização › Portal)

| Parâmetro | Para que serve |
|---|---|
| `Portal:QuadroId` / `Portal:ListaEntradaId` | Onde as solicitações entram. Enquanto não forem escolhidos, o portal mostra "ainda não configurado" e não aceita novas solicitações. |
| `Portal:UrlBase` | Endereço público do portal (links dos e-mails). |
| `Portal:StatusInicial` | Status antes da primeira lista com status público. |
| `Portal:TiposSolicitacao` | Tipos oferecidos, separados por vírgula. Vazio = campo não aparece. |
| `Portal:CamposVisiveis` | Informações liberadas (tabela acima). |
| `Portal:PermitirAnexos` / `Portal:PermitirComentarios` | Liga/desliga envio de anexos e mensagens pelo solicitante. |
| `Portal:NotificarSolicitante` | Liga/desliga os e-mails ao solicitante. |
| `Cartao:PrefixoCodigo` | Prefixo do código exibido (padrão `KB` → `KB-123`). |

Os limites e tipos de anexo são os mesmos do sistema principal (`Anexos:*`).

### No portal (`src/KanbanDemandas.Portal/appsettings.json`)

| Chave | Valor |
|---|---|
| `ConnectionStrings:DefaultConnection` | O **mesmo banco** do sistema principal. Em desenvolvimento, o portal usa o mesmo `UserSecretsId`, então a connection string de `dotnet user-secrets` já vale para os dois. |
| `Portal:UrlSistemaPrincipal` | Endereço do sistema principal (ex.: `https://localhost:7245`). O portal avisa o hub de tempo real dele quando cria cartões ou mensagens, para quadros e sinos abertos atualizarem na hora. Se estiver fora do ar, nada se perde: os dados já estão no banco e aparecem na próxima atualização. |
| `Anexos:PastaBase` | **A mesma pasta física** de anexos do sistema principal, para que os dois enxerguem os mesmos arquivos. O padrão (`../KanbanDemandas.Web/uploads`) funciona rodando os dois com `dotnet run` a partir das pastas dos projetos. Em servidor, use um caminho absoluto (ou compartilhamento de rede) igual nos dois `appsettings`. |

Os parâmetros gravados na tela de Parametrização (tabela `ParametrosSistema`) valem também para o portal, sem reiniciar.

## Executando

```bash
# terminal 1 — sistema principal
dotnet run --project src/KanbanDemandas.Web
# terminal 2 — portal
dotnet run --project src/KanbanDemandas.Portal
```

Portal: `https://localhost:7310` (ou `http://localhost:5310`).

No Visual Studio: botão direito na solução › "Configurar Projetos de Inicialização" › "Vários projetos de inicialização" › `KanbanDemandas.Web` e `KanbanDemandas.Portal` como "Iniciar".

## Segurança e limites atuais

- Sem login real: qualquer pessoa com acesso ao endereço pode escolher qualquer usuário. Quando o login entrar, basta trocar o seletor pelo usuário autenticado — o `PortalService` já recebe o id do usuário em todas as operações e filtra tudo por ele.
- Downloads passam pelo servidor, que confere se o anexo pertence a uma solicitação do usuário e se ele pode vê-lo.
- O método `AvisarAlteracao` do hub do sistema principal (usado pelo portal) só dispara recarregamento de telas; nenhum dado trafega por ele.

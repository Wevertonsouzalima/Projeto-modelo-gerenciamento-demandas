# Integração com o Microsoft Teams

O sistema posta mensagens em **canais** do Teams por meio de automações. A integração **não usa Entra ID**:
ela funciona com um endereço de webhook gerado no próprio canal.

## O que dá para fazer

| Ação de automação | O que posta | Quando usar |
|---|---|---|
| **Postar no canal do Teams** | Um cartão com título, mensagem, lista, prazo, prioridade, desenvolvedores, solicitante e o botão **Abrir cartão** | Avisar o canal sobre um cartão específico (entrou em Homologação, venceu, ficou bloqueado, PR mergeado…) |
| **Postar resumo do quadro no Teams** | Os números do quadro (em andamento, atrasados, vencendo em 3 dias, bloqueados, em conflito, parados há 5+ dias), os 10 atrasados mais antigos e o botão **Abrir quadro** | Resumo periódico para o time, com o gatilho **Agendamento** |

**O que não é possível:** enviar mensagem direta (chat 1:1) para uma pessoa. Isso exige um aplicativo
registrado no Entra ID (Microsoft Graph ou bot) e está fora desta integração. Para avisos individuais,
use as ações **Enviar notificação** (sino do sistema) e **Enviar e-mail**.

## Passo a passo: gerar a URL do webhook no canal

A Microsoft está descontinuando os antigos conectores do Office 365 ("Incoming Webhook"). O caminho atual
é o app **Workflows** (Fluxos de trabalho) do próprio Teams:

1. No Teams, vá até o **canal** que vai receber as mensagens.
2. Clique em **⋯ (Mais opções)** ao lado do nome do canal e escolha **Workflows** (ou **Fluxos de trabalho**).
3. Escolha o modelo **"Postar em um canal quando uma solicitação de webhook for recebida"**
   (*Post to a channel when a webhook request is received*).
4. Dê um nome ao fluxo (ex.: "Kanban – avisos"), confirme a conta e clique em **Avançar**.
5. Confirme a **equipe** e o **canal** e clique em **Adicionar fluxo de trabalho**.
6. Copie a **URL** exibida no final. Ela começa com `https://` e é longa.

> **Recomendação:** crie o fluxo com uma **conta de serviço** (ex.: `kanban@empresa.com`). O fluxo pertence a
> quem o criou; se essa pessoa sair da empresa ou perder a licença, as mensagens param.

URLs antigas do conector "Incoming Webhook" (terminam em `webhook.office.com`) continuam funcionando
enquanto a Microsoft mantiver o serviço, mas devem ser trocadas por uma URL do Workflows.

## Configurar a regra no sistema

1. Abra **Administração › Automações**, escolha o quadro e crie uma regra, ou use os modelos prontos
   **"Resumo diário no canal do Teams"** e **"Avisar o canal do Teams"**.
2. Em **Então**, adicione a ação **Postar no canal do Teams** ou **Postar resumo do quadro no Teams**.
3. Cole a URL no campo **URL do webhook do canal** e clique em **Testar envio**. Uma mensagem de teste deve
   aparecer no canal em alguns segundos.
4. Ajuste o **Título** e a **Mensagem**. Os dois aceitam marcadores:
   `{codigo}`, `{titulo}`, `{lista}`, `{prazo}`, `{inicio}`, `{prioridade}`, `{desenvolvedores}`,
   `{solicitante}`, `{sistema}`, `{dias_na_lista}`, `{conflitos}`, `{hoje}`, `{regra}` e `{link}`.
   Sem título, o padrão é `{codigo} · {titulo}` para cartões e `Resumo do quadro — {hoje}` para o resumo.
5. Salve e use **Simular** para ver quais cartões seriam afetados antes de ativar.

### Exemplos

- **Entrega em homologação:** Quando *um cartão entrar em "Homologação"* → *Postar no canal do Teams*
  com a mensagem "{titulo} está pronto para teste. Solicitante: {solicitante}".
- **Resumo diário:** Quando *for 09:00 em dias úteis* → *Postar resumo do quadro no Teams*.
- **PR mergeado:** Quando *um pull request for mergeado (GitHub)* → *Mover para "Homologação"* e
  *Postar no canal do Teams*.
- **Atraso:** Quando *o prazo vencer* → *Postar no canal do Teams* com a mensagem "Atrasado desde {prazo}".

## Como funciona por dentro

- A mensagem é um **Adaptive Card** (versão 1.4), formato aceito tanto pelo Workflows quanto pelo antigo
  "Incoming Webhook".
- O envio acontece no servidor do sistema, no mesmo processamento das automações. Ele respeita a
  **espera das automações** (Parametrização › Automações) e aparece no **Histórico** da regra com a
  contagem de mensagens.
- O botão **Abrir cartão / Abrir quadro** usa o **Endereço do sistema** (Parametrização › Geral). Sem
  esse endereço configurado, o botão só aparece depois do primeiro acesso ao sistema após iniciar.
- **Limite:** cada regra posta no máximo o valor de *Limite de mensagens no Teams por regra por hora*
  (padrão 100; Parametrização › Automações). O Teams também limita cerca de 4 mensagens por segundo por
  webhook e 28 KB por mensagem.
- Se o Teams recusar a mensagem, a ação aparece com erro no Histórico e as demais ações da regra
  continuam.

## Segurança

- A URL do webhook funciona como **senha**: quem a tem consegue postar no canal. No editor ela fica
  mascarada, e nas descrições da regra aparece só o domínio.
- Para revogar o acesso, exclua ou desative o fluxo no app Workflows do canal e gere uma nova URL.

## Rede

O servidor do sistema precisa de **saída HTTPS (porta 443)** para os domínios do Power Automate e do Teams:

- `*.logic.azure.com`
- `*.environment.api.powerplatform.com`
- `*.webhook.office.com` (apenas URLs antigas)

## Solução de problemas

| Sintoma no Histórico | Causa provável | O que fazer |
|---|---|---|
| `o Teams recusou a mensagem (400)` | Formato recusado pelo fluxo | Confira se o modelo do fluxo é "Postar em um canal quando uma solicitação de webhook for recebida" |
| `(401)` / `(403)` | Fluxo desativado ou dono sem acesso ao canal | Reative o fluxo ou recrie-o com a conta de serviço |
| `(404)` | Fluxo excluído | Gere uma nova URL e atualize a regra |
| Tempo esgotado / erro de conexão | Firewall ou proxy bloqueando a saída | Libere os domínios da seção **Rede** |
| `limite de ... mensagens no Teams por hora` | Regra disparando demais | Revise as condições da regra ou ajuste o limite |
| Mensagem sem o botão "Abrir" | Endereço do sistema desconhecido | Preencha **Endereço do sistema** na Parametrização |

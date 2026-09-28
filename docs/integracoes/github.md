# Integração com o GitHub (corporativo)

Branches, commits e pull requests que citam o **código do cartão** (ex.: `KB-123`) aparecem no cartão e
podem disparar automações, como mover o cartão para "Homologação" quando o PR é mergeado. A integração usa
**webhooks do GitHub** assinados com um segredo. Não depende de Entra ID nem de token de acesso.

Vale para GitHub Enterprise Cloud (github.com) e GitHub Enterprise Server (instalação própria).

## Como o vínculo é feito

O código do cartão aparece no board (acima do título) e nos detalhes (chip com `#`). O prefixo é
configurável em **Parametrização › Cartões › Prefixo do código do cartão** (padrão `KB`).

| Onde citar | Exemplo | Resultado no cartão |
|---|---|---|
| Nome da branch | `feature/KB-123-login-sso` | Branch vinculada; todo commit enviado nela também é vinculado |
| Mensagem do commit | `KB-123: corrige validação do CPF` | Commit vinculado |
| Título, descrição ou branch do PR | `KB-123 Login com SSO` | Pull request vinculado, com estado (aberto, fechado, mergeado) |

- Maiúsculas e minúsculas tanto faz, e são aceitos `KB-123`, `KB_123`, `KB 123` e `kb123`.
- Um mesmo commit ou PR pode citar vários cartões (`KB-12 KB-15`).
- Códigos de cartões inexistentes ou excluídos são ignorados.

Os vínculos aparecem na aba **Código** dos detalhes do cartão (com link para o GitHub), na **linha do tempo**
(pull requests) e como chip **PR aberto / mergeado / fechado** no cartão do board.

## Gatilhos de automação

| Gatilho | Quando dispara |
|---|---|
| **Commit citou o cartão (GitHub)** | A cada push com commits que citam o cartão |
| **Pull request aberto (GitHub)** | PR aberto ou reaberto citando o cartão |
| **Pull request mergeado (GitHub)** | PR citando o cartão foi mergeado (fechar sem merge não dispara) |

Assim como os demais eventos, respeitam a **espera das automações** (Parametrização › Automações).

Exemplos de regra:

- Quando **um pull request for mergeado** → *Mover cartão* para "Homologação" → *Notificar* o solicitante →
  *Postar no canal do Teams*.
- Quando **um pull request for aberto** → *Mover cartão* para "Code Review" → *Atribuir* o revisor.
- Quando **um commit citar o cartão**, **se** o cartão estiver em "A Fazer" → *Mover* para "Em Andamento".

## Passo a passo

### 1. No sistema

1. **Parametrização › Geral › Endereço do sistema**: informe o endereço pelo qual o GitHub alcança o
   sistema (ex.: `https://kanban.suaempresa.com.br`).
2. **Parametrização › Integrações**:
   - clique no ícone de chave para **gerar um segredo** (ou digite um com 16 caracteres ou mais) e copie-o;
   - ligue **Receber webhooks do GitHub**;
   - **Salvar alterações**.
3. Copie o endereço exibido na aba (**Payload URL**): `https://kanban.suaempresa.com.br/api/integracoes/github`.

### 2. No GitHub

Configure na **organização** (vale para todos os repositórios) ou em cada **repositório**:

- Organização: **Settings › Webhooks › Add webhook** (exige ser owner da organização).
- Repositório: **Settings › Webhooks › Add webhook** (exige ser admin do repositório).

Preencha:

| Campo | Valor |
|---|---|
| Payload URL | o endereço copiado no passo 1 |
| Content type | `application/json` |
| Secret | o mesmo segredo configurado no sistema |
| SSL verification | **Enable** (mantenha, a menos que o certificado seja interno; veja abaixo) |
| Which events? | **Let me select individual events** → marque **Pushes**, **Pull requests** e **Branch or tag creation** |
| Active | marcado |

Ao salvar, o GitHub envia um `ping`. Em **Recent Deliveries**, a resposta deve ser **200** com `"pong"`.

### 3. Teste

1. Crie uma branch `teste/KB-<número de um cartão>` e envie um commit.
2. Abra o cartão: a aba **Código** deve mostrar a branch e o commit.
3. Abra um PR com o código no título: o chip **PR aberto** aparece no board.

## Rede e certificado

- **GitHub Enterprise Server na rede interna:** o servidor do GitHub precisa alcançar o sistema por HTTPS.
  Se o certificado do sistema for emitido por uma CA interna, cadastre a CA no GitHub Enterprise Server ou,
  como último recurso, desative a verificação SSL do webhook.
- **GitHub Enterprise Cloud (github.com):** o GitHub chama a partir da internet. O endereço do sistema
  precisa estar publicado (proxy reverso ou gateway). Se possível, restrinja a entrada às faixas de IP de
  webhooks do GitHub (lista `hooks` em `https://api.github.com/meta`).

## Segurança

- Toda chamada precisa da assinatura `X-Hub-Signature-256` (HMAC-SHA256 do corpo com o segredo). Chamadas
  sem assinatura ou com segredo errado recebem **401** e não gravam nada.
- Com a integração desligada, o endereço responde **404**.
- Para trocar o segredo: gere um novo na Parametrização, salve e atualize o campo **Secret** no GitHub.
- O sistema grava apenas título, link, autor e estado de commits, branches e PRs. **Nenhum código-fonte é
  lido ou armazenado.**

## Solução de problemas (GitHub › Webhooks › Recent Deliveries)

| Resposta | Causa | O que fazer |
|---|---|---|
| 404 | Integração desligada | Ligue **Receber webhooks do GitHub** e salve |
| 401 | Segredo diferente | Copie o segredo da Parametrização para o campo **Secret** do webhook |
| 503 | Segredo não configurado | Gere e salve o segredo na Parametrização |
| Timeout / "failed to connect" | GitHub não alcança o sistema | Veja a seção **Rede e certificado** |
| 200 com `"vinculos": 0` | Nenhum código de cartão reconhecido | Confira o prefixo configurado e se o cartão existe |

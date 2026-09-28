using KanbanDemandas.Core.Entities;
using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Interfaces;
using KanbanDemandas.Infrastructure.Data;
using KanbanDemandas.Core.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KanbanDemandas.Infrastructure.Services;

public sealed record ResultadoMovimentacao(
    bool MudouDeLista, int ListaDestinoId, string ListaDestinoNome, string? AvisoCaminho, bool Estornado = false,
    CamposFixosCartao CamposLimpos = CamposFixosCartao.Nenhum);

public sealed class MovimentacaoCartaoService(
    KanbanDbContext db,
    IUsuarioAtualProvider usuarioProvider,
    IConfiguration configuracao)
{
    private TimeSpan JanelaCorrecao => TimeSpan.FromMinutes(int.TryParse(configuracao["Movimentacao:JanelaCorrecaoMinutos"], out var minutos) ? Math.Max(0, minutos) : 10);

    /// <summary>
    /// O que falta para o cartão cumprir a etapa da lista (resolvida para a sublista que recebe cartões).
    /// Só considera campos manuais: os automáticos são preenchidos pelo próprio movimento.
    /// </summary>
    public async Task<PendenciasEtapa?> ObterPendenciasAsync(int cartaoId, int listaDestinoId)
    {
        var cartao = await db.Cartoes.AsNoTracking().Include(c => c.Lista).Include(c => c.Desenvolvedores)
            .FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        if (cartao is null) return null;
        var listasQuadro = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId && !l.Excluido).ToListAsync();
        var listaId = ListasQuadro.PrimeiraFolha(listaDestinoId, listasQuadro);
        var lista = await db.Listas.AsNoTracking().Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
            .FirstOrDefaultAsync(l => l.Id == listaId && l.QuadroId == cartao.Lista.QuadroId);
        if (lista is null) return null;

        var manuais = lista.DefinicoesCampo.Where(RegrasEtapa.EhManual).OrderBy(c => c.Ordem).ToList();
        var valores = await db.ValoresCampoCartao.AsNoTracking().Where(v => v.CartaoId == cartaoId).ToListAsync();
        var entraAgora = cartao.ListaId != lista.Id;

        var obrigatoriosVazios = manuais
            .Where(c => c.Obrigatorio && (c.PedirNovamenteACadaEntrada && entraAgora
                || string.IsNullOrWhiteSpace(CamposCartao.ValorMaisRecente(valores, c.Id))))
            .ToList();
        var recorrentes = entraAgora ? manuais.Where(c => c.PedirNovamenteACadaEntrada).ToList() : [];

        return new PendenciasEtapa(lista.Id, lista.Nome, lista.BloquearEntradaComPendencias,
            RegrasEtapa.FixosFaltando(cartao, lista), obrigatoriosVazios, recorrentes,
            entraAgora ? RegrasEtapa.BloqueadosPreenchidos(cartao, lista) : CamposFixosCartao.Nenhum);
    }

    /// <summary>
    /// Move o cartão para a lista de destino, posicionando-o antes de <paramref name="cartaoAlvoId"/>
    /// (ou no fim da lista quando nulo). Retorna nulo se o cartão ou a lista não forem válidos.
    /// Voltar para a lista de origem logo após um movimento é tratado como correção: o movimento anterior é
    /// estornado e seus preenchimentos automáticos desfeitos, para o vai-e-volta não sujar histórico e métricas.
    /// Regras de automação da lista de destino disparam pelos eventos gerados ao gravar.
    /// </summary>
    public async Task<ResultadoMovimentacao?> MoverAsync(int cartaoId, int listaDestinoId, int? cartaoAlvoId, bool permitirEstorno = true)
    {
        if (cartaoAlvoId == cartaoId) return null;

        var cartao = await db.Cartoes.Include(c => c.Lista).FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        if (cartao is null) return null;

        var listasQuadro = await db.Listas.AsNoTracking().Where(l => l.QuadroId == cartao.Lista.QuadroId && !l.Excluido).ToListAsync();
        var folhaDestino = ListasQuadro.PrimeiraFolha(listaDestinoId, listasQuadro);
        if (folhaDestino != listaDestinoId)
        {
            listaDestinoId = folhaDestino;
            cartaoAlvoId = null;
        }

        var listaDestino = await db.Listas
            .Include(l => l.DefinicoesCampo.Where(d => !d.Excluido))
            .FirstOrDefaultAsync(l => l.Id == listaDestinoId && !l.Excluido && l.QuadroId == cartao.Lista.QuadroId);
        if (listaDestino is null) return null;

        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
        var origemListaId = cartao.ListaId;
        var moveuDeLista = origemListaId != listaDestino.Id;

        var destino = await db.Cartoes
            .Where(c => c.ListaId == listaDestino.Id && !c.Excluido && c.Id != cartaoId)
            .OrderBy(c => c.Ordem).ToListAsync();
        var indice = cartaoAlvoId.HasValue ? destino.FindIndex(c => c.Id == cartaoAlvoId.Value) : -1;
        destino.Insert(indice < 0 ? destino.Count : indice, cartao);
        for (var i = 0; i < destino.Count; i++) destino[i].Ordem = i;

        var estornado = false;
        var limpos = CamposFixosCartao.Nenhum;
        if (moveuDeLista)
        {
            cartao.ListaId = listaDestino.Id;

            var restantes = await db.Cartoes
                .Where(c => c.ListaId == origemListaId && !c.Excluido && c.Id != cartaoId)
                .OrderBy(c => c.Ordem).ToListAsync();
            for (var i = 0; i < restantes.Count; i++) restantes[i].Ordem = i;

            var movimentoAnterior = await db.HistoricoAtividades
                .Where(h => h.CartaoId == cartaoId && h.Tipo == TipoHistoricoAtividade.CartaoMovido)
                .OrderByDescending(h => h.OcorridoEm).ThenByDescending(h => h.Id)
                .FirstOrDefaultAsync();
            estornado = permitirEstorno && movimentoAnterior is not null
                && movimentoAnterior.ListaOrigemId == listaDestino.Id && movimentoAnterior.ListaDestinoId == origemListaId
                && movimentoAnterior.UsuarioId == usuarioId
                && DateTime.UtcNow - movimentoAnterior.OcorridoEm <= JanelaCorrecao;

            if (estornado)
            {
                await EstornarAsync(movimentoAnterior!, cartao);
            }
            else
            {
                var numeroEntrada = await db.HistoricoAtividades.CountAsync(h => h.CartaoId == cartaoId && h.ListaDestinoId == listaDestino.Id
                    && (h.Tipo == TipoHistoricoAtividade.CartaoMovido || h.Tipo == TipoHistoricoAtividade.CartaoCriado)) + 1;
                var entrada = new HistoricoAtividade
                {
                    CartaoId = cartaoId, Tipo = TipoHistoricoAtividade.CartaoMovido,
                    ListaOrigemId = origemListaId, ListaDestinoId = listaDestino.Id,
                    Descricao = $"Cartão movido para {listaDestino.Nome}.", UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
                };
                db.HistoricoAtividades.Add(entrada);
                await AplicarPreenchimentoAutomaticoAsync(cartao, listaDestino, entrada, numeroEntrada);
                await NotificarPendenciasAsync(cartao, listaDestino);
            }
            limpos = await LimparBloqueadosAsync(cartao, listaDestino, usuarioId);
            await SincronizarItemPromovidoAsync(cartao, listasQuadro);
        }

        await db.SaveChangesAsync();

        var aviso = moveuDeLista && !estornado ? CaminhoQuadro.AvisoMovimentacao(origemListaId, listaDestino.Id, listasQuadro) : null;

        return new ResultadoMovimentacao(moveuDeLista, listaDestino.Id, listaDestino.Nome, aviso, estornado, limpos);
    }

    /// <summary>Aplica os campos automáticos da lista em que o cartão acabou de ser criado.</summary>
    public async Task AplicarEntradaNaCriacaoAsync(int cartaoId)
    {
        var cartao = await db.Cartoes.FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        var criacao = await db.HistoricoAtividades
            .FirstOrDefaultAsync(h => h.CartaoId == cartaoId && h.Tipo == TipoHistoricoAtividade.CartaoCriado);
        if (cartao is null || criacao is null) return;
        var lista = await db.Listas.Include(l => l.DefinicoesCampo.Where(d => !d.Excluido)).FirstAsync(l => l.Id == cartao.ListaId);
        await AplicarPreenchimentoAutomaticoAsync(cartao, lista, criacao, 1);
        await LimparBloqueadosAsync(cartao, lista, usuarioProvider.ObterIdUsuarioAtual());
        await NotificarPendenciasAsync(cartao, lista);
        await db.SaveChangesAsync();
    }

    /// <summary>Quantos cartões da lista têm campos que ela não permite (para oferecer a limpeza ao mudar a regra).</summary>
    public async Task<int> ContarComBloqueadosAsync(int listaId)
    {
        var lista = await db.Listas.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listaId);
        if (lista is null || lista.CamposFixosBloqueados == CamposFixosCartao.Nenhum) return 0;
        var cartoes = await db.Cartoes.AsNoTracking().Include(c => c.Desenvolvedores).Where(c => c.ListaId == listaId && !c.Excluido).ToListAsync();
        return cartoes.Count(c => RegrasEtapa.BloqueadosPreenchidos(c, lista) != CamposFixosCartao.Nenhum);
    }

    /// <summary>Limpa, nos cartões que já estão na lista, os campos que ela não permite. Retorna quantos cartões mudaram.</summary>
    public async Task<int> LimparBloqueadosDaListaAsync(int listaId)
    {
        var lista = await db.Listas.FirstOrDefaultAsync(l => l.Id == listaId);
        if (lista is null) return 0;
        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
        var alterados = 0;
        foreach (var cartao in await db.Cartoes.Where(c => c.ListaId == listaId && !c.Excluido).ToListAsync())
            if (await LimparBloqueadosAsync(cartao, lista, usuarioId) != CamposFixosCartao.Nenhum) alterados++;
        await db.SaveChangesAsync();
        return alterados;
    }

    /// <summary>
    /// Remove do cartão o que a lista não permite (ex.: desenvolvedor no Backlog) e registra no histórico.
    /// O solicitante de uma solicitação do portal nunca é removido (ele deixaria de enxergá-la).
    /// </summary>
    private async Task<CamposFixosCartao> LimparBloqueadosAsync(Cartao cartao, Lista lista, int usuarioId)
    {
        var bloqueados = lista.CamposFixosBloqueados;
        if (bloqueados == CamposFixosCartao.Nenhum) return CamposFixosCartao.Nenhum;
        var limpos = CamposFixosCartao.Nenhum;
        if (bloqueados.HasFlag(CamposFixosCartao.Desenvolvedor))
        {
            var desenvolvedores = await db.CartaoDesenvolvedores.Where(d => d.CartaoId == cartao.Id).ToListAsync();
            if (desenvolvedores.Count > 0)
            {
                db.CartaoDesenvolvedores.RemoveRange(desenvolvedores);
                limpos |= CamposFixosCartao.Desenvolvedor;
            }
        }
        if (bloqueados.HasFlag(CamposFixosCartao.Prazo) && cartao.Prazo.HasValue) { cartao.Prazo = null; limpos |= CamposFixosCartao.Prazo; }
        if (bloqueados.HasFlag(CamposFixosCartao.DataInicio) && cartao.DataInicio.HasValue) { cartao.DataInicio = null; limpos |= CamposFixosCartao.DataInicio; }
        if (bloqueados.HasFlag(CamposFixosCartao.Estimativa) && cartao.Estimativa.HasValue) { cartao.Estimativa = null; limpos |= CamposFixosCartao.Estimativa; }
        if (bloqueados.HasFlag(CamposFixosCartao.Sistema) && cartao.SistemaId.HasValue) { cartao.SistemaId = null; limpos |= CamposFixosCartao.Sistema; }
        if (bloqueados.HasFlag(CamposFixosCartao.Solicitante) && cartao.SolicitanteId.HasValue && !cartao.OrigemPortal)
        {
            cartao.SolicitanteId = null;
            limpos |= CamposFixosCartao.Solicitante;
        }
        if (limpos != CamposFixosCartao.Nenhum)
        {
            db.HistoricoAtividades.Add(new HistoricoAtividade
            {
                CartaoId = cartao.Id, Tipo = TipoHistoricoAtividade.CampoAlterado,
                Descricao = $"Removido ao entrar em {lista.Nome} (não permitido nesta etapa): {RegrasEtapa.Descrever(limpos)}.",
                UsuarioId = usuarioId, OcorridoEm = DateTime.UtcNow
            });
        }
        return limpos;
    }

    /// <summary>Exclusão lógica do cartão; retorna false se ele não existir.</summary>
    public async Task<bool> ExcluirAsync(int cartaoId)
    {
        var cartao = await db.Cartoes.FirstOrDefaultAsync(c => c.Id == cartaoId && !c.Excluido);
        if (cartao is null) return false;

        cartao.Excluido = true;
        cartao.ExcluidoEm = DateTime.UtcNow;
        cartao.ExcluidoPorId = usuarioProvider.ObterIdUsuarioAtual();

        var restantes = await db.Cartoes
            .Where(c => c.ListaId == cartao.ListaId && !c.Excluido && c.Id != cartaoId)
            .OrderBy(c => c.Ordem).ToListAsync();
        for (var i = 0; i < restantes.Count; i++) restantes[i].Ordem = i;

        await db.SaveChangesAsync();
        return true;
    }

    private async Task AplicarPreenchimentoAutomaticoAsync(Cartao cartao, Lista lista, HistoricoAtividade entrada, int numeroEntrada)
    {
        var automaticos = lista.DefinicoesCampo.Where(c => !c.Excluido && !RegrasEtapa.EhManual(c)).ToList();
        if (automaticos.Count == 0) return;
        var usuario = usuarioProvider.ObterUsuarioAtual();

        foreach (var campo in automaticos)
        {
            var valor = campo.Preenchimento == PreenchimentoAutomatico.DataEntrada
                ? DateTime.Now.ToString("yyyy-MM-dd")
                : usuario.Nome;
            var atual = await db.ValoresCampoCartao
                .Where(v => v.CartaoId == cartao.Id && v.DefinicaoCampoId == campo.Id)
                .OrderByDescending(v => v.NumeroEntradaNaLista).ThenByDescending(v => v.DataPreenchimento)
                .FirstOrDefaultAsync();

            var aplicou = campo.RegraReentrada switch
            {
                RegraReentrada.ManterPrimeiro when !string.IsNullOrWhiteSpace(atual?.Valor) => false,
                RegraReentrada.NovoRegistro => Criar(),
                _ when atual is null => Criar(),
                _ => Atualizar(atual)
            };

            if (aplicou && campo.DefineDataInicioCartao && campo.Tipo == TipoCampo.Data
                && (campo.RegraReentrada != RegraReentrada.ManterPrimeiro || !cartao.DataInicio.HasValue))
            {
                entrada.AlterouDataInicio = true;
                entrada.DataInicioAnterior = cartao.DataInicio;
                cartao.DataInicio = DateTime.Today.ToUniversalTime();
            }

            bool Criar()
            {
                db.ValoresCampoCartao.Add(new ValorCampoCartao
                {
                    CartaoId = cartao.Id, DefinicaoCampoId = campo.Id, Valor = valor, NumeroEntradaNaLista = numeroEntrada,
                    DataPreenchimento = DateTime.UtcNow, PreenchidoPorId = usuario.Id, HistoricoOrigem = entrada,
                    CriadoAutomaticamente = true, CriadoPorId = usuario.Id, CriadoEm = DateTime.UtcNow
                });
                return true;
            }

            bool Atualizar(ValorCampoCartao existente)
            {
                existente.ValorAnteriorAutomatico = existente.Valor;
                existente.Valor = valor;
                existente.DataPreenchimento = DateTime.UtcNow;
                existente.PreenchidoPorId = usuario.Id;
                existente.HistoricoOrigem = entrada;
                existente.CriadoAutomaticamente = false;
                return true;
            }
        }
    }

    /// <summary>Desfaz o movimento anterior: marca como estornado e reverte o que ele preencheu automaticamente.</summary>
    private async Task EstornarAsync(HistoricoAtividade movimento, Cartao cartao)
    {
        movimento.Estornado = true;
        var preenchidos = await db.ValoresCampoCartao.Where(v => v.HistoricoOrigemId == movimento.Id).ToListAsync();
        foreach (var valor in preenchidos)
        {
            if (valor.CriadoAutomaticamente)
            {
                valor.Excluido = true;
                valor.ExcluidoEm = DateTime.UtcNow;
                valor.ExcluidoPorId = usuarioProvider.ObterIdUsuarioAtual();
            }
            else
            {
                valor.Valor = valor.ValorAnteriorAutomatico;
                valor.ValorAnteriorAutomatico = null;
            }
            valor.HistoricoOrigemId = null;
        }
        if (movimento.AlterouDataInicio) cartao.DataInicio = movimento.DataInicioAnterior;
    }

    /// <summary>Avisa os responsáveis quando o cartão entra numa etapa sem o que ela exige.</summary>
    private async Task NotificarPendenciasAsync(Cartao cartao, Lista lista)
    {
        var desenvolvedores = await db.CartaoDesenvolvedores.Where(d => d.CartaoId == cartao.Id).Select(d => d.UsuarioId).ToListAsync();
        var cartaoComDevs = new Cartao
        {
            Prazo = cartao.Prazo, Estimativa = cartao.Estimativa, SistemaId = cartao.SistemaId, SolicitanteId = cartao.SolicitanteId,
            Desenvolvedores = desenvolvedores.Select(id => new CartaoDesenvolvedor { UsuarioId = id }).ToList()
        };
        var pendentes = RegrasEtapa.Separar(RegrasEtapa.FixosFaltando(cartaoComDevs, lista)).Select(RegrasEtapa.Nome).ToList();

        var obrigatorios = lista.DefinicoesCampo.Where(c => !c.Excluido && c.Obrigatorio && RegrasEtapa.EhManual(c)).ToList();
        if (obrigatorios.Count > 0)
        {
            var valores = await db.ValoresCampoCartao.Where(v => v.CartaoId == cartao.Id).ToListAsync();
            pendentes.AddRange(obrigatorios
                .Where(c => c.PedirNovamenteACadaEntrada || string.IsNullOrWhiteSpace(CamposCartao.ValorMaisRecente(valores, c.Id)))
                .Select(c => c.Nome));
        }
        if (pendentes.Count == 0) return;

        var destinatarios = desenvolvedores.Count > 0
            ? desenvolvedores
            : cartao.SolicitanteId.HasValue ? [cartao.SolicitanteId.Value] : [];
        var usuarioId = usuarioProvider.ObterIdUsuarioAtual();
        db.Notificacoes.AddRange(destinatarios.Select(destinatario => new Notificacao
        {
            DestinatarioId = destinatario, CartaoOrigemId = cartao.Id, Tipo = TipoNotificacao.CampoPendente,
            Mensagem = $"O cartão '{cartao.Titulo}' entrou em '{lista.Nome}' com pendências: {string.Join(", ", pendentes)}.",
            CriadoPorId = usuarioId, CriadoEm = DateTime.UtcNow
        }));
    }

    /// <summary>
    /// O item de tarefa que originou um cartão filho reflete o status desse filho: fica concluído
    /// enquanto o filho estiver em uma lista "Concluído" e volta a pendente se o filho sair dela.
    /// </summary>
    private async Task SincronizarItemPromovidoAsync(Cartao cartao, IReadOnlyCollection<Lista> listasQuadro)
    {
        var itens = await db.ItensTarefa.Where(i => i.CartaoPromovidoId == cartao.Id && !i.Excluido).ToListAsync();
        if (itens.Count == 0) return;
        var concluido = ListasQuadro.EhConcluido(cartao.ListaId, listasQuadro);
        foreach (var item in itens) item.Concluido = concluido;
    }
}

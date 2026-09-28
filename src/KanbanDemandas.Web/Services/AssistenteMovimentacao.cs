using KanbanDemandas.Core.Enums;
using KanbanDemandas.Core.Regras;
using KanbanDemandas.Infrastructure.Services;
using KanbanDemandas.Web.Components.Shared;
using MudBlazor;

namespace KanbanDemandas.Web.Services;

/// <summary>
/// Fluxo de interface para mover um cartão, igual em todas as telas: se a lista de destino bloqueia a entrada
/// com pendências, pede os dados antes (cancelar = não move); se só avisa, move e depois oferece o preenchimento.
/// </summary>
public sealed class AssistenteMovimentacao(
    MovimentacaoCartaoService movimentacao,
    ValoresCampoService valoresCampo,
    IDialogService dialog,
    ISnackbar snackbar)
{
    public async Task<ResultadoMovimentacao?> MoverAsync(int cartaoId, int listaDestinoId, int? cartaoAlvoId)
    {
        var pendencias = await movimentacao.ObterPendenciasAsync(cartaoId, listaDestinoId);
        if (pendencias is { BloqueadosPreenchidos: not CamposFixosCartao.Nenhum })
        {
            var confirmado = await dialog.ShowMessageBox($"Entrada em {pendencias.ListaNome}",
                $"\"{pendencias.ListaNome}\" não permite: {RegrasEtapa.Descrever(pendencias.BloqueadosPreenchidos)}. " +
                "Ao mover, esses dados serão removidos do cartão (fica registrado no histórico).",
                yesText: "Mover e remover", cancelText: "Cancelar");
            if (confirmado != true) return null;
        }
        DadosEtapa? dadosAntes = null;
        if (pendencias is { Bloqueia: true, TemObrigatoriasFaltando: true })
        {
            dadosAntes = await PedirDadosAsync(cartaoId, pendencias, obrigatorio: true);
            if (dadosAntes is null)
            {
                snackbar.Add($"Movimento cancelado: '{pendencias.ListaNome}' exige preencher as pendências antes de entrar.", Severity.Info);
                return null;
            }
        }

        ResultadoMovimentacao? resultado;
        try
        {
            resultado = await movimentacao.MoverAsync(cartaoId, listaDestinoId, cartaoAlvoId);
        }
        catch (Exception)
        {
            snackbar.Add("Não foi possível mover o cartão.", Severity.Error);
            return null;
        }
        if (resultado is null) return null;

        if (resultado.Estornado)
        {
            snackbar.Add($"Movimento desfeito: o cartão voltou para '{resultado.ListaDestinoNome}' e a ida foi retirada do histórico.", Severity.Info);
            return resultado;
        }
        if (resultado.MudouDeLista)
            snackbar.Add($"Cartão movido para {resultado.ListaDestinoNome}.", Severity.Success);
        if (resultado.AvisoCaminho is not null)
            snackbar.Add(resultado.AvisoCaminho, Severity.Warning);
        if (resultado.CamposLimpos != CamposFixosCartao.Nenhum)
            snackbar.Add($"Removido por não ser permitido em '{resultado.ListaDestinoNome}': {RegrasEtapa.Descrever(resultado.CamposLimpos)}.", Severity.Info);

        if (!resultado.MudouDeLista) return resultado;

        if (dadosAntes is not null)
        {
            await valoresCampo.AplicarDadosEtapaAsync(cartaoId, resultado.ListaDestinoId, dadosAntes);
        }
        else if (pendencias is { TemAlgoParaPreencher: true })
        {
            var dadosDepois = await PedirDadosAsync(cartaoId, pendencias, obrigatorio: false);
            if (dadosDepois is not null)
                await valoresCampo.AplicarDadosEtapaAsync(cartaoId, resultado.ListaDestinoId, dadosDepois);
        }
        return resultado;
    }

    private async Task<DadosEtapa?> PedirDadosAsync(int cartaoId, PendenciasEtapa pendencias, bool obrigatorio)
    {
        var referencia = await dialog.ShowAsync<DialogEntradaEtapa>($"Entrada em {pendencias.ListaNome}",
            new DialogParameters<DialogEntradaEtapa>
            {
                { x => x.CartaoId, cartaoId }, { x => x.Pendencias, pendencias }, { x => x.Obrigatorio, obrigatorio }
            },
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = !obrigatorio });
        var resultado = await referencia.Result;
        return resultado is { Canceled: false, Data: DadosEtapa dados } ? dados : null;
    }
}

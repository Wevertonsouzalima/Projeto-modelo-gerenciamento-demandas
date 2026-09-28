using KanbanDemandas.Core.Entities;

namespace KanbanDemandas.Core.Interfaces;

/// <summary>
/// Fornece o usuário atualmente logado.
/// Na v1, implementado com um provider fake/stub.
/// Plugue a implementação real (AD/SSO) sem alterar o restante do sistema.
/// </summary>
public interface IUsuarioAtualProvider
{
    /// <summary>Retorna o usuário atual. Nunca retorna null — lança exceção se não houver sessão.</summary>
    Usuario ObterUsuarioAtual();

    int ObterIdUsuarioAtual();
}

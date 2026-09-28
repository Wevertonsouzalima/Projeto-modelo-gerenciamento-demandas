using System.Text.Json;
using System.Text.Json.Serialization;
using KanbanDemandas.Core.Enums;

namespace KanbanDemandas.Web.Services.Automacoes;

public sealed class ParametrosGatilho
{
    public CampoMonitorado Campo { get; set; } = CampoMonitorado.Qualquer;

    /// <summary>Prazo próximo: dias de antecedência. Parado: dias na lista. Sprint encerrando: dias antes do fim. Vencido: repetir a cada N dias (0 = uma vez).</summary>
    public int Dias { get; set; } = 2;

    /// <summary>Agendado: horário no formato HH:mm.</summary>
    public string Horario { get; set; } = "08:00";

    /// <summary>Agendado: dias da semana; vazio = todos os dias.</summary>
    public List<DayOfWeek> DiasSemana { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
}

public sealed class Condicao
{
    public TipoCondicao Tipo { get; set; } = TipoCondicao.Lista;
    public OperadorCondicao Operador { get; set; } = OperadorCondicao.EstaEm;

    /// <summary>IDs de listas, etiquetas, sistemas, usuários ou valores de prioridade.</summary>
    public List<int> Ids { get; set; } = [];

    /// <summary>Campos personalizados são definidos por lista; a condição os identifica pelo nome.</summary>
    public string? CampoNome { get; set; }
    public string? Texto { get; set; }
    public decimal? Numero { get; set; }
}

public sealed class ParametrosAcao
{
    public int? ListaId { get; set; }
    public bool NoTopo { get; set; }

    public ModoAtribuicao ModoAtribuicao { get; set; } = ModoAtribuicao.MenorCarga;
    public int? UsuarioId { get; set; }

    /// <summary>Candidatos para menor carga/revezamento; vazio = todos os usuários ativos.</summary>
    public List<int> UsuarioIds { get; set; } = [];
    public bool SubstituirAtuais { get; set; }

    public int? EtiquetaId { get; set; }

    public Prioridade? Prioridade { get; set; }
    public bool AumentarUmNivel { get; set; }

    public BaseDataAutomacao BaseData { get; set; } = BaseDataAutomacao.Hoje;
    public int Dias { get; set; }
    public bool DiasUteis { get; set; } = true;
    public bool AplicarEmDataInicio { get; set; }

    public string? CampoNome { get; set; }
    public string? Valor { get; set; }
    public bool SomenteSeVazio { get; set; } = true;

    public List<string> Itens { get; set; } = [];
    public string? Titulo { get; set; }

    public string? Texto { get; set; }
    public string? Assunto { get; set; }
    public DestinatariosAutomacao Destinatarios { get; set; } = DestinatariosAutomacao.Desenvolvedores;
    public List<int> DestinatarioIds { get; set; } = [];
    public string? EmailsFixos { get; set; }

    /// <summary>URL do webhook do canal do Teams (gerada pelo app Workflows no canal).</summary>
    public string? UrlWebhook { get; set; }

    public SeveridadeAlerta Severidade { get; set; } = SeveridadeAlerta.Aviso;

    public EstrategiaConflito Estrategia { get; set; } = EstrategiaConflito.MaisRecente;
    public ResolucaoConflito Resolucao { get; set; } = ResolucaoConflito.EmpurrarDatas;
    public ModoAplicacao Modo { get; set; } = ModoAplicacao.Sugerir;

    /// <summary>Resolver alertas: só os criados por esta regra (padrão) ou todos os alertas abertos do cartão.</summary>
    public bool TodosAlertasDoCartao { get; set; }
}

/// <summary>Mudança proposta para resolver um conflito de datas (aplicada direto ou via sugestão).</summary>
public sealed class PropostaConflito
{
    public DateTime? NovaDataInicio { get; set; }
    public DateTime? NovoPrazo { get; set; }
    public int? ListaId { get; set; }
    public List<int> ConflitaCom { get; set; } = [];
}

/// <summary>Operação inversa de uma alteração feita por automação.</summary>
public sealed class OperacaoDesfazer
{
    public string Tipo { get; set; } = string.Empty;
    public int CartaoId { get; set; }
    public int? Id { get; set; }
    public string? ValorAnterior { get; set; }
    public bool Flag { get; set; }

    public static class Tipos
    {
        public const string Lista = "lista";
        public const string Prioridade = "prioridade";
        public const string Prazo = "prazo";
        public const string DataInicio = "inicio";
        public const string DesenvolvedorAdicionado = "dev+";
        public const string DesenvolvedorRemovido = "dev-";
        public const string EtiquetaAdicionada = "etiqueta+";
        public const string EtiquetaRemovida = "etiqueta-";
        public const string ValorCampo = "campo";
        public const string AlertaCriado = "alerta+";
        public const string AlertaResolvido = "alerta-";
        public const string ItemCriado = "item+";
        public const string CartaoCriado = "cartao+";
        public const string ComentarioCriado = "comentario+";
        public const string SugestaoCriada = "sugestao+";
    }
}

public static class JsonAutomacao
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Opcoes);

    public static T Ler<T>(string? json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json)) return new T();
        try { return JsonSerializer.Deserialize<T>(json, Opcoes) ?? new T(); }
        catch (JsonException) { return new T(); }
    }
}

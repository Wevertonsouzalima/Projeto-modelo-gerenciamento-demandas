namespace KanbanDemandas.Core.Interfaces;

public interface IEmailSender
{
    Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default);
}

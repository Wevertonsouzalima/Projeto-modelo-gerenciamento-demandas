using KanbanDemandas.Core.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace KanbanDemandas.Infrastructure.Services;

/// <summary>
/// Implementação de IEmailSender usando MailKit.
/// Configuração via appsettings: seção "Email".
/// </summary>
public class MailKitEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IConfiguration config, ILogger<MailKitEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAsync(string para, string? cc, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        var emailConfig = _config.GetSection("Email");
        var host = emailConfig["Host"] ?? "localhost";
        var port = int.TryParse(emailConfig["Port"], out var p) ? p : 25;
        var remetente = emailConfig["Remetente"] ?? "sistema@empresa.com";
        var nomeRemetente = emailConfig["NomeRemetente"] ?? "Sistema Kanban";
        var usuario = emailConfig["Usuario"];
        var senha = emailConfig["Senha"];

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(nomeRemetente, remetente));
        foreach (var addr in para.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            mensagem.To.Add(MailboxAddress.Parse(addr));

        if (!string.IsNullOrWhiteSpace(cc))
        {
            foreach (var addr in cc.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mensagem.Cc.Add(MailboxAddress.Parse(addr));
        }

        mensagem.Subject = assunto;
        var builder = new BodyBuilder { HtmlBody = corpoHtml };
        mensagem.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrEmpty(usuario) && !string.IsNullOrEmpty(senha))
                await client.AuthenticateAsync(usuario, senha, ct);

            await client.SendAsync(mensagem, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Para}", para);
            throw;
        }
    }
}

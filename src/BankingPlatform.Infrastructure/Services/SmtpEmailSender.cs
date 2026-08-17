using System.Net;
using System.Net.Mail;
using BankingPlatform.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BankingPlatform.Infrastructure.Services;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "complaints@bank.local";
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        using var message = new MailMessage(cfg.From, to, subject, body);
        using var client = new SmtpClient(cfg.Host, cfg.Port)
        {
            EnableSsl = cfg.EnableSsl
        };
        if (!string.IsNullOrWhiteSpace(cfg.Username))
            client.Credentials = new NetworkCredential(cfg.Username, cfg.Password);
        await client.SendMailAsync(message);
    }
}

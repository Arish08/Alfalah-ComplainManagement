namespace BankingPlatform.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
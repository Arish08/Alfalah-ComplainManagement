namespace BankingPlatform.Infrastructure.Email;
using BankingPlatform.Infrastructure.Email;
public sealed class EmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string FromEmail { get; set; } = "";
    public string FromName { get; set; }
        = "Complaint Management";

    public bool EnableSsl { get; set; } = true;
}
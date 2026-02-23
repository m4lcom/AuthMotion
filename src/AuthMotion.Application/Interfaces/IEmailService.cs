namespace AuthMotion.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string body);
}
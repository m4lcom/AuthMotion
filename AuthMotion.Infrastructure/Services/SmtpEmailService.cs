using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;
using AuthMotion.Application.Interfaces;

namespace AuthMotion.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Sends an HTML email asynchronously using SMTP configurations.
    /// </summary>
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();

        var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "noreply@authmotion.com";
        var senderName = _configuration["SmtpSettings:SenderName"] ?? "AuthMotion Security";

        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        var host = _configuration["SmtpSettings:Host"]
            ?? throw new InvalidOperationException("SMTP Host is not configured.");

        var user = _configuration["SmtpSettings:User"]
            ?? throw new InvalidOperationException("SMTP User is not configured.");

        var pass = _configuration["SmtpSettings:Password"]
            ?? throw new InvalidOperationException("SMTP Password is not configured.");

        if (!int.TryParse(_configuration["SmtpSettings:Port"], out var port))
        {
            port = 2525; // Safe fallback
        }

        using var smtp = new SmtpClient();

        // Connect and authenticate
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, pass);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
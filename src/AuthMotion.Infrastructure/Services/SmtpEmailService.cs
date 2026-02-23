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

        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@authmotion.com";
        var senderName = _configuration["EmailSettings:SenderName"] ?? "AuthMotion Security";

        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        var host = _configuration["EmailSettings:Host"]
            ?? throw new InvalidOperationException("SMTP Host is not configured.");
        var user = _configuration["EmailSettings:Username"]
            ?? throw new InvalidOperationException("SMTP Username is not configured.");
        var pass = _configuration["EmailSettings:Password"]
            ?? throw new InvalidOperationException("SMTP Password is not configured.");

        if (!int.TryParse(_configuration["EmailSettings:Port"], out var port)) port = 2525;

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, pass);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
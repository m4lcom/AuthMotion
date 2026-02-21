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

        // We set a default sender if none is provided in the configuration
        var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "noreply@authmotion.com";
        var senderName = _configuration["SmtpSettings:SenderName"] ?? "AuthMotion Security";

        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();

        var host = _configuration["SmtpSettings:Host"];
        var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "2525");
        var user = _configuration["SmtpSettings:User"];
        var pass = _configuration["SmtpSettings:Password"];

        // Connect and authenticate
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, pass);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
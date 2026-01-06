using HabitFlow.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace HabitFlow.Core.Services;

/// <summary>
/// Email sender implementation using SMTP.
/// Configured via appsettings.json (Email section).
/// </summary>
public class EmailSender(
    ILogger<EmailSender> logger,
    IConfiguration configuration) : IEmailSender
{
    private readonly string _smtpHost = configuration["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host not configured");
    private readonly int _smtpPort = int.Parse(configuration["Email:Smtp:Port"] ?? "587");
    private readonly string _smtpUsername = configuration["Email:Smtp:Username"] ?? throw new InvalidOperationException("Email:Smtp:Username not configured");
    private readonly string _smtpPassword = configuration["Email:Smtp:Password"] ?? throw new InvalidOperationException("Email:Smtp:Password not configured");
    private readonly string _fromEmail = configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
    private readonly string _fromName = configuration["Email:FromName"] ?? "HabitFlow";
    private readonly string _linkBaseUrl = configuration["Email:LinkBaseUrl"]
        ?? configuration["App:PublicBaseUrl"]
        ?? configuration["App:BaseUrl"]
        ?? "http://localhost:5000";
    private readonly bool _enableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) ? enableSsl : true;

    public async Task SendEmailConfirmationAsync(string email, string confirmationLink, CancellationToken cancellationToken = default)
    {
        var fullLink = $"{_linkBaseUrl}{confirmationLink}";

        var subject = "Potwierdź swój adres email - HabitFlow";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #4CAF50;'>Witaj w HabitFlow!</h2>
        <p>Dziękujemy za rejestrację. Aby aktywować swoje konto, kliknij poniższy przycisk:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{fullLink}' style='background-color: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>Potwierdź email</a>
        </div>
        <p style='color: #666; font-size: 14px;'>Lub skopiuj i wklej ten link do przeglądarki:</p>
        <p style='color: #666; font-size: 12px; word-break: break-all;'>{fullLink}</p>
        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>Link wygasa za 24 godziny.</p>
        <p style='color: #999; font-size: 12px;'>Jeśli nie zakładałeś konta w HabitFlow, zignoruj tę wiadomość.</p>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        var fullLink = $"{_linkBaseUrl}{resetLink}";

        var subject = "Reset hasła - HabitFlow";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #FF9800;'>Reset hasła</h2>
        <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta HabitFlow.</p>
        <p>Aby ustawić nowe hasło, kliknij poniższy przycisk:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{fullLink}' style='background-color: #FF9800; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>Zresetuj hasło</a>
        </div>
        <p style='color: #666; font-size: 14px;'>Lub skopiuj i wklej ten link do przeglądarki:</p>
        <p style='color: #666; font-size: 12px; word-break: break-all;'>{fullLink}</p>
        <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
        <p style='color: #999; font-size: 12px;'>Link wygasa za 1 godzinę.</p>
        <p style='color: #999; font-size: 12px;'>Jeśli nie prosiłeś o reset hasła, zignoruj tę wiadomość - Twoje konto jest bezpieczne.</p>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 10000 // 10 seconds
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}

using System.Net;
using System.Net.Mail;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CanvasArt.API.Services;

/// <summary>
/// SMTP-backed email sender. Send failures are logged and swallowed so that a broken mail
/// server never fails the caller's request (e.g. a contact-form submission still persists).
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning("Email not sent (Email:SmtpHost is not configured). Subject: {Subject}", subject);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toAddress);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject {Subject}", toAddress, subject);
        }
    }
}

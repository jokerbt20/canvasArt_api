using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CanvasArt.API.Services;

/// <summary>
/// MailKit-backed SMTP email sender. MailKit (unlike System.Net.Mail) supports implicit SSL on
/// port 465 as well as STARTTLS on 587. Send failures are logged and swallowed so that a broken
/// mail server never fails the caller's request (e.g. an order still persists if mail is down).
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

    public async Task SendAsync(string toAddress, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning("Email not sent (Email:SmtpHost is not configured). Subject: {Subject}", subject);
            return;
        }

        // Bound the whole operation so an unreachable mail server fails fast instead of hanging on
        // the OS socket-connect timeout (~20s). Linked to the caller's token so it still cancels.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        var ct = timeoutCts.Token;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
                builder.HtmlBody = body;
            else
                builder.TextBody = body;
            message.Body = builder.ToMessageBody();

            // Port 465 is implicit SSL (SSL-on-connect); 587 is STARTTLS; anything else, let MailKit choose.
            var socketOptions = _settings.SmtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => _settings.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto
            };

            using var client = new SmtpClient { Timeout = _settings.TimeoutSeconds * 1000 };
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions, ct);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject {Subject}", toAddress, subject);
        }
    }
}

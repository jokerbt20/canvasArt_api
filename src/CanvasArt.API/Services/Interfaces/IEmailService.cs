namespace CanvasArt.API.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an email (plain text by default, or HTML when <paramref name="isHtml"/> is true).
    /// Failures are logged, never thrown to the caller.
    /// </summary>
    Task SendAsync(string toAddress, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}

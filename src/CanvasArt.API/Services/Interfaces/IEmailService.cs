namespace CanvasArt.API.Services.Interfaces;

public interface IEmailService
{
    /// <summary>Sends a plain-text email. Failures are logged, never thrown to the caller.</summary>
    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}

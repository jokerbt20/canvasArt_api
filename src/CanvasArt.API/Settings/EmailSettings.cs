namespace CanvasArt.API.Settings;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "CanvasArt";
    public string NotifyToAddress { get; set; } = string.Empty;
    /// <summary>Max seconds for the whole SMTP send (connect + auth + send) before it aborts.</summary>
    public int TimeoutSeconds { get; set; } = 20;
}

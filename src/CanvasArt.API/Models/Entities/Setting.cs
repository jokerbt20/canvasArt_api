namespace CanvasArt.API.Models.Entities;

/// <summary>Key/value CMS setting (homepage, footer, contact, social links, about, …).</summary>
public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Group { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

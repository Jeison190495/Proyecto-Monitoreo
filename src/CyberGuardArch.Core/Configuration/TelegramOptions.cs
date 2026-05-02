namespace CyberGuardArch.Core.Configuration;

public class TelegramOptions
{
    // El nombre de la sección en el JSON del secrets
    public const string SectionName = "Telegram";

    public string Token { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string NameBot { get; set; } = string.Empty;
}
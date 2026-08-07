namespace EventManager.Infrastructure.Configurations;

public sealed class TokenSettingsConfiguration
{
    public const string SectionName = "TokenSettings";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int LifeTimeInMinutes { get; set; }
}

namespace EventManager.Infrastructure.Configurations;

public sealed class TokenSettingsConfiguration
{
    public const string SectionName = "TokenSettings";
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int LifeTimeInMinutes { get; set; }
}

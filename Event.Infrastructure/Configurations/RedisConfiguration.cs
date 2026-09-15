namespace Event.Infrastructure.Configurations;

public sealed class RedisConfiguration
{
    public const string SectionName = "Redis";
    public string Host { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 3000;
    public bool AbortOnConnectFail { get; set; } = false;
};

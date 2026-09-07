namespace Event.Application.Configurations;

public sealed class EventCacheOptions
{
    public const string SectionName = "EventCache";
    public TimeSpan EventByIdTtl { get; set; }
    public TimeSpan TopEventsTtl { get; set; }
    public string EventKeyPrefix { get; set; } = "event";
    public string TopEventsKey { get; set; } = "events:top10";
}

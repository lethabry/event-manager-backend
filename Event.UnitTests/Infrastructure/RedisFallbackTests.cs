using Event.Application.Configurations;
using Event.Application.Interfaces;
using Event.Application.Services.EventService;
using Event.Application.Services.EventValidatorService;
using Event.Infrastructure;
using Event.Infrastructure.Repositories.EventCacheRepository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using EventEntity = Event.Domain.Models.Event;

namespace Event.UnitTests.Infrastructure;

public class RedisFallbackTests
{
    [Fact]
    public async Task AddInfrastructureServices_WhenRedisIsUnavailable_ShouldProvideUsableCache()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TokenSettings:Secret"] = "test-secret",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Redis:Host"] = "127.0.0.1:1",
                ["Redis:ConnectTimeout"] = "1",
                ["Redis:SyncTimeout"] = "1",
                ["Redis:AbortOnConnectFail"] = "true",
                ["EventCache:EventKeyPrefix"] = "event"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructureServices(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<ICacher>();

        var cachedValue = await cache.GetDataByKeyAsync<string>("event:test");
        var isSaved = await cache.TryWriteDataAsync("event:test", "value", TimeSpan.FromMinutes(1));
        var isDeleted = await cache.TryDeleteDataAsync("event:test");

        cachedValue.Should().BeNull();
        isSaved.Should().BeFalse();
        isDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventById_WhenCacheIsUnavailable_ShouldReadEventFromRepository()
    {
        var eventId = Guid.NewGuid();
        var expected = EventEntity.Reconstruct(
            eventId,
            "Test event",
            new DateTime(2026, 9, 10, 12, 0, 0),
            new DateTime(2026, 9, 10, 14, 0, 0),
            20,
            20);
        var repository = new Mock<IEventRepository>();
        repository.Setup(item => item.GetEventByIdAsync(eventId)).ReturnsAsync(expected);
        var service = new EventService(
            repository.Object,
            Mock.Of<IEventValidatorService>(),
            new UnavailableEventCache(),
            NullLogger<EventService>.Instance,
            Options.Create(new EventCacheOptions()));

        var result = await service.GetEventByIdAsync(eventId);

        result.Should().BeEquivalentTo(expected);
        repository.Verify(item => item.GetEventByIdAsync(eventId), Times.Once);
    }
}

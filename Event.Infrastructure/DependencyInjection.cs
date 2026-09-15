using Event.Application.Configurations;
using Event.Application.Interfaces;
using Event.Infrastructure.Configurations;
using Event.Infrastructure.DataAccess;
using Event.Infrastructure.Kafka.BookingEventsConsumerWorker;
using Event.Infrastructure.Kafka.BookingProducer;
using Event.Infrastructure.Kafka.KafkaTopicInitializerService;
using Event.Infrastructure.Repositories.EventCacheRepository;
using Event.Infrastructure.Repositories.EventRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace Event.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<TokenSettingsConfiguration>()
            .Bind(configuration.GetRequiredSection(TokenSettingsConfiguration.SectionName));
        services.AddOptions<KafkaConfiguration>()
            .Bind(configuration.GetRequiredSection(KafkaConfiguration.SectionName));
        services.AddOptions<EventCacheOptions>()
            .Bind(configuration.GetRequiredSection(EventCacheOptions.SectionName));
        
        var redisSettings = configuration.GetRequiredSection(RedisConfiguration.SectionName).Get<RedisConfiguration>();
        var options = new ConfigurationOptions
        {
            EndPoints = { redisSettings!.Host },
            Password = redisSettings.Password,
            ConnectTimeout = redisSettings.ConnectTimeout,
            SyncTimeout = redisSettings.SyncTimeout,
            AbortOnConnectFail = redisSettings.AbortOnConnectFail
        };
        
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<ICacher>(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            try
            {
                var multiplexer = ConnectionMultiplexer.Connect(options);
                return new EventCacheRepository(
                    multiplexer,
                    loggerFactory.CreateLogger<EventCacheRepository>());
            }
            catch (Exception exception)
            {
                loggerFactory.CreateLogger(nameof(EventCacheRepository))
                    .LogWarning(exception, "Redis is unavailable. Event cache is disabled.");
                return new UnavailableEventCache();
            }
        });
        services.AddSingleton<IBookingProducer, BookingProducer>();
        services.AddHostedService<KafkaTopicInitializerService>();
        services.AddHostedService<BookingEventsConsumerWorker>();
        
        return services;
    }
}

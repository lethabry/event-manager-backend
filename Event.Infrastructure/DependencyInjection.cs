using Event.Application.Interfaces;
using Event.Infrastructure.Configurations;
using Event.Infrastructure.DataAccess;
using Event.Infrastructure.Kafka.BookingEventsConsumerWorker;
using Event.Infrastructure.Kafka.BookingProducer;
using Event.Infrastructure.Kafka.KafkaTopicInitializerService;
using Event.Infrastructure.Repositories.EventRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddOptions<RedisConfiguration>(RedisConfiguration.SectionName);
        
        var redisSettings = configuration.GetSection(RedisConfiguration.SectionName).Get<RedisConfiguration>();
        var options = new ConfigurationOptions
        {
            EndPoints = { redisSettings!.Host },
            Password = redisSettings.Password,
            ConnectTimeout = redisSettings.ConnectTimeout,
            SyncTimeout = redisSettings.SyncTimeout,
            AbortOnConnectFail = redisSettings.AbortOnConnectFail
        };
        
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IBookingProducer, BookingProducer>();
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
        services.AddHostedService<KafkaTopicInitializerService>();
        services.AddHostedService<BookingEventsConsumerWorker>();
        
        return services;
    }
}

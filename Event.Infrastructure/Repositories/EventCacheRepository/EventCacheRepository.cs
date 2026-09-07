using System.Text.Json;
using Event.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace Event.Infrastructure.Repositories.EventCacheRepository;

public sealed class EventCacheRepository : ICacher, IDisposable
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _db;
    private readonly ILogger<EventCacheRepository> _logger;
    
    public EventCacheRepository(IConnectionMultiplexer multiplexer, ILogger<EventCacheRepository> logger)
    {
        _multiplexer = multiplexer;
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }
    
    public async Task<T?> GetDataByKeyAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogInformation("No data found for {Key}", key);
                return null;
            }

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while reading data for {Key}", key);
            return null;
        }
    }
    
    public async Task<bool> TryWriteDataAsync<T>(string key, T data, TimeSpan ttl) where T : class
    {
        try
        {
            var stringData = JsonSerializer.Serialize(data);
            return await _db.StringSetAsync(key, stringData, ttl);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while writing data for {Key}", key);
            return false;
        }
    }
    
    public async Task<bool> TryDeleteDataAsync(string key)
    {
        try
        {
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while deleting data for {Key}", key);
            return false;
        }
    }

    public void Dispose()
    {
        _multiplexer.Dispose();
    }
}

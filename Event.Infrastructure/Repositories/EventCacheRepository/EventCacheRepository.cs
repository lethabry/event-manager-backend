using System.Text.Json;
using Event.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace Event.Infrastructure.Repositories.EventCacheRepository;

public sealed class EventCacheRepository : ICacher
{
    private readonly IDatabase _db;
    private readonly ILogger<EventCacheRepository> _logger;
    
    public EventCacheRepository(IConnectionMultiplexer multiplexer, ILogger<EventCacheRepository> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }
    
    public async Task<T?> GetDataByKeyAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue)
        {
            _logger.LogInformation($"No data found for {key}");
            return null;
        }
        
        try
        {
            var data = JsonSerializer.Deserialize<T>(value.ToString());
            return data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while reading data for {key}");
            return null;
        }
    }
    
    public async Task<bool> TryWriteDataAsync<T>(string key, T data, int ttl) where T : class
    {
        var stringData = JsonSerializer.Serialize(data);
        try
        {
            await _db.StringSetAsync(key, stringData, TimeSpan.FromMinutes(ttl));
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while writing data for {key}");
            return false;
        }
    }
    
    public async Task<bool> TryDeleteDataAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Error while deleting data for {key}");
            return false;
        }
    }
}

using System.Text.Json;
using Event.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace Event.Infrastructure.Cacher;

public sealed class RedisCacher : ICacher
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacher> _logger;
    
    public RedisCacher(IConnectionMultiplexer multiplexer, ILogger<RedisCacher> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }
    
    public async Task<T?> GetDataByIdAsync<T>(string id) where T : class
    {
        var value = await _db.StringGetAsync(id);
        if (!value.HasValue)
        {
            _logger.LogInformation($"No data found for {id}");
            return null;
        }
        
        try
        {
            var data = JsonSerializer.Deserialize<T>(value.ToString());
            return data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while reading data for {id}");
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
    
    public async Task<bool> TryDeleteDataAsync<T>(string key)
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

using Event.Application.Interfaces;

namespace Event.Infrastructure.Repositories.EventCacheRepository;

public sealed class UnavailableEventCache : ICacher
{
    public Task<T?> GetDataByKeyAsync<T>(string key) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    public Task<bool> TryWriteDataAsync<T>(string key, T data, TimeSpan ttl) where T : class
    {
        return Task.FromResult(false);
    }

    public Task<bool> TryDeleteDataAsync(string key)
    {
        return Task.FromResult(false);
    }
}

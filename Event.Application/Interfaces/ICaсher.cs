namespace Event.Application.Interfaces;

public interface ICacher
{
    Task<T?> GetDataByKeyAsync<T>(string key) where T : class;
    Task<bool> TryWriteDataAsync<T>(string key, T data, int ttl) where T : class;
    Task<bool> TryDeleteDataAsync(string key);
}

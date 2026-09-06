namespace Event.Application.Interfaces;

public interface ICacher
{
    Task<T?> GetDataByIdAsync<T>(string id) where T : class;
    Task<bool> TryWriteDataAsync<T>(string key, T data, int ttl) where T : class;
    Task<bool> TryDeleteDataAsync<T>(string key);
}

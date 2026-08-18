namespace Auth.Infrastructure.Services;

public interface IHasher
{
    public string GetHash(string text);
}

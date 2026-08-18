using System.Security.Cryptography;
using System.Text;
namespace Auth.Infrastructure.Services;

public class Hasher : IHasher
{
    public string GetHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}

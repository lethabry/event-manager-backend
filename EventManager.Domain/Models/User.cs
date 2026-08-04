using EventManager.Domain.Common;
namespace EventManager.Domain.Models;

public class User
{
    public Guid Id { get; init; }
    public string Login { get; init; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    private User()
    {
    }
}

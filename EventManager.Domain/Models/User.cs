using EventManager.Domain.Common;
namespace EventManager.Domain.Models;

public class User
{
    public Guid Id { get; init; }
    public string Login { get; init; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public List<Booking> Bookings { get; private set; } = [];

    private User()
    {
    }

    public User(string login, string passwordHash, UserRole? role)
    {
        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role ?? UserRole.User;
    }
}

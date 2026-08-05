namespace EventManager.Application.DTOs;

public record LogingUserDTO()
{
    public string Login { get; set; }
    public string Password { get; set; }
};

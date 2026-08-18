namespace Auth.Application.DTOs;

public record LoginUserDTO()
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
};

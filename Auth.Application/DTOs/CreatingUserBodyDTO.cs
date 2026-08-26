namespace Auth.Application.DTOs;

public sealed record CreatingUserBodyDTO(
    string Login,
    string Password,
    string Role = "User");

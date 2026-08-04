using System.Text;
using EventManager.Application.DTOs;
using EventManager.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
namespace EventManager.Infrastructure.Services;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly TokenSettingsConfiguration _configuration;

    public JwtTokenGenerator(IOptions<TokenSettingsConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public string GenerateToken(UserForTokenDTO user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            ["login"] = user.Login,
            ["role"] = user.Role,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _configuration.Issuer,
            Audience = _configuration.Audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(_configuration.LifeTimeInMinutes),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = creds
        };

        var tokenString = new JsonWebTokenHandler().CreateToken(descriptor);
        return tokenString;
    }
}

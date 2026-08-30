using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Auth;

public class JwtTokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly int _expirationMinutes;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        _expirationMinutes = options.Value.ExpirationMinutes;
    }

    public string GenerateToken(string username)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, username) };
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

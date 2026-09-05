using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Auth;

public class JwtTokenService
{
    private readonly SigningCredentials _credentials;
    private readonly int _expirationMinutes;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _expirationMinutes = options.Value.ExpirationMinutes;
    }

    // JsonWebTokenHandler rather than the older JwtSecurityTokenHandler: it's the handler
    // JwtBearer already validates with, and it doesn't lean on the reflection-based claim mapping
    // that Native AOT would trim away.
    public string GenerateToken(string username)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = username },
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            SigningCredentials = _credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

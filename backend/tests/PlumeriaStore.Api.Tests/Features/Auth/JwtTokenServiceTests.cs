using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Auth;

namespace PlumeriaStore.Api.Tests.Features.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        Secret = "test-secret-key-at-least-32-bytes-long-for-hmac-sha256",
        ExpirationMinutes = 60,
    }));

    [Fact]
    public void GenerateToken_produces_a_token_carrying_the_username_and_a_future_expiry()
    {
        var token = _service.GenerateToken("admin");

        var jwt = new JsonWebToken(token);

        Assert.Equal("admin", jwt.Subject);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }
}

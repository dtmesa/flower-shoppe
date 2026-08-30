using System.Net;
using System.Net.Http.Json;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<PlumeriaApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(PlumeriaApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_with_the_seeded_admin_credentials_returns_a_token()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("admin", body.Username);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_missing_field_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

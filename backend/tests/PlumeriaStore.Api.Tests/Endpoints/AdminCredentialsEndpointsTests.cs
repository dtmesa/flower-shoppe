using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Endpoints;

// Read-only checks against the default seeded admin - safe to share one factory across these
// since none of them mutate the account.
public class AdminProfileEndpointsTests : IClassFixture<PlumeriaApiFactory>
{
    private readonly PlumeriaApiFactory _factory;

    public AdminProfileEndpointsTests(PlumeriaApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Me_returns_the_current_admin_username()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AdminProfileResponse>();

        Assert.Equal("admin", body!.Username);
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Updating_credentials_rejects_the_wrong_current_password()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/auth/admin", new UpdateCredentialsRequest("wrong-password", "admin", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

// The account-mutating case gets its own factory (own isolated in-memory db) so changing the
// admin's username/password here can't affect - or be affected by - any other test class.
public class AdminCredentialsUpdateEndpointsTests : IClassFixture<PlumeriaApiFactory>
{
    private readonly PlumeriaApiFactory _factory;

    public AdminCredentialsUpdateEndpointsTests(PlumeriaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Updating_username_and_password_reissues_a_token_and_the_old_credentials_stop_working()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/auth/admin",
            new UpdateCredentialsRequest("admin", "newadmin", "newpass123"));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("newadmin", updated!.Username);
        Assert.False(string.IsNullOrWhiteSpace(updated.Token));

        var newLogin = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("newadmin", "newpass123"));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        var oldLogin = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
    }
}

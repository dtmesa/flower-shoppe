using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Endpoints;

public class InventoryEndpointsTests : IClassFixture<PlumeriaApiFactory>
{
    private readonly PlumeriaApiFactory _factory;

    public InventoryEndpointsTests(PlumeriaApiFactory factory)
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
    public async Task Creating_an_item_without_a_token_is_rejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-001", "Rooted Plant", null, null, 24.99m, 5, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Full_create_read_update_delete_round_trip()
    {
        var client = await AuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-001", "Rooted Plant", "Yellow/White", "Medium", 24.99m, 5, "Fragrant."));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<InventoryItemResponse>();
        Assert.NotNull(created);
        Assert.Equal("TAG-001", created!.Id);

        // Public read requires no auth token.
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetFromJsonAsync<InventoryItemResponse>($"/api/inventory/{created.Id}");
        Assert.Equal("TAG-001", getResponse!.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/inventory/{created.Id}", new InventoryItemUpdateRequest("Rooted Plant", "Yellow/White", "Medium", 29.99m, 3, "Fragrant."));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<InventoryItemResponse>();
        Assert.Equal(29.99m, updated!.Price);

        var deleteResponse = await client.DeleteAsync($"/api/inventory/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await publicClient.GetAsync($"/api/inventory/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Creating_an_item_with_a_duplicate_id_returns_a_bad_request()
    {
        var client = await AuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-DUP", "Rooted Plant", null, null, 24.99m, 5, null));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-DUP", "Cutting", null, null, 9.99m, 2, null));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Creating_an_item_with_a_negative_price_returns_a_validation_problem()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-002", "Rooted Plant", null, null, -1m, 5, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_photo_over_HTTP_succeeds_and_serves_it_back()
    {
        // Regression coverage for a real bug: Minimal APIs attach antiforgery metadata to any
        // endpoint with form binding, and the request 500s unless the endpoint calls
        // .DisableAntiforgery() (this app has no antiforgery middleware, by design - it's a
        // stateless Bearer-token API). A service-layer test with a hand-built IFormFile can't
        // catch this; it only surfaces when the request actually goes through the HTTP pipeline.
        var client = await AuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/inventory", new InventoryItemCreateRequest("TAG-003", "Rooted Plant", null, null, 24.99m, 5, null));
        var created = await createResponse.Content.ReadFromJsonAsync<InventoryItemResponse>();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([137, 80, 78, 71]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "photo.png");

        var uploadResponse = await client.PostAsync($"/api/inventory/{created!.Id}/images", content);
        uploadResponse.EnsureSuccessStatusCode();
        var withImage = await uploadResponse.Content.ReadFromJsonAsync<InventoryItemResponse>();

        var image = Assert.Single(withImage!.Images);
        var imageResponse = await client.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
    }
}

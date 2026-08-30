using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Features.Inventory;

public class InventoryServiceTests : IDisposable
{
    private readonly SqliteInMemoryDbContext _fixture = new();
    private readonly string _uploadDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        var fileStorageService = new FileStorageService(Options.Create(new UploadOptions { Directory = _uploadDir }));
        _service = new InventoryService(_fixture.Db, fileStorageService);
    }

    private static InventoryItemCreateRequest ValidCreateRequest(string id = "TAG-001") =>
        new(id, "Rooted Plant", "Yellow/White", "Medium", 24.99m, 5, "A fragrant classic.");

    private static InventoryItemUpdateRequest ValidUpdateRequest() =>
        new("Rooted Plant", "Yellow/White", "Medium", 24.99m, 5, "A fragrant classic.");

    [Fact]
    public async Task CreateAsync_persists_and_returns_item()
    {
        var created = await _service.CreateAsync(ValidCreateRequest("TAG-001"));

        Assert.Equal("TAG-001", created.Id);
        Assert.Empty(created.Images);

        var fetched = await _service.FindByIdAsync(created.Id);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_id()
    {
        await _service.CreateAsync(ValidCreateRequest("TAG-001"));

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(ValidCreateRequest("TAG-001")));
    }

    [Fact]
    public async Task FindByIdAsync_throws_NotFoundException_for_missing_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.FindByIdAsync("does-not-exist"));
    }

    [Fact]
    public async Task UpdateAsync_overwrites_fields()
    {
        var created = await _service.CreateAsync(ValidCreateRequest("TAG-001"));

        var updated = await _service.UpdateAsync(created.Id, ValidUpdateRequest() with { QuantityAvailable = 2 });

        Assert.Equal(2, updated.QuantityAvailable);
    }

    [Fact]
    public async Task DeleteAsync_removes_item()
    {
        var created = await _service.CreateAsync(ValidCreateRequest("TAG-001"));

        await _service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.FindByIdAsync(created.Id));
    }

    [Fact]
    public async Task AddImageAsync_rejects_disallowed_content_type()
    {
        var created = await _service.CreateAsync(ValidCreateRequest("TAG-001"));
        var file = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "malware.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _service.AddImageAsync(created.Id, file));
    }

    [Fact]
    public async Task AddImageAsync_then_DeleteImageAsync_round_trips()
    {
        var created = await _service.CreateAsync(ValidCreateRequest("TAG-001"));
        var bytes = new byte[] { 137, 80, 78, 71 };
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "photo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        var withImage = await _service.AddImageAsync(created.Id, file);
        Assert.Single(withImage.Images);

        var withoutImage = await _service.DeleteImageAsync(created.Id, withImage.Images[0].Id);
        Assert.Empty(withoutImage.Images);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        if (Directory.Exists(_uploadDir))
        {
            Directory.Delete(_uploadDir, recursive: true);
        }
    }
}

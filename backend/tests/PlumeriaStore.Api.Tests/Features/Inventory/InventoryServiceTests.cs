using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Reservations;
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

    private static InventoryItemCreateRequest ValidCreateRequest() =>
        new("Rooted Plant", "Yellow/White", "Medium", 24.99m, 5, "A fragrant classic.");

    private static InventoryItemUpdateRequest ValidUpdateRequest() =>
        new(24.99m, 5, "A fragrant classic.");

    private static FormFile ValidPngFile(string name = "photo.png")
    {
        var bytes = new byte[] { 137, 80, 78, 71 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };
    }

    [Fact]
    public async Task CreateAsync_derives_the_id_from_type_color_and_size()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());

        Assert.Equal("RYM", created.Id);
        Assert.Empty(created.Images);

        var fetched = await _service.FindByIdAsync(created.Id);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_second_item_with_the_same_type_color_and_size()
    {
        await _service.CreateAsync(ValidCreateRequest());

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(ValidCreateRequest()));
        Assert.Contains("increase its quantity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_unrecognized_category_value()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(ValidCreateRequest() with { Type = "Sapling" }));
    }

    [Fact]
    public async Task FindByIdAsync_throws_NotFoundException_for_missing_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.FindByIdAsync("does-not-exist"));
    }

    /// <summary>Confirms a pickup request for <paramref name="quantity"/> of the item, placing a hold.</summary>
    private async Task HoldStockAsync(string itemId, int quantity)
    {
        var reservations = new ReservationService(_fixture.Db, NoopEmailNotificationService.Create());
        var request = await reservations.CreateAsync(new PickupRequestCreateRequest(
            "Jane", null, "jane@example.com", null, [new PickupRequestLineItemInput(itemId, quantity)]));
        await reservations.UpdateStatusAsync(request.Id, ReservationStatus.CONFIRMED);
    }

    [Fact]
    public async Task An_item_with_no_confirmed_requests_reports_all_of_its_stock_as_available()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());

        Assert.Equal(5, created.QuantityTotal);
        Assert.Equal(0, created.QuantityReserved);
        Assert.Equal(5, created.QuantityAvailable);
    }

    [Fact]
    public async Task Confirmed_requests_are_reported_as_reserved_and_subtracted_from_available()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        await HoldStockAsync(created.Id, 2);

        var item = await _service.FindByIdAsync(created.Id);

        Assert.Equal(5, item.QuantityTotal);
        Assert.Equal(2, item.QuantityReserved);
        Assert.Equal(3, item.QuantityAvailable);
    }

    [Fact]
    public async Task Holds_from_several_requests_add_up()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        await HoldStockAsync(created.Id, 2);
        await HoldStockAsync(created.Id, 1);

        var item = await _service.FindByIdAsync(created.Id);

        Assert.Equal(3, item.QuantityReserved);
        Assert.Equal(2, item.QuantityAvailable);
    }

    [Fact]
    public async Task FindAllAsync_reports_reserved_quantities_per_item()
    {
        var withHold = await _service.CreateAsync(ValidCreateRequest());
        var withoutHold = await _service.CreateAsync(ValidCreateRequest() with { Color = "Pink" });
        await HoldStockAsync(withHold.Id, 4);

        var all = await _service.FindAllAsync();

        Assert.Equal(4, all.Single(i => i.Id == withHold.Id).QuantityReserved);
        Assert.Equal(1, all.Single(i => i.Id == withHold.Id).QuantityAvailable);
        Assert.Equal(0, all.Single(i => i.Id == withoutHold.Id).QuantityReserved);
    }

    [Fact]
    public async Task UpdateAsync_rejects_a_total_below_what_is_already_reserved()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        await HoldStockAsync(created.Id, 4);

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateAsync(created.Id, ValidUpdateRequest() with { QuantityTotal = 3 }));
        Assert.Contains("4", error.Message);
    }

    [Fact]
    public async Task UpdateAsync_allows_a_total_exactly_matching_what_is_reserved()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        await HoldStockAsync(created.Id, 4);

        var updated = await _service.UpdateAsync(created.Id, ValidUpdateRequest() with { QuantityTotal = 4 });

        Assert.Equal(4, updated.QuantityTotal);
        Assert.Equal(0, updated.QuantityAvailable);
    }

    [Fact]
    public async Task UpdateAsync_overwrites_price_quantity_and_description()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());

        var updated = await _service.UpdateAsync(created.Id, ValidUpdateRequest() with { QuantityTotal = 2 });

        Assert.Equal(2, updated.QuantityTotal);
        // Nothing is held, so available mirrors the total.
        Assert.Equal(0, updated.QuantityReserved);
        Assert.Equal(2, updated.QuantityAvailable);
        Assert.Equal("Rooted Plant", updated.Type);
        Assert.Equal("Yellow/White", updated.Color);
        Assert.Equal("Medium", updated.Size);
    }

    [Fact]
    public async Task DeleteAsync_removes_item()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());

        await _service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.FindByIdAsync(created.Id));
    }

    [Fact]
    public async Task AddImageAsync_rejects_disallowed_content_type()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
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
        var created = await _service.CreateAsync(ValidCreateRequest());

        var withImage = await _service.AddImageAsync(created.Id, ValidPngFile());
        Assert.Single(withImage.Images);

        var withoutImage = await _service.DeleteImageAsync(created.Id, withImage.Images[0].Id);
        Assert.Empty(withoutImage.Images);
    }

    [Fact]
    public async Task The_first_photo_uploaded_defaults_to_primary_and_later_ones_do_not()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());

        var withFirst = await _service.AddImageAsync(created.Id, ValidPngFile());
        Assert.True(withFirst.Images[0].IsPrimary);

        var withSecond = await _service.AddImageAsync(created.Id, ValidPngFile());
        Assert.True(withSecond.Images[0].IsPrimary);
        Assert.False(withSecond.Images[1].IsPrimary);
    }

    [Fact]
    public async Task SetPrimaryImageAsync_switches_which_photo_is_primary()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        var withFirst = await _service.AddImageAsync(created.Id, ValidPngFile());
        var withSecond = await _service.AddImageAsync(created.Id, ValidPngFile());
        var secondImageId = withSecond.Images[1].Id;

        var updated = await _service.SetPrimaryImageAsync(created.Id, secondImageId);

        Assert.False(updated.Images.Single(img => img.Id == withFirst.Images[0].Id).IsPrimary);
        Assert.True(updated.Images.Single(img => img.Id == secondImageId).IsPrimary);
    }

    [Fact]
    public async Task Deleting_the_primary_photo_promotes_the_next_one()
    {
        var created = await _service.CreateAsync(ValidCreateRequest());
        var withFirst = await _service.AddImageAsync(created.Id, ValidPngFile());
        var firstImageId = withFirst.Images[0].Id;
        var withSecond = await _service.AddImageAsync(created.Id, ValidPngFile());
        var secondImageId = withSecond.Images[1].Id;

        var afterDelete = await _service.DeleteImageAsync(created.Id, firstImageId);

        Assert.True(Assert.Single(afterDelete.Images, img => img.Id == secondImageId).IsPrimary);
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

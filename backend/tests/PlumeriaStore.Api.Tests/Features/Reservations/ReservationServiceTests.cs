using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Reservations;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Features.Reservations;

public class ReservationServiceTests : IDisposable
{
    private readonly SqliteInMemoryDbContext _fixture = new();
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService(_fixture.Db);
    }

    private async Task<string> SeedItemAsync()
    {
        var item = new InventoryItem
        {
            Id = "TAG-001",
            Type = "Rooted Plant",
            Color = "Yellow/White",
            Size = "Medium",
            Price = 24.99m,
            QuantityAvailable = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _fixture.Db.InventoryItems.Add(item);
        await _fixture.Db.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task CreateAsync_snapshots_item_characteristics_and_defaults_to_pending()
    {
        var itemId = await SeedItemAsync();

        var reservation = await _service.CreateAsync(new ReservationCreateRequest(itemId, "Jane Customer", null, "jane@example.com", 2, "weekend pickup?"));

        Assert.Equal(itemId, reservation.InventoryItemId);
        Assert.Equal("Rooted Plant · Yellow/White · Medium", reservation.ItemSnapshot);
        Assert.Equal(ReservationStatus.PENDING, reservation.Status);
        Assert.Equal("jane@example.com", reservation.CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_phone_number_with_no_email()
    {
        var itemId = await SeedItemAsync();

        var reservation = await _service.CreateAsync(new ReservationCreateRequest(itemId, "Jane Customer", "555-0100", null, 1, null));

        Assert.Equal("555-0100", reservation.CustomerPhone);
        Assert.Null(reservation.CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_request_with_neither_phone_nor_email()
    {
        var itemId = await SeedItemAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new ReservationCreateRequest(itemId, "Jane Customer", null, null, 1, null)));
    }

    [Fact]
    public async Task CreateAsync_throws_NotFoundException_for_missing_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(new ReservationCreateRequest("does-not-exist", "Jane", null, "jane@example.com", 1, null)));
    }

    [Fact]
    public async Task UpdateStatusAsync_changes_status()
    {
        var itemId = await SeedItemAsync();
        var reservation = await _service.CreateAsync(new ReservationCreateRequest(itemId, "Jane", null, "jane@example.com", 1, null));

        var updated = await _service.UpdateStatusAsync(reservation.Id, ReservationStatus.CONTACTED);

        Assert.Equal(ReservationStatus.CONTACTED, updated.Status);
    }

    [Fact]
    public async Task Deleting_the_inventory_item_nulls_the_reservation_fk_but_keeps_the_snapshot()
    {
        var itemId = await SeedItemAsync();
        var reservation = await _service.CreateAsync(new ReservationCreateRequest(itemId, "Jane", null, "jane@example.com", 1, null));

        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        _fixture.Db.InventoryItems.Remove(item!);
        await _fixture.Db.SaveChangesAsync();

        var survivors = await _service.FindAllAsync();
        var survivor = Assert.Single(survivors);
        Assert.Null(survivor.InventoryItemId);
        Assert.Equal("Rooted Plant · Yellow/White · Medium", survivor.ItemSnapshot);
        Assert.Equal(reservation.Id, survivor.Id);
    }

    public void Dispose() => _fixture.Dispose();
}

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
        _service = new ReservationService(_fixture.Db, NoopEmailNotificationService.Create());
    }

    private async Task<string> SeedItemAsync(string id = "TAG-001", int quantityTotal = 5)
    {
        var item = new InventoryItem
        {
            Id = id,
            Type = "Rooted Plant",
            Color = "Yellow/White",
            Size = "Medium",
            Price = 24.99m,
            QuantityTotal = quantityTotal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _fixture.Db.InventoryItems.Add(item);
        await _fixture.Db.SaveChangesAsync();
        return item.Id;
    }

    private static PickupRequestCreateRequest ValidRequest(string itemId, int quantity = 2, string? notes = "weekend pickup?") =>
        new("Jane Customer", null, "jane@example.com", notes, [new PickupRequestLineItemInput(itemId, quantity)]);

    [Fact]
    public async Task CreateAsync_snapshots_item_characteristics_and_defaults_to_new()
    {
        var itemId = await SeedItemAsync();

        var request = await _service.CreateAsync(ValidRequest(itemId));

        var line = Assert.Single(request.Items);
        Assert.Equal(itemId, line.InventoryItemId);
        Assert.Equal("Rooted Plant · Yellow/White · Medium", line.ItemSnapshot);
        Assert.Equal(ReservationStatus.NEW, request.Status);
        Assert.False(request.StockReserved);
        Assert.Equal("jane@example.com", request.CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_groups_multiple_items_under_one_request()
    {
        var itemId1 = await SeedItemAsync("TAG-001");
        var itemId2 = await SeedItemAsync("TAG-002");

        var request = await _service.CreateAsync(new PickupRequestCreateRequest(
            "Jane Customer", null, "jane@example.com", "one request, two plants",
            [new PickupRequestLineItemInput(itemId1, 1), new PickupRequestLineItemInput(itemId2, 3)]));

        Assert.Equal(2, request.Items.Count);
        Assert.Contains(request.Items, line => line.InventoryItemId == itemId1 && line.QuantityRequested == 1);
        Assert.Contains(request.Items, line => line.InventoryItemId == itemId2 && line.QuantityRequested == 3);

        var all = await _service.FindAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_phone_number_with_no_email()
    {
        var itemId = await SeedItemAsync();

        var request = await _service.CreateAsync(new PickupRequestCreateRequest("Jane Customer", "(555) 010-0100", null, null, [new PickupRequestLineItemInput(itemId, 1)]));

        Assert.Equal("(555) 010-0100", request.CustomerPhone);
        Assert.Null(request.CustomerEmail);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_request_with_neither_phone_nor_email()
    {
        var itemId = await SeedItemAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new PickupRequestCreateRequest("Jane Customer", null, null, null, [new PickupRequestLineItemInput(itemId, 1)])));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_phone_number_without_10_digits()
    {
        var itemId = await SeedItemAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new PickupRequestCreateRequest("Jane Customer", "555-0100", null, null, [new PickupRequestLineItemInput(itemId, 1)])));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_malformed_email()
    {
        var itemId = await SeedItemAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new PickupRequestCreateRequest("Jane Customer", null, "not-an-email", null, [new PickupRequestLineItemInput(itemId, 1)])));
    }

    // The [Range(1, ...)] attribute on PickupRequestLineItemInput never fires - the endpoint's
    // ValidationFilter doesn't recurse into collection elements - so the service enforces it.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_rejects_a_line_item_quantity_below_one(int quantity)
    {
        var itemId = await SeedItemAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(ValidRequest(itemId, quantity)));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_quantity_beyond_available_stock()
    {
        var itemId = await SeedItemAsync(quantityTotal: 3);

        var error = await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(ValidRequest(itemId, 4)));
        Assert.Contains("3", error.Message);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_quantity_exactly_matching_available_stock()
    {
        var itemId = await SeedItemAsync(quantityTotal: 3);

        var request = await _service.CreateAsync(ValidRequest(itemId, 3));

        Assert.Equal(3, Assert.Single(request.Items).QuantityRequested);
    }

    [Fact]
    public async Task CreateAsync_throws_NotFoundException_for_missing_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(new PickupRequestCreateRequest("Jane", null, "jane@example.com", null, [new PickupRequestLineItemInput("does-not-exist", 1)])));
    }

    [Fact]
    public async Task UpdateStatusAsync_changes_status()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));

        var updated = await _service.UpdateStatusAsync(request.Id, ReservationStatus.CONTACTED);

        Assert.Equal(ReservationStatus.CONTACTED, updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_setting_status_to_completed_directly()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateStatusAsync(request.Id, ReservationStatus.COMPLETED));
    }

    [Fact]
    public async Task Confirming_a_request_holds_stock_for_every_item_without_changing_totals()
    {
        var itemId1 = await SeedItemAsync("TAG-001", quantityTotal: 5);
        var itemId2 = await SeedItemAsync("TAG-002", quantityTotal: 5);
        var request = await _service.CreateAsync(new PickupRequestCreateRequest(
            "Jane", null, "jane@example.com", null,
            [new PickupRequestLineItemInput(itemId1, 2), new PickupRequestLineItemInput(itemId2, 1)]));

        var confirmed = await _service.UpdateStatusAsync(request.Id, ReservationStatus.CONFIRMED);

        Assert.True(confirmed.StockReserved);
        // Every line takes a hold...
        var lines = _fixture.Db.Reservations.Where(line => line.PickupRequestId == request.Id).ToList();
        Assert.All(lines, line => Assert.True(line.StockReserved));
        // ...but on-hand totals only move when the request is completed.
        Assert.Equal(5, (await _fixture.Db.InventoryItems.FindAsync(itemId1))!.QuantityTotal);
        Assert.Equal(5, (await _fixture.Db.InventoryItems.FindAsync(itemId2))!.QuantityTotal);
    }

    [Fact]
    public async Task Moving_a_confirmed_request_away_from_confirmed_releases_the_hold()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));
        await _service.UpdateStatusAsync(request.Id, ReservationStatus.CONFIRMED);

        var cancelled = await _service.UpdateStatusAsync(request.Id, ReservationStatus.CANCELLED);

        Assert.False(cancelled.StockReserved);
        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        Assert.Equal(5, item!.QuantityTotal);
    }

    [Fact]
    public async Task CompleteAsync_with_permanentlyClear_removes_the_units_from_the_total()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));
        await _service.UpdateStatusAsync(request.Id, ReservationStatus.CONFIRMED);

        var completed = await _service.CompleteAsync(request.Id, permanentlyClear: true);

        Assert.Equal(ReservationStatus.COMPLETED, completed.Status);
        Assert.False(completed.StockReserved);
        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        Assert.Equal(3, item!.QuantityTotal);
    }

    [Fact]
    public async Task CompleteAsync_without_permanentlyClear_releases_the_hold_and_keeps_the_total()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));
        await _service.UpdateStatusAsync(request.Id, ReservationStatus.CONFIRMED);

        var completed = await _service.CompleteAsync(request.Id, permanentlyClear: false);

        Assert.Equal(ReservationStatus.COMPLETED, completed.Status);
        Assert.False(completed.StockReserved);
        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        Assert.Equal(5, item!.QuantityTotal);
    }

    [Fact]
    public async Task CompleteAsync_on_a_never_confirmed_request_leaves_stock_untouched()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));

        var completed = await _service.CompleteAsync(request.Id, permanentlyClear: true);

        Assert.Equal(ReservationStatus.COMPLETED, completed.Status);
        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        Assert.Equal(5, item!.QuantityTotal);
    }

    [Fact]
    public async Task Deleting_the_inventory_item_nulls_the_line_items_fk_but_keeps_the_snapshot()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));

        var item = await _fixture.Db.InventoryItems.FindAsync(itemId);
        _fixture.Db.InventoryItems.Remove(item!);
        await _fixture.Db.SaveChangesAsync();

        var survivors = await _service.FindAllAsync();
        var survivor = Assert.Single(survivors);
        var line = Assert.Single(survivor.Items);
        Assert.Null(line.InventoryItemId);
        Assert.Equal("Rooted Plant · Yellow/White · Medium", line.ItemSnapshot);
        Assert.Equal(request.Id, survivor.Id);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_request_and_its_line_items()
    {
        var itemId = await SeedItemAsync();
        var request = await _service.CreateAsync(ValidRequest(itemId));

        await _service.DeleteAsync(request.Id);

        Assert.Empty(await _service.FindAllAsync());
    }

    public void Dispose() => _fixture.Dispose();
}

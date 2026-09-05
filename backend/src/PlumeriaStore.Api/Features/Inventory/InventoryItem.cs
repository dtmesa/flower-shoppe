namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryItem
{
    // Set by the admin at creation time to match the physical ID tag on the plant - not
    // database-generated.
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }

    // How many the shop physically has. Confirmed pickup requests place a *hold* on some of these
    // rather than decrementing this number (see QuantityReserved), so the count customers see is
    // QuantityTotal minus whatever is currently held. This only drops when a completed request
    // permanently clears its stock, or when the admin edits it directly.
    public int QuantityTotal { get; set; }

    // Units held by confirmed, not-yet-completed pickup requests. Under SQLite this was summed on
    // demand from the reservation rows; DynamoDB has no GROUP BY across partitions, so it is kept
    // here instead and moved in the same transaction that flips a request's holds on or off (see
    // ReservationRepository.SaveAsync).
    public int QuantityReserved { get; set; }

    public string? Description { get; set; }
    public List<InventoryImage> Images { get; set; } = new();

    // Photos are stored inside this item rather than as rows of their own, so their IDs come from
    // a per-item counter. It only ever grows, so deleting a photo can't hand its ID to the next one.
    public int NextImageId { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace PlumeriaStore.Api.Features.Reservations;

// One line item within a PickupRequest - one inventory item + quantity. Customer info, notes, and
// status all live on the parent; this only tracks what's specific to this particular item. Lines
// are stored inside the parent request rather than as rows of their own: they are only ever read
// or written together with it, so one DynamoDB item holds the whole request.
public class Reservation
{
    public int Id { get; set; }

    // Null once the referenced item is deleted. SQLite did that with ON DELETE SET NULL; now
    // InventoryService.DeleteAsync clears it explicitly (see ReservationRepository).
    public string? InventoryItemId { get; set; }

    // Snapshot of the item's characteristics at request time, so history reads fine even if the
    // item is later deleted (items have no name - just an ID tag and characteristics).
    public string ItemSnapshot { get; set; } = string.Empty;

    public int QuantityRequested { get; set; }

    // True while this line is holding QuantityRequested of the item's stock: set when the parent
    // request is confirmed, cleared when it completes (whether the stock is permanently cleared or
    // released back). The item's QuantityTotal is never changed by the hold itself - held units are
    // counted in InventoryItem.QuantityReserved and subtracted when reporting what's available.
    public bool StockReserved { get; set; }
}

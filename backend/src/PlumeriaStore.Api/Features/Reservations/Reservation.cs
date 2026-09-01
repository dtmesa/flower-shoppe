using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlumeriaStore.Api.Features.Inventory;

namespace PlumeriaStore.Api.Features.Reservations;

// One line item within a PickupRequest - one inventory item + quantity. Customer info, notes, and
// status all live on the parent now; this only tracks what's specific to this particular item.
public class Reservation
{
    public int Id { get; set; }

    public int PickupRequestId { get; set; }
    public PickupRequest? PickupRequest { get; set; }

    public string? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    // Snapshot of the item's characteristics at request time, so history reads fine even if the
    // item is later deleted (items have no name - just an ID tag and characteristics).
    public string ItemSnapshot { get; set; } = string.Empty;

    public int QuantityRequested { get; set; }

    // True while this line is holding QuantityRequested of the item's stock: set when the parent
    // request is confirmed, cleared when it completes (whether the stock is permanently cleared or
    // released back). The item's QuantityTotal is never changed by the hold itself - held units are
    // simply subtracted from it when reporting what's available.
    public bool StockReserved { get; set; }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        // Reservation history should survive item deletion, so the FK is set null rather than cascaded.
        builder.HasOne(reservation => reservation.InventoryItem)
            .WithMany()
            .HasForeignKey(reservation => reservation.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

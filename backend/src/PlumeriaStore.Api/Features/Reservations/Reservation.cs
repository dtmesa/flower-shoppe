using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlumeriaStore.Api.Common.Data;
using PlumeriaStore.Api.Features.Inventory;

namespace PlumeriaStore.Api.Features.Reservations;

public class Reservation
{
    public int Id { get; set; }

    public string? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    // Snapshot of the item's characteristics at request time, so history reads fine even if the
    // item is later deleted (items have no name - just an ID tag and characteristics).
    public string ItemSnapshot { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    // At least one of these is required - enforced in ReservationService rather than via
    // DataAnnotations, since neither field is unconditionally required on its own.
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public int QuantityRequested { get; set; }
    public string? Notes { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.PENDING;
    public DateTime CreatedAt { get; set; }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(reservation => reservation.CreatedAt).HasConversion<UtcDateTimeConverter>();

        // Reservation history should survive item deletion, so the FK is set null rather than cascaded.
        builder.HasOne(reservation => reservation.InventoryItem)
            .WithMany()
            .HasForeignKey(reservation => reservation.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

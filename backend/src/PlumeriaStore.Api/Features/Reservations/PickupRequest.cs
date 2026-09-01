using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Reservations;

// The parent record for a checkout - one customer submitting a cart with N items produces one
// PickupRequest with N Reservation line items underneath it, rather than N separate top-level
// records with no link between them.
public class PickupRequest
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    // At least one of these is required - enforced in ReservationService rather than via
    // DataAnnotations, since neither field is unconditionally required on its own.
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public string? Notes { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.NEW;
    public DateTime CreatedAt { get; set; }

    public List<Reservation> Items { get; set; } = new();
}

public class PickupRequestConfiguration : IEntityTypeConfiguration<PickupRequest>
{
    public void Configure(EntityTypeBuilder<PickupRequest> builder)
    {
        builder.Property(request => request.CreatedAt).HasConversion<UtcDateTimeConverter>();

        builder.HasMany(request => request.Items)
            .WithOne(item => item.PickupRequest)
            .HasForeignKey(item => item.PickupRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

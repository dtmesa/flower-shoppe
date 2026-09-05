namespace PlumeriaStore.Api.Features.Reservations;

// The parent record for a checkout - one customer submitting a cart with N items produces one
// PickupRequest with N Reservation line items underneath it, rather than N separate top-level
// records with no link between them.
public class PickupRequest
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    // At least one of these is required - enforced in ReservationService rather than at the edge,
    // since neither field is unconditionally required on its own.
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public string? Notes { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.NEW;
    public DateTime CreatedAt { get; set; }

    public List<Reservation> Items { get; set; } = new();
}

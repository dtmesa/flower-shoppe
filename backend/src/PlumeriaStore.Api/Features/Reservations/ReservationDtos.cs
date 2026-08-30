using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Reservations;

public record ReservationCreateRequest(
    [property: Required] string InventoryItemId,
    [property: Required] string CustomerName,
    // At least one of CustomerPhone/CustomerEmail is required - checked in ReservationService,
    // since neither field alone is unconditionally required.
    string? CustomerPhone,
    string? CustomerEmail,
    [property: Required][property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int QuantityRequested,
    string? Notes);

public record ReservationStatusUpdateRequest([property: Required] ReservationStatus Status);

public record ReservationResponse(
    int Id,
    string? InventoryItemId,
    string ItemSnapshot,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    int QuantityRequested,
    string? Notes,
    ReservationStatus Status,
    DateTime CreatedAt);

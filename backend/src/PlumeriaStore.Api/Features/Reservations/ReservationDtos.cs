using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Reservations;

public record PickupRequestLineItemInput(
    [property: Required] string InventoryItemId,
    [property: Required][property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int QuantityRequested);

public record PickupRequestCreateRequest(
    [property: Required] string CustomerName,
    // At least one of CustomerPhone/CustomerEmail is required - checked in ReservationService,
    // since neither field alone is unconditionally required.
    string? CustomerPhone,
    string? CustomerEmail,
    string? Notes,
    // ValidationFilter only validates this list's own attributes (non-null, non-empty) - it doesn't
    // recurse into each PickupRequestLineItemInput, so CreateAsync re-checks each line by hand.
    [property: Required][property: MinLength(1, ErrorMessage = "At least one item is required")] List<PickupRequestLineItemInput> Items);

public record ReservationStatusUpdateRequest([property: Required] ReservationStatus Status);

public record ReservationCompleteRequest(bool PermanentlyClear);

public record ReservationLineResponse(
    int Id,
    string? InventoryItemId,
    string ItemSnapshot,
    int QuantityRequested);

public record PickupRequestResponse(
    int Id,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string? Notes,
    ReservationStatus Status,
    bool StockReserved,
    DateTime CreatedAt,
    List<ReservationLineResponse> Items);

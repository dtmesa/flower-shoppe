using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Reservations;

public record PickupRequestLineItemInput(string InventoryItemId, int QuantityRequested);

public record PickupRequestCreateRequest(
    string CustomerName,
    // At least one of CustomerPhone/CustomerEmail is required - checked in ReservationService,
    // since neither field alone is unconditionally required.
    string? CustomerPhone,
    string? CustomerEmail,
    string? Notes,
    // Only the list itself is checked here (present, non-empty); each line's own quantity is
    // re-checked in ReservationService, which is also where a line is matched to a real item.
    List<PickupRequestLineItemInput> Items) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(CustomerName), CustomerName);
        errors.NotEmpty(nameof(Items), Items, "At least one item is required");
    }
}

public record ReservationStatusUpdateRequest(ReservationStatus Status) : IValidatableRequest
{
    // Status is a non-nullable enum: a missing or unparseable value fails at deserialization, so
    // by the time it reaches here there is nothing left to check.
    public void Validate(ValidationErrors errors)
    {
    }
}

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

namespace PlumeriaStore.Api.Features.Reservations;

// Explicit values preserve the meaning of already-stored ordinals (this enum is persisted as
// a plain int column) - CONFIRMED is new and gets a fresh value instead of shifting the rest.
public enum ReservationStatus
{
    NEW = 0,
    CONTACTED = 1,
    COMPLETED = 2,
    CANCELLED = 3,
    CONFIRMED = 4,
}

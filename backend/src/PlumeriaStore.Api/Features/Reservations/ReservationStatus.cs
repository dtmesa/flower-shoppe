namespace PlumeriaStore.Api.Features.Reservations;

// Stored by name in DynamoDB and serialized by name over the wire, so the order here carries no
// meaning and new values can be added anywhere.
public enum ReservationStatus
{
    NEW,
    CONTACTED,
    COMPLETED,
    CANCELLED,
    CONFIRMED,
}

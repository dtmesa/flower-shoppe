using PlumeriaStore.Api.Features.Inventory;

namespace PlumeriaStore.Api.Features.Reservations;

public class ReservationService
{
    private readonly PlumeriaDbContext _db;

    public ReservationService(PlumeriaDbContext db)
    {
        _db = db;
    }

    public async Task<ReservationResponse> CreateAsync(ReservationCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhone) && string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new BadRequestException("Provide a phone number or email address");
        }

        var item = await _db.InventoryItems.FindAsync(request.InventoryItemId)
            ?? throw new NotFoundException($"Inventory item not found: {request.InventoryItemId}");

        var reservation = new Reservation
        {
            InventoryItemId = item.Id,
            ItemSnapshot = string.Join(" · ", new[] { item.Type, item.Color, item.Size }.Where(part => !string.IsNullOrWhiteSpace(part))),
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            QuantityRequested = request.QuantityRequested,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        return ToResponse(reservation);
    }

    public async Task<List<ReservationResponse>> FindAllAsync()
    {
        var reservations = await _db.Reservations
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reservations.Select(ToResponse).ToList();
    }

    public async Task<ReservationResponse> UpdateStatusAsync(int id, ReservationStatus status)
    {
        var reservation = await _db.Reservations.FindAsync(id)
            ?? throw new NotFoundException($"Reservation not found: {id}");

        reservation.Status = status;
        await _db.SaveChangesAsync();

        return ToResponse(reservation);
    }

    public async Task DeleteAsync(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id)
            ?? throw new NotFoundException($"Reservation not found: {id}");

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
    }

    private static ReservationResponse ToResponse(Reservation reservation)
    {
        return new ReservationResponse(
            reservation.Id,
            reservation.InventoryItemId,
            reservation.ItemSnapshot,
            reservation.CustomerName,
            reservation.CustomerPhone,
            reservation.CustomerEmail,
            reservation.QuantityRequested,
            reservation.Notes,
            reservation.Status,
            reservation.CreatedAt);
    }
}

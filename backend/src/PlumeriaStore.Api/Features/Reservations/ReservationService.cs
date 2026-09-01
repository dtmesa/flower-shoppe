using System.Text.RegularExpressions;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Notifications;

namespace PlumeriaStore.Api.Features.Reservations;

public partial class ReservationService
{
    // Basic shape check ("something@something.something"), not full RFC 5322 - matches the
    // frontend's equally basic check rather than trying to be the definitive validator.
    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    private readonly PlumeriaDbContext _db;
    private readonly EmailNotificationService _emailNotificationService;

    public ReservationService(PlumeriaDbContext db, EmailNotificationService emailNotificationService)
    {
        _db = db;
        _emailNotificationService = emailNotificationService;
    }

    public async Task<PickupRequestResponse> CreateAsync(PickupRequestCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhone) && string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new BadRequestException("Provide a phone number or email address");
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            var digitCount = request.CustomerPhone.Count(char.IsDigit);
            if (digitCount != 10)
            {
                throw new BadRequestException("Phone number must have 10 digits, e.g. (555) 123-4567");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail) && !EmailPattern().IsMatch(request.CustomerEmail))
        {
            throw new BadRequestException("Enter a valid email address");
        }

        var pickupRequest = new PickupRequest
        {
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        // Units already held by confirmed requests aren't up for grabs, so availability is checked
        // against total-minus-held rather than the raw total.
        var reservedByItem = await _db.Reservations
            .Where(line => line.StockReserved && line.InventoryItemId != null)
            .GroupBy(line => line.InventoryItemId!)
            .Select(group => new { ItemId = group.Key, Reserved = group.Sum(line => line.QuantityRequested) })
            .ToDictionaryAsync(row => row.ItemId, row => row.Reserved);

        foreach (var lineInput in request.Items)
        {
            if (string.IsNullOrWhiteSpace(lineInput.InventoryItemId))
            {
                throw new BadRequestException("Each item requires an inventory item ID");
            }

            // PickupRequestLineItemInput carries a [Range(1, ...)] attribute, but the endpoint's
            // ValidationFilter runs Validator.TryValidateObject on the request itself, which does
            // not recurse into collection elements - so the range is re-checked here by hand.
            if (lineInput.QuantityRequested < 1)
            {
                throw new BadRequestException("Quantity must be at least 1");
            }

            var item = await _db.InventoryItems.FindAsync(lineInput.InventoryItemId)
                ?? throw new NotFoundException($"Inventory item not found: {lineInput.InventoryItemId}");

            var available = Math.Max(0, item.QuantityTotal - reservedByItem.GetValueOrDefault(item.Id));
            if (lineInput.QuantityRequested > available)
            {
                throw new BadRequestException(
                    $"Only {available} of \"{item.Id}\" {(available == 1 ? "is" : "are")} available");
            }

            pickupRequest.Items.Add(new Reservation
            {
                InventoryItemId = item.Id,
                ItemSnapshot = string.Join(" · ", new[] { item.Type, item.Color, item.Size }.Where(part => !string.IsNullOrWhiteSpace(part))),
                QuantityRequested = lineInput.QuantityRequested,
            });
        }

        _db.PickupRequests.Add(pickupRequest);
        await _db.SaveChangesAsync();

        var response = ToResponse(pickupRequest);
        await _emailNotificationService.NotifyNewPickupRequestAsync(response);

        return response;
    }

    public async Task<List<PickupRequestResponse>> FindAllAsync()
    {
        var requests = await _db.PickupRequests
            .Include(request => request.Items)
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();

        return requests.Select(ToResponse).ToList();
    }

    public async Task<PickupRequestResponse> UpdateStatusAsync(int id, ReservationStatus status)
    {
        if (status == ReservationStatus.COMPLETED)
        {
            throw new BadRequestException("Use the complete endpoint to mark a pickup request completed.");
        }

        var pickupRequest = await GetRequestOrThrowAsync(id);

        // Confirming places a hold on stock; any other status releases one. Neither touches the
        // item's QuantityTotal - held units are subtracted when reporting availability instead.
        var confirming = status == ReservationStatus.CONFIRMED;

        foreach (var line in pickupRequest.Items.Where(line => line.StockReserved != confirming))
        {
            line.StockReserved = confirming;
        }

        pickupRequest.Status = status;
        await _db.SaveChangesAsync();

        return ToResponse(pickupRequest);
    }

    // Completion is a separate, deliberate action (not a plain status change) because it resolves
    // the stock held since confirmation for every item in the request - either finalizing it (the
    // customer took the plants, so the units leave inventory for good) or releasing the hold
    // without shipping anything (the request fell through, stock goes back on sale).
    public async Task<PickupRequestResponse> CompleteAsync(int id, bool permanentlyClear)
    {
        var pickupRequest = await GetRequestOrThrowAsync(id);

        var heldLines = pickupRequest.Items.Where(line => line.StockReserved).ToList();
        var items = permanentlyClear ? await LoadItemsForAsync(heldLines) : null;

        foreach (var line in heldLines)
        {
            // Releasing a hold needs no arithmetic; only a permanent clear reduces the total.
            if (permanentlyClear && line.InventoryItemId is not null
                && items!.TryGetValue(line.InventoryItemId, out var item))
            {
                item.QuantityTotal = Math.Max(0, item.QuantityTotal - line.QuantityRequested);
            }
            line.StockReserved = false;
        }

        pickupRequest.Status = ReservationStatus.COMPLETED;
        await _db.SaveChangesAsync();

        return ToResponse(pickupRequest);
    }

    /// <summary>
    /// Loads every inventory item referenced by the given lines in one query, keyed by ID, so
    /// stock adjustment doesn't issue a lookup per line. Lines whose item has since been deleted
    /// (InventoryItemId goes null on delete) simply won't appear in the result.
    /// </summary>
    private async Task<Dictionary<string, InventoryItem>> LoadItemsForAsync(IEnumerable<Reservation> lines)
    {
        var ids = lines
            .Select(line => line.InventoryItemId)
            .Where(id => id is not null)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await _db.InventoryItems
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var pickupRequest = await GetRequestOrThrowAsync(id);

        _db.PickupRequests.Remove(pickupRequest);
        await _db.SaveChangesAsync();
    }

    private async Task<PickupRequest> GetRequestOrThrowAsync(int id)
    {
        var pickupRequest = await _db.PickupRequests
            .Include(request => request.Items)
            .FirstOrDefaultAsync(request => request.Id == id);

        return pickupRequest ?? throw new NotFoundException($"Pickup request not found: {id}");
    }

    private static PickupRequestResponse ToResponse(PickupRequest pickupRequest)
    {
        var items = pickupRequest.Items
            .Select(line => new ReservationLineResponse(line.Id, line.InventoryItemId, line.ItemSnapshot, line.QuantityRequested))
            .ToList();

        return new PickupRequestResponse(
            pickupRequest.Id,
            pickupRequest.CustomerName,
            pickupRequest.CustomerPhone,
            pickupRequest.CustomerEmail,
            pickupRequest.Notes,
            pickupRequest.Status,
            pickupRequest.Items.Any(line => line.StockReserved),
            pickupRequest.CreatedAt,
            items);
    }
}

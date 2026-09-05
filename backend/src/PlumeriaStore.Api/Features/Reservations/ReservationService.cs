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

    private readonly ReservationRepository _requests;
    private readonly InventoryRepository _items;
    private readonly EmailNotificationService _emailNotificationService;

    public ReservationService(
        ReservationRepository requests,
        InventoryRepository items,
        EmailNotificationService emailNotificationService)
    {
        _requests = requests;
        _items = items;
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

        var lineId = 1;

        foreach (var lineInput in request.Items)
        {
            if (string.IsNullOrWhiteSpace(lineInput.InventoryItemId))
            {
                throw new BadRequestException("Each item requires an inventory item ID");
            }

            // PickupRequestLineItemInput's own minimum is not enforced at the edge - the endpoint's
            // ValidationFilter checks the request body, not each element of its collection - so the
            // range is re-checked here by hand.
            if (lineInput.QuantityRequested < 1)
            {
                throw new BadRequestException("Quantity must be at least 1");
            }

            var item = await _items.FindByIdAsync(lineInput.InventoryItemId)
                ?? throw new NotFoundException($"Inventory item not found: {lineInput.InventoryItemId}");

            // Units already held by confirmed requests aren't up for grabs, so availability is
            // checked against total-minus-held rather than the raw total.
            var available = Math.Max(0, item.QuantityTotal - item.QuantityReserved);
            if (lineInput.QuantityRequested > available)
            {
                throw new BadRequestException(
                    $"Only {available} of \"{item.Id}\" {(available == 1 ? "is" : "are")} available");
            }

            pickupRequest.Items.Add(new Reservation
            {
                Id = lineId++,
                InventoryItemId = item.Id,
                ItemSnapshot = string.Join(" · ", new[] { item.Type, item.Color, item.Size }.Where(part => !string.IsNullOrWhiteSpace(part))),
                QuantityRequested = lineInput.QuantityRequested,
            });
        }

        // Only assigned once the request is known to be valid, so a rejected submission doesn't
        // burn an ID and leave a gap in the admin's numbering.
        pickupRequest.Id = await _requests.NextIdAsync();

        // A brand-new request holds nothing yet, so no stock moves with this write.
        await _requests.SaveAsync(pickupRequest);

        var response = ToResponse(pickupRequest);
        await _emailNotificationService.NotifyNewPickupRequestAsync(response);

        return response;
    }

    public async Task<List<PickupRequestResponse>> FindAllAsync()
    {
        var requests = await _requests.FindAllAsync();

        return requests
            .OrderByDescending(request => request.CreatedAt)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<PickupRequestResponse> UpdateStatusAsync(int id, ReservationStatus status)
    {
        if (status == ReservationStatus.COMPLETED)
        {
            throw new BadRequestException("Use the complete endpoint to mark a pickup request completed");
        }

        var pickupRequest = await GetRequestOrThrowAsync(id);

        // Confirming places a hold on stock; any other status releases one. Neither touches the
        // item's QuantityTotal - held units are subtracted when reporting availability instead.
        var confirming = status == ReservationStatus.CONFIRMED;

        var changedLines = pickupRequest.Items.Where(line => line.StockReserved != confirming).ToList();
        var adjustments = await BuildAdjustmentsAsync(
            changedLines,
            (item, heldByThisRequest) => item with
            {
                Reserved = item.Reserved + (confirming ? heldByThisRequest : -heldByThisRequest),
            });

        foreach (var line in changedLines)
        {
            line.StockReserved = confirming;
        }

        pickupRequest.Status = status;
        await _requests.SaveAsync(pickupRequest, adjustments);

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
        var adjustments = await BuildAdjustmentsAsync(
            heldLines,
            (item, heldByThisRequest) => item with
            {
                // Releasing a hold needs no arithmetic on the total; only a permanent clear reduces it.
                Total = permanentlyClear ? Math.Max(0, item.Total - heldByThisRequest) : item.Total,
                Reserved = item.Reserved - heldByThisRequest,
            });

        foreach (var line in heldLines)
        {
            line.StockReserved = false;
        }

        pickupRequest.Status = ReservationStatus.COMPLETED;
        await _requests.SaveAsync(pickupRequest, adjustments);

        return ToResponse(pickupRequest);
    }

    public async Task DeleteAsync(int id)
    {
        var pickupRequest = await GetRequestOrThrowAsync(id);

        // Deleting a request that is still holding stock has to give those units back. Under the
        // old schema that fell out of cascading the line items away, since the reserved count was
        // summed from them; now the count lives on the item and has to be moved explicitly.
        var released = await BuildAdjustmentsAsync(
            pickupRequest.Items.Where(line => line.StockReserved),
            (item, heldByThisRequest) => item with { Reserved = item.Reserved - heldByThisRequest });

        await _requests.DeleteAsync(id, released);
    }

    /// <summary>Counts read off an inventory item, so an adjustment can be expressed as "from these, to those".</summary>
    private readonly record struct StockCounts(int Total, int Reserved);

    /// <summary>
    /// Turns a set of line items into the per-item stock writes they imply. Lines are grouped by
    /// item first, so a request naming the same item twice moves its counts once, and lines whose
    /// item has since been deleted are skipped - there is nothing left to adjust.
    /// </summary>
    private async Task<List<StockAdjustment>> BuildAdjustmentsAsync(
        IEnumerable<Reservation> lines,
        Func<StockCounts, int, StockCounts> apply)
    {
        var quantityByItem = lines
            .Where(line => line.InventoryItemId is not null)
            .GroupBy(line => line.InventoryItemId!)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.QuantityRequested));

        var adjustments = new List<StockAdjustment>();

        foreach (var (itemId, quantity) in quantityByItem)
        {
            var item = await _items.FindByIdAsync(itemId);
            if (item is null)
            {
                continue;
            }

            var from = new StockCounts(item.QuantityTotal, item.QuantityReserved);
            var to = apply(from, quantity);

            adjustments.Add(new StockAdjustment(itemId, from.Total, from.Reserved, to.Total, Math.Max(0, to.Reserved)));
        }

        return adjustments;
    }

    private async Task<PickupRequest> GetRequestOrThrowAsync(int id)
    {
        return await _requests.FindByIdAsync(id)
            ?? throw new NotFoundException($"Pickup request not found: {id}");
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

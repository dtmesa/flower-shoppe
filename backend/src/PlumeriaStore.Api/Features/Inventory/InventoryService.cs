using PlumeriaStore.Api.Features.Reservations;

namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryService
{
    private readonly InventoryRepository _items;
    private readonly CategoryRepository _categories;
    private readonly ReservationRepository _reservations;
    private readonly IFileStorage _fileStorage;

    public InventoryService(
        InventoryRepository items,
        CategoryRepository categories,
        ReservationRepository reservations,
        IFileStorage fileStorage)
    {
        _items = items;
        _categories = categories;
        _reservations = reservations;
        _fileStorage = fileStorage;
    }

    // Categories are admin-editable (see CategoryService), so the code for each Type/Color/Size
    // value is looked up rather than hardcoded; concatenating the three gives a human-readable tag
    // matching the real-world label an admin would write on the plant, e.g. "RYM" for
    // Rooted Plant + Yellow/White + Medium. Nothing here stops two categories of the same kind
    // from sharing a code, which would make their generated IDs collide - not guarded against yet.
    private async Task<string> GenerateIdAsync(string type, string color, string size)
    {
        // The whole category partition is a handful of rows, so one read covers all three lookups.
        var categories = await _categories.FindAllAsync();

        string CodeFor(CategoryKind kind, string name, string label) =>
            categories.FirstOrDefault(category => category.Kind == kind && category.Name == name)?.Code
            ?? throw new BadRequestException($"Unknown {label}: \"{name}\"");

        var typeCode = CodeFor(CategoryKind.TYPE, type, "type");
        var colorCode = CodeFor(CategoryKind.COLOR, color, "color");
        var sizeCode = CodeFor(CategoryKind.SIZE, size, "size");

        return $"{typeCode}{colorCode}{sizeCode}";
    }

    public async Task<List<InventoryItemResponse>> FindAllAsync()
    {
        var items = await _items.FindAllAsync();
        return items.Select(ToResponse).ToList();
    }

    public async Task<InventoryItemResponse> FindByIdAsync(string id)
    {
        return ToResponse(await GetItemOrThrowAsync(id));
    }

    public async Task<InventoryItemResponse> CreateAsync(InventoryItemCreateRequest request)
    {
        var id = await GenerateIdAsync(request.Type, request.Color, request.Size);

        var item = new InventoryItem
        {
            Id = id,
            Type = request.Type,
            Color = request.Color,
            Size = request.Size,
            Price = request.Price,
            QuantityTotal = request.QuantityTotal,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // The ID encodes type+color+size, so "this key is already taken" is exactly the duplicate
        // rule - let the conditional write enforce it instead of reading first and hoping.
        if (!await _items.TryCreateAsync(item))
        {
            throw new BadRequestException(
                $"An item with type \"{request.Type}\", color \"{request.Color}\", and size \"{request.Size}\" " +
                $"already exists (ID: {id}). Increase its quantity instead of creating a duplicate.");
        }

        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> UpdateAsync(string id, InventoryItemUpdateRequest request)
    {
        var item = await GetItemOrThrowAsync(id);
        var reserved = item.QuantityReserved;

        // Dropping the total below what's already held would leave the item owing stock it
        // doesn't have; the admin has to release those requests first.
        if (request.QuantityTotal < reserved)
        {
            throw new BadRequestException(
                $"{reserved} of this item {(reserved == 1 ? "is" : "are")} reserved by confirmed pickup requests, " +
                $"so the total can't be set below {reserved}.");
        }

        item.Price = request.Price;
        item.QuantityTotal = request.QuantityTotal;
        item.Description = request.Description;
        item.UpdatedAt = DateTime.UtcNow;

        await _items.SaveAsync(item);
        return ToResponse(item);
    }

    public async Task DeleteAsync(string id)
    {
        var item = await GetItemOrThrowAsync(id);

        await _items.DeleteAsync(id);

        // Past requests keep their line items and snapshots but stop pointing at an item that no
        // longer exists - the ON DELETE SET NULL the relational schema used to do on its own.
        await _reservations.ClearInventoryItemReferencesAsync(id);

        foreach (var image in item.Images)
        {
            await _fileStorage.DeleteAsync(image.Filename);
        }
    }

    public async Task<InventoryItemResponse> AddImageAsync(string itemId, IFormFile file)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var filename = await _fileStorage.StoreAsync(file);

        item.Images.Add(new InventoryImage
        {
            Id = item.NextImageId++,
            Filename = filename,
            SortOrder = item.Images.Count,
            // The first photo on an item has nothing to be chosen over, so it's the thumbnail by
            // default - later uploads stay non-primary until the admin explicitly picks one.
            IsPrimary = item.Images.Count == 0,
        });

        await _items.SaveAsync(item);
        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> DeleteImageAsync(string itemId, int imageId)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var image = item.Images.FirstOrDefault(img => img.Id == imageId)
            ?? throw new NotFoundException($"Image not found: {imageId}");

        var wasPrimary = image.IsPrimary;
        item.Images.Remove(image);

        // Deleting the primary photo shouldn't leave the item with photos but no thumbnail -
        // hand primary status to whichever photo now sorts first, if any are left.
        if (wasPrimary)
        {
            var replacement = item.Images.OrderBy(img => img.SortOrder).FirstOrDefault();
            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        await _items.SaveAsync(item);
        await _fileStorage.DeleteAsync(image.Filename);

        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> SetPrimaryImageAsync(string itemId, int imageId)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var image = item.Images.FirstOrDefault(img => img.Id == imageId)
            ?? throw new NotFoundException($"Image not found: {imageId}");

        foreach (var img in item.Images)
        {
            img.IsPrimary = img.Id == image.Id;
        }

        await _items.SaveAsync(item);
        return ToResponse(item);
    }

    private async Task<InventoryItem> GetItemOrThrowAsync(string id)
    {
        return await _items.FindByIdAsync(id)
            ?? throw new NotFoundException($"Inventory item not found: {id}");
    }

    private static InventoryItemResponse ToResponse(InventoryItem item)
    {
        var images = item.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new InventoryImageResponse(image.Id, $"/uploads/{image.Filename}", image.SortOrder, image.IsPrimary))
            .ToList();

        return new InventoryItemResponse(
            item.Id,
            item.Type,
            item.Color,
            item.Size,
            item.Price,
            item.QuantityTotal,
            item.QuantityReserved,
            // Never negative, even if an admin somehow lowered the total below the held amount.
            Math.Max(0, item.QuantityTotal - item.QuantityReserved),
            item.Description,
            images,
            item.CreatedAt,
            item.UpdatedAt);
    }
}

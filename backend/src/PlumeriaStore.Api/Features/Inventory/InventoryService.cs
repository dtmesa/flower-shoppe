namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryService
{
    private readonly PlumeriaDbContext _db;
    private readonly FileStorageService _fileStorageService;

    public InventoryService(PlumeriaDbContext db, FileStorageService fileStorageService)
    {
        _db = db;
        _fileStorageService = fileStorageService;
    }

    // Categories are admin-editable (see CategoryService), so the code for each Type/Color/Size
    // value is looked up rather than hardcoded; concatenating the three gives a human-readable tag
    // matching the real-world label an admin would write on the plant, e.g. "RYM" for
    // Rooted Plant + Yellow/White + Medium. Nothing here stops two categories of the same kind
    // from sharing a code, which would make their generated IDs collide - not guarded against yet.
    private async Task<string> GenerateIdAsync(string type, string color, string size)
    {
        // One round-trip for all three lookups rather than three sequential ones - the category
        // table is tiny, so fetching the matching rows in a single query and picking them apart
        // in memory is cheaper than three awaits.
        var matches = await _db.Categories
            .Where(category =>
                (category.Kind == CategoryKind.TYPE && category.Name == type) ||
                (category.Kind == CategoryKind.COLOR && category.Name == color) ||
                (category.Kind == CategoryKind.SIZE && category.Name == size))
            .Select(category => new { category.Kind, category.Code })
            .ToListAsync();

        string CodeFor(CategoryKind kind, string name, string label) =>
            matches.FirstOrDefault(match => match.Kind == kind)?.Code
            ?? throw new BadRequestException($"Unknown {label}: \"{name}\"");

        var typeCode = CodeFor(CategoryKind.TYPE, type, "type");
        var colorCode = CodeFor(CategoryKind.COLOR, color, "color");
        var sizeCode = CodeFor(CategoryKind.SIZE, size, "size");

        return $"{typeCode}{colorCode}{sizeCode}";
    }

    /// <summary>
    /// Units currently held per item by confirmed pickup requests, keyed by item ID. Items with
    /// nothing on hold are absent from the result rather than present with a zero.
    /// </summary>
    private async Task<Dictionary<string, int>> GetReservedQuantitiesAsync(string? itemId = null)
    {
        var query = _db.Reservations.Where(line => line.StockReserved && line.InventoryItemId != null);

        if (itemId is not null)
        {
            query = query.Where(line => line.InventoryItemId == itemId);
        }

        return await query
            .GroupBy(line => line.InventoryItemId!)
            .Select(group => new { ItemId = group.Key, Reserved = group.Sum(line => line.QuantityRequested) })
            .ToDictionaryAsync(row => row.ItemId, row => row.Reserved);
    }

    public async Task<List<InventoryItemResponse>> FindAllAsync()
    {
        var items = await _db.InventoryItems
            .Include(item => item.Images)
            .AsNoTracking()
            .ToListAsync();

        // One grouped query for the whole list rather than a per-item lookup.
        var reserved = await GetReservedQuantitiesAsync();

        return items.Select(item => ToResponse(item, reserved.GetValueOrDefault(item.Id))).ToList();
    }

    public async Task<InventoryItemResponse> FindByIdAsync(string id)
    {
        var item = await GetItemOrThrowAsync(id);
        var reserved = await GetReservedQuantitiesAsync(id);
        return ToResponse(item, reserved.GetValueOrDefault(id));
    }

    public async Task<InventoryItemResponse> CreateAsync(InventoryItemCreateRequest request)
    {
        var id = await GenerateIdAsync(request.Type, request.Color, request.Size);

        if (await _db.InventoryItems.AnyAsync(i => i.Id == id))
        {
            throw new BadRequestException(
                $"An item with type \"{request.Type}\", color \"{request.Color}\", and size \"{request.Size}\" " +
                $"already exists (ID: {id}). Increase its quantity instead of creating a duplicate.");
        }

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

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        // A brand-new item can't have holds against it yet.
        return ToResponse(item, 0);
    }

    public async Task<InventoryItemResponse> UpdateAsync(string id, InventoryItemUpdateRequest request)
    {
        var item = await GetItemOrThrowAsync(id);
        var reserved = (await GetReservedQuantitiesAsync(id)).GetValueOrDefault(id);

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

        await _db.SaveChangesAsync();
        return ToResponse(item, reserved);
    }

    public async Task DeleteAsync(string id)
    {
        var item = await GetItemOrThrowAsync(id);

        foreach (var image in item.Images)
        {
            _fileStorageService.Delete(image.Filename);
        }

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task<InventoryItemResponse> AddImageAsync(string itemId, IFormFile file)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var filename = await _fileStorageService.StoreAsync(file);

        item.Images.Add(new InventoryImage
        {
            Filename = filename,
            SortOrder = item.Images.Count,
            // The first photo on an item has nothing to be chosen over, so it's the thumbnail by
            // default - later uploads stay non-primary until the admin explicitly picks one.
            IsPrimary = item.Images.Count == 0,
        });

        await _db.SaveChangesAsync();
        return await ToResponseWithReservedAsync(item);
    }

    public async Task<InventoryItemResponse> DeleteImageAsync(string itemId, int imageId)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var image = item.Images.FirstOrDefault(img => img.Id == imageId)
            ?? throw new NotFoundException($"Image not found: {imageId}");

        var wasPrimary = image.IsPrimary;
        item.Images.Remove(image);
        _db.InventoryImages.Remove(image);

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

        await _db.SaveChangesAsync();
        _fileStorageService.Delete(image.Filename);

        return await ToResponseWithReservedAsync(item);
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

        await _db.SaveChangesAsync();
        return await ToResponseWithReservedAsync(item);
    }

    private async Task<InventoryItem> GetItemOrThrowAsync(string id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);

        return item ?? throw new NotFoundException($"Inventory item not found: {id}");
    }

    /// <summary>Convenience for call sites that have an item but haven't looked up its holds.</summary>
    private async Task<InventoryItemResponse> ToResponseWithReservedAsync(InventoryItem item)
    {
        var reserved = await GetReservedQuantitiesAsync(item.Id);
        return ToResponse(item, reserved.GetValueOrDefault(item.Id));
    }

    private static InventoryItemResponse ToResponse(InventoryItem item, int reserved)
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
            reserved,
            // Never negative, even if an admin somehow lowered the total below the held amount.
            Math.Max(0, item.QuantityTotal - reserved),
            item.Description,
            images,
            item.CreatedAt,
            item.UpdatedAt);
    }
}

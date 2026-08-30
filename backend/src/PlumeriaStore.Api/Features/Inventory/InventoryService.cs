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

    public async Task<List<InventoryItemResponse>> FindAllAsync()
    {
        var items = await _db.InventoryItems
            .Include(item => item.Images)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(ToResponse).ToList();
    }

    public async Task<InventoryItemResponse> FindByIdAsync(string id)
    {
        var item = await GetItemOrThrowAsync(id);
        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> CreateAsync(InventoryItemCreateRequest request)
    {
        if (await _db.InventoryItems.AnyAsync(i => i.Id == request.Id))
        {
            throw new BadRequestException($"An item with ID \"{request.Id}\" already exists");
        }

        var item = new InventoryItem { Id = request.Id };
        ApplyRequest(item, request.Type, request.Color, request.Size, request.Price, request.QuantityAvailable, request.Description);
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> UpdateAsync(string id, InventoryItemUpdateRequest request)
    {
        var item = await GetItemOrThrowAsync(id);
        ApplyRequest(item, request.Type, request.Color, request.Size, request.Price, request.QuantityAvailable, request.Description);
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToResponse(item);
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
        });

        await _db.SaveChangesAsync();
        return ToResponse(item);
    }

    public async Task<InventoryItemResponse> DeleteImageAsync(string itemId, int imageId)
    {
        var item = await GetItemOrThrowAsync(itemId);
        var image = item.Images.FirstOrDefault(img => img.Id == imageId)
            ?? throw new NotFoundException($"Image not found: {imageId}");

        item.Images.Remove(image);
        _db.InventoryImages.Remove(image);
        await _db.SaveChangesAsync();
        _fileStorageService.Delete(image.Filename);

        return ToResponse(item);
    }

    private async Task<InventoryItem> GetItemOrThrowAsync(string id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);

        return item ?? throw new NotFoundException($"Inventory item not found: {id}");
    }

    private static void ApplyRequest(InventoryItem item, string type, string? color, string? size, decimal price, int quantityAvailable, string? description)
    {
        item.Type = type;
        item.Color = color;
        item.Size = size;
        item.Price = price;
        item.QuantityAvailable = quantityAvailable;
        item.Description = description;
    }

    private static InventoryItemResponse ToResponse(InventoryItem item)
    {
        var images = item.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new InventoryImageResponse(image.Id, $"/uploads/{image.Filename}", image.SortOrder))
            .ToList();

        return new InventoryItemResponse(
            item.Id,
            item.Type,
            item.Color,
            item.Size,
            item.Price,
            item.QuantityAvailable,
            item.Description,
            images,
            item.CreatedAt,
            item.UpdatedAt);
    }
}

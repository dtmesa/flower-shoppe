namespace PlumeriaStore.Api.Features.Inventory;

public class CategoryService
{
    private readonly PlumeriaDbContext _db;

    public CategoryService(PlumeriaDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryResponse>> FindAllAsync()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Kind)
            .ThenBy(category => category.Name)
            .ToListAsync();

        return categories.Select(ToResponse).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryCreateRequest request)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.Categories.AnyAsync(category => category.Kind == request.Kind && category.Name == name))
        {
            throw new BadRequestException($"A {request.Kind.ToString().ToLowerInvariant()} named \"{name}\" already exists");
        }

        var category = new InventoryCategory { Kind = request.Kind, Name = name, Code = code };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryUpdateRequest request)
    {
        var category = await _db.Categories.FindAsync(id)
            ?? throw new NotFoundException($"Category not found: {id}");

        category.Name = request.Name.Trim();
        category.Code = request.Code.Trim().ToUpperInvariant();
        await _db.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id)
            ?? throw new NotFoundException($"Category not found: {id}");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }

    private static CategoryResponse ToResponse(InventoryCategory category) =>
        new(category.Id, category.Kind, category.Name, category.Code);
}

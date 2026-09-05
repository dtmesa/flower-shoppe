namespace PlumeriaStore.Api.Features.Inventory;

public class CategoryService
{
    private readonly CategoryRepository _categories;

    public CategoryService(CategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<List<CategoryResponse>> FindAllAsync()
    {
        var categories = await _categories.FindAllAsync();

        return categories
            .OrderBy(category => category.Kind)
            .ThenBy(category => category.Name, StringComparer.Ordinal)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryCreateRequest request)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        var category = new InventoryCategory
        {
            Id = await _categories.NextIdAsync(),
            Kind = request.Kind,
            Name = name,
            Code = code,
        };

        // Kind + name is the row's key, so the write itself rejects a duplicate - no separate
        // existence check to race against.
        if (!await _categories.TryCreateAsync(category))
        {
            throw new BadRequestException($"A {request.Kind.ToString().ToLowerInvariant()} named \"{name}\" already exists");
        }

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryUpdateRequest request)
    {
        var category = await _categories.FindByIdAsync(id)
            ?? throw new NotFoundException($"Category not found: {id}");

        var updated = new InventoryCategory
        {
            Id = category.Id,
            Kind = category.Kind,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
        };

        if (!await _categories.ReplaceAsync(category, updated))
        {
            throw new BadRequestException($"A {updated.Kind.ToString().ToLowerInvariant()} named \"{updated.Name}\" already exists");
        }

        return ToResponse(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _categories.FindByIdAsync(id)
            ?? throw new NotFoundException($"Category not found: {id}");

        await _categories.DeleteAsync(category);
    }

    private static CategoryResponse ToResponse(InventoryCategory category) =>
        new(category.Id, category.Kind, category.Name, category.Code);
}

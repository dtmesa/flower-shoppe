using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Tests.TestSupport;

namespace PlumeriaStore.Api.Tests.Features.Inventory;

public class CategoryServiceTests : IDisposable
{
    private readonly PlumeriaTestContext _context = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = _context.NewCategoryService();
    }

    [Fact]
    public async Task FindAllAsync_returns_the_seeded_defaults()
    {
        var categories = await _service.FindAllAsync();

        Assert.Contains(categories, c => c.Kind == CategoryKind.TYPE && c.Name == "Rooted Plant" && c.Code == "R");
        Assert.Equal(8, categories.Count);
    }

    [Fact]
    public async Task CreateAsync_adds_a_new_category()
    {
        var created = await _service.CreateAsync(new CategoryCreateRequest(CategoryKind.COLOR, "Purple", "U"));

        Assert.Equal("Purple", created.Name);
        Assert.Equal("U", created.Code);

        var all = await _service.FindAllAsync();
        Assert.Contains(all, c => c.Id == created.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_name_within_the_same_kind()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(new CategoryCreateRequest(CategoryKind.SIZE, "Medium", "X")));
    }

    [Fact]
    public async Task UpdateAsync_renames_a_category_and_recodes_it()
    {
        var created = await _service.CreateAsync(new CategoryCreateRequest(CategoryKind.COLOR, "Purple", "U"));

        var updated = await _service.UpdateAsync(created.Id, new CategoryUpdateRequest("Lavender", "L"));

        Assert.Equal("Lavender", updated.Name);
        Assert.Equal("L", updated.Code);
    }

    [Fact]
    public async Task DeleteAsync_removes_a_category()
    {
        var created = await _service.CreateAsync(new CategoryCreateRequest(CategoryKind.COLOR, "Purple", "U"));

        await _service.DeleteAsync(created.Id);

        var all = await _service.FindAllAsync();
        Assert.DoesNotContain(all, c => c.Id == created.Id);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_for_missing_category()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999));
    }

    public void Dispose() => _context.Dispose();
}

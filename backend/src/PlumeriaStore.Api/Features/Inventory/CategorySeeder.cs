namespace PlumeriaStore.Api.Features.Inventory;

public static class CategorySeeder
{
    private static readonly (CategoryKind Kind, string Name, string Code)[] Defaults =
    [
        (CategoryKind.TYPE, "Cutting", "C"),
        (CategoryKind.TYPE, "Rooted Plant", "R"),
        (CategoryKind.COLOR, "Red", "R"),
        (CategoryKind.COLOR, "Pink", "P"),
        (CategoryKind.COLOR, "Yellow/White", "Y"),
        (CategoryKind.SIZE, "Small", "S"),
        (CategoryKind.SIZE, "Medium", "M"),
        (CategoryKind.SIZE, "Large", "L"),
    ];

    /// <summary>
    /// Seeds the original fixed Type/Color/Size values once, on first run, so existing behavior
    /// (and existing item IDs like "RYM") stays intact after categories became admin-editable.
    /// </summary>
    public static async Task SeedDefaultCategoriesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await SeedDefaultCategoriesAsync(scope.ServiceProvider.GetRequiredService<CategoryRepository>());
    }

    /// <summary>The same defaults against a repository directly, for tests that need them present.</summary>
    public static async Task SeedDefaultCategoriesAsync(CategoryRepository categories)
    {
        if ((await categories.FindAllAsync()).Count > 0)
        {
            return;
        }

        foreach (var (kind, name, code) in Defaults)
        {
            // Each write is conditional on the row being absent, so a second cold start racing
            // this one adds nothing and neither fails.
            await categories.TryCreateAsync(new InventoryCategory
            {
                Id = await categories.NextIdAsync(),
                Kind = kind,
                Name = name,
                Code = code,
            });
        }
    }
}

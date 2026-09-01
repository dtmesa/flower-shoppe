namespace PlumeriaStore.Api.Features.Inventory;

public static class CategorySeeder
{
    /// <summary>
    /// Seeds the original fixed Type/Color/Size values once, on first run, so existing behavior
    /// (and existing item IDs like "RYM") stays intact after categories became admin-editable.
    /// </summary>
    public static async Task SeedDefaultCategoriesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlumeriaDbContext>();

        if (await db.Categories.AnyAsync())
        {
            return;
        }

        db.Categories.AddRange(
            new InventoryCategory { Kind = CategoryKind.TYPE, Name = "Cutting", Code = "C" },
            new InventoryCategory { Kind = CategoryKind.TYPE, Name = "Rooted Plant", Code = "R" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Red", Code = "R" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Pink", Code = "P" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Yellow/White", Code = "Y" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Small", Code = "S" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Medium", Code = "M" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Large", Code = "L" });

        await db.SaveChangesAsync();
    }
}

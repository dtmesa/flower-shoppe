using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlumeriaStore.Api.Features.Inventory;

public enum CategoryKind
{
    TYPE,
    COLOR,
    SIZE,
}

// The admin-editable source of truth for what Type/Color/Size values exist. Code is the letter(s)
// that feed InventoryService.GenerateId - editable here too, so an admin can shape new item IDs,
// though nothing currently guards against two categories of the same Kind sharing a Code.
public class InventoryCategory
{
    public int Id { get; set; }
    public CategoryKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.Code).IsRequired().HasMaxLength(4);
        builder.HasIndex(category => new { category.Kind, category.Name }).IsUnique();
    }
}

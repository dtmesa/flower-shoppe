using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryItem
{
    // Set by the admin at creation time to match the physical ID tag on the plant - not
    // database-generated.
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public int QuantityAvailable { get; set; }
    public string? Description { get; set; }
    public List<InventoryImage> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Price).HasPrecision(10, 2);
        builder.Property(item => item.CreatedAt).HasConversion<UtcDateTimeConverter>();
        builder.Property(item => item.UpdatedAt).HasConversion<UtcDateTimeConverter>();

        builder.HasMany(item => item.Images)
            .WithOne(image => image.InventoryItem)
            .HasForeignKey(image => image.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

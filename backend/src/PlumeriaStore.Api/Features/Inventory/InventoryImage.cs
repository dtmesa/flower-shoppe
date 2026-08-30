namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryImage
{
    public int Id { get; set; }
    public string InventoryItemId { get; set; } = string.Empty;
    public InventoryItem? InventoryItem { get; set; }
    public string Filename { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

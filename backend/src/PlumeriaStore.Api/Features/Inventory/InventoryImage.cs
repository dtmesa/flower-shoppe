namespace PlumeriaStore.Api.Features.Inventory;

public class InventoryImage
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

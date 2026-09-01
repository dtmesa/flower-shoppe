using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Inventory;

// No Id here - it's derived from Type+Color+Size (see InventoryService.GenerateId) so a given
// category can only ever have one row; a second create for the same combo is rejected in favor
// of raising the existing row's quantity.
public record InventoryItemCreateRequest(
    [property: Required] string Type,
    [property: Required] string Color,
    [property: Required] string Size,
    [property: Required][property: Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater")] decimal Price,
    [property: Required][property: Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater")] int QuantityTotal,
    string? Description);

// Type/Color/Size are immutable after creation - they're baked into the Id, so changing them
// would desync the tag from what it's supposed to encode. Only these fields are ever revised.
public record InventoryItemUpdateRequest(
    [property: Required][property: Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater")] decimal Price,
    [property: Required][property: Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater")] int QuantityTotal,
    string? Description);

public record InventoryImageResponse(int Id, string Url, int SortOrder, bool IsPrimary);

/// <param name="QuantityTotal">Units physically on hand, including any currently held.</param>
/// <param name="QuantityReserved">Units held by confirmed-but-not-yet-completed pickup requests.</param>
/// <param name="QuantityAvailable">Total minus reserved - what a customer can actually request.</param>
public record InventoryItemResponse(
    string Id,
    string Type,
    string? Color,
    string? Size,
    decimal Price,
    int QuantityTotal,
    int QuantityReserved,
    int QuantityAvailable,
    string? Description,
    List<InventoryImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);

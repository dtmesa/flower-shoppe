using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Inventory;

// No Id here - it's derived from Type+Color+Size (see InventoryService.GenerateIdAsync) so a given
// category can only ever have one row; a second create for the same combo is rejected in favor
// of raising the existing row's quantity.
public record InventoryItemCreateRequest(
    string Type,
    string Color,
    string Size,
    decimal Price,
    int QuantityTotal,
    string? Description) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(Type), Type);
        errors.Required(nameof(Color), Color);
        errors.Required(nameof(Size), Size);
        errors.AtLeast(nameof(Price), Price, 0, "Price must be zero or greater");
        errors.AtLeast(nameof(QuantityTotal), QuantityTotal, 0, "Quantity must be zero or greater");
    }
}

// Type/Color/Size are immutable after creation - they're baked into the Id, so changing them
// would desync the tag from what it's supposed to encode. Only these fields are ever revised.
public record InventoryItemUpdateRequest(
    decimal Price,
    int QuantityTotal,
    string? Description) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.AtLeast(nameof(Price), Price, 0, "Price must be zero or greater");
        errors.AtLeast(nameof(QuantityTotal), QuantityTotal, 0, "Quantity must be zero or greater");
    }
}

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

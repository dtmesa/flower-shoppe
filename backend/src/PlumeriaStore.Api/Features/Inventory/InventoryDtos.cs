using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Inventory;

public record InventoryItemCreateRequest(
    [property: Required] string Id,
    [property: Required] string Type,
    string? Color,
    string? Size,
    [property: Required][property: Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater")] decimal Price,
    [property: Required][property: Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater")] int QuantityAvailable,
    string? Description);

public record InventoryItemUpdateRequest(
    [property: Required] string Type,
    string? Color,
    string? Size,
    [property: Required][property: Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater")] decimal Price,
    [property: Required][property: Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater")] int QuantityAvailable,
    string? Description);

public record InventoryImageResponse(int Id, string Url, int SortOrder);

public record InventoryItemResponse(
    string Id,
    string Type,
    string? Color,
    string? Size,
    decimal Price,
    int QuantityAvailable,
    string? Description,
    List<InventoryImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);

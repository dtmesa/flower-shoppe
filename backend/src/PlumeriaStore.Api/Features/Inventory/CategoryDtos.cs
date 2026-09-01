using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Inventory;

public record CategoryCreateRequest(
    [property: Required] CategoryKind Kind,
    [property: Required] string Name,
    [property: Required][property: StringLength(4, MinimumLength = 1)] string Code);

public record CategoryUpdateRequest(
    [property: Required] string Name,
    [property: Required][property: StringLength(4, MinimumLength = 1)] string Code);

public record CategoryResponse(int Id, CategoryKind Kind, string Name, string Code);
